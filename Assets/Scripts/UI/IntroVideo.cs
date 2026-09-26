using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays the intro video once, on the very first launch: loading screen, then the
/// video, then the main menu.
///
/// It sets itself up after the first scene loads (no scene wiring): the clip comes from
/// Resources/IntroVideoConfig. The video is prepared while the loading screen runs;
/// when that ends, LoadingMenuUI hands over to it instead of opening the menu. It covers
/// the whole screen. Menu music waits until it is over - only the
/// video's own sound is heard - and a PlayerPrefs flag makes sure it never plays again.
/// </summary>
public class IntroVideo : MonoBehaviour
{
    public const string PlayedKey = "IntroVideoPlayed";

    // Prepared and waiting for the loading screen to finish.
    private static IntroVideo pending;

    private VideoPlayer player;
    private RenderTexture target;
    private GameObject overlay;
    private bool finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        if (PlayerPrefs.GetInt(PlayedKey, 0) == 1)
            return;

        IntroVideoConfig config = Resources.Load<IntroVideoConfig>("IntroVideoConfig");

        if (config == null || config.clip == null)
            return;

        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.SetPaused(true);

        pending = new GameObject("[IntroVideo]").AddComponent<IntroVideo>();
        pending.Prepare(config.clip);
    }

    /// <summary>
    /// Called by the loading screen where it would open the main menu. True when the
    /// intro takes over; it opens the main menu itself once it ends.
    /// </summary>
    public static bool TryPlayPending()
    {
        if (pending == null)
            return false;

        IntroVideo intro = pending;
        pending = null;
        intro.Play();
        return true;
    }

    private void Prepare(VideoClip clip)
    {
        target = new RenderTexture((int)clip.width, (int)clip.height, 0);

        player = gameObject.AddComponent<VideoPlayer>();
        player.playOnAwake = false;
        player.clip = clip;
        player.isLooping = false;
        player.renderMode = VideoRenderMode.RenderTexture;
        player.targetTexture = target;
        player.audioOutputMode = VideoAudioOutputMode.Direct;
        player.skipOnDrop = true;
        player.waitForFirstFrame = true;

        player.loopPointReached += _ => Finish();
        player.errorReceived += (_, message) =>
        {
            Debug.LogWarning("[IntroVideo] " + message);
            Finish();
        };

        // Decoding starts now, so the video is ready by the time the loading ends.
        player.Prepare();
    }

    private void Play()
    {
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.HideAllPanels();

        BuildOverlay();
        player.Play();
    }

    /// <summary>
    /// Black full-screen canvas above every menu canvas, with the video filling it. It
    /// takes all touches, so nothing behind can be pressed.
    /// </summary>
    private void BuildOverlay()
    {
        overlay = new GameObject("IntroVideoCanvas", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        overlay.AddComponent<GraphicRaycaster>();
        LSUITheme.EnsureEventSystem();

        var background = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
        background.transform.SetParent(overlay.transform, false);
        background.color = Color.black;
        LSUITheme.Stretch(background.rectTransform);

        var video = new GameObject("Video", typeof(RectTransform)).AddComponent<RawImage>();
        video.transform.SetParent(overlay.transform, false);
        video.texture = target;
        video.raycastTarget = false;
        LSUITheme.Stretch(video.rectTransform);

        // Fill the whole screen without stretching: on phones wider than 16:9 (most are
        // 19.5:9 - 20:9) the video's top and bottom edges are cropped instead of black
        // bars being drawn at the sides.
        var fitter = video.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = target.width / (float)target.height;

    }

    private void Finish()
    {
        if (finished)
            return;

        finished = true;

        PlayerPrefs.SetInt(PlayedKey, 1);
        PlayerPrefs.Save();

        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.SetPaused(false);

        // Loading screen -> video -> main menu.
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowMainMenu();

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (pending == this)
            pending = null;

        if (target != null)
        {
            target.Release();
            Destroy(target);
        }
    }
}
