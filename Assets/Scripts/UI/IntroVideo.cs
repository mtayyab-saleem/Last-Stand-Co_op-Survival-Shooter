using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Plays the intro video once, on the very first launch: video, then the loading
/// screen, then the main menu.
///
/// It starts itself after the first scene loads (no scene wiring): the clip comes from
/// Resources/IntroVideoConfig. The video covers the whole screen; the loading screen is
/// held back until it ends. Menu music is paused while it plays - only the video's own
/// sound is heard - and a PlayerPrefs flag makes sure it never plays again.
/// </summary>
public class IntroVideo : MonoBehaviour
{
    public const string PlayedKey = "IntroVideoPlayed";

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

        new GameObject("[IntroVideo]").AddComponent<IntroVideo>().Play(config.clip);
    }

    private void Play(VideoClip clip)
    {
        if (MusicPlayer.Instance != null)
            MusicPlayer.Instance.SetPaused(true);

        // The video comes before the loading screen. GameUIManager has already opened
        // it; closing it stops its loading run, which starts over from the beginning
        // when Finish opens it again - and that run then opens the main menu.
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.HideAllPanels();

        target = new RenderTexture((int)clip.width, (int)clip.height, 0);
        BuildOverlay();

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

        player.Play();
    }

    /// <summary>
    /// Black full-screen canvas above every menu canvas, with the video fitted inside
    /// it. It also takes all touches, so nothing behind can be pressed meanwhile.
    /// </summary>
    private void BuildOverlay()
    {
        overlay = new GameObject("IntroVideoCanvas", typeof(RectTransform));
        overlay.transform.SetParent(transform, false);

        var canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32000;
        overlay.AddComponent<GraphicRaycaster>();

        var background = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
        background.transform.SetParent(overlay.transform, false);
        background.color = Color.black;
        LSUITheme.Stretch(background.rectTransform);

        var video = new GameObject("Video", typeof(RectTransform)).AddComponent<RawImage>();
        video.transform.SetParent(overlay.transform, false);
        video.texture = target;
        video.raycastTarget = false;
        LSUITheme.Stretch(video.rectTransform);

        // Letterbox rather than stretch on screens that are not 16:9.
        var fitter = video.gameObject.AddComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
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

        // Video -> loading screen -> main menu.
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.ShowLoadingPanel();

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.Release();
            Destroy(target);
        }
    }
}
