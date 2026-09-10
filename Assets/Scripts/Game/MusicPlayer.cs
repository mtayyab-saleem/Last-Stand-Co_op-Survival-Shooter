using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// One persistent music source for the whole game.
///
/// It builds itself at startup from Resources/MusicLibrary, so no scene needs a
/// music object wired into it, and it survives scene loads. Only one clip is ever
/// streaming at a time: the old track fades out before the new one fades in, which
/// keeps memory and decode cost low on phones.
///
/// Output goes to the mixer's Music group, so the Music slider and on/off switch
/// in the settings panel already control it.
/// </summary>
[DisallowMultipleComponent]
public class MusicPlayer : MonoBehaviour
{
    private static MusicPlayer instance;

    private MusicLibrarySO library;
    private AudioSource source;
    private Coroutine fadeRoutine;
    private AudioClip currentClip;

    public static MusicPlayer Instance { get { return instance; } }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null)
            return;

        MusicLibrarySO libraryAsset = Resources.Load<MusicLibrarySO>("MusicLibrary");

        if (libraryAsset == null)
        {
            Debug.LogWarning("[MusicPlayer] No MusicLibrary found in Resources. Music is disabled.");
            return;
        }

        GameObject host = new GameObject("[MusicPlayer]");
        DontDestroyOnLoad(host);

        instance = host.AddComponent<MusicPlayer>();
        instance.Initialise(libraryAsset);
    }

    private void Initialise(MusicLibrarySO libraryAsset)
    {
        library = libraryAsset;

        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = library.loop;
        source.spatialBlend = 0f;   // 2D, never positional
        source.volume = 0f;

        AudioMixer mixer = LSAudioSettings.Mixer;

        if (mixer != null)
        {
            AudioMixerGroup[] groups = mixer.FindMatchingGroups("Music");

            if (groups.Length > 0)
                source.outputAudioMixerGroup = groups[0];
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        PlayForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayForScene(scene.name);
    }

    /// <summary>Switches to the track configured for a scene, if it is not already playing.</summary>
    public void PlayForScene(string sceneName)
    {
        if (library == null)
            return;

        AudioClip clip = library.GetClip(sceneName);

        // Menu and Lobby share a track: leave it rolling rather than restarting.
        if (clip == currentClip)
            return;

        currentClip = clip;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(SwapTrack(clip));
    }

    private IEnumerator SwapTrack(AudioClip clip)
    {
        float fade = Mathf.Max(0.01f, library.fadeSeconds);

        // Unscaled time, so a fade still runs if anything ever pauses the game.
        if (source.isPlaying)
        {
            float from = source.volume;

            for (float elapsed = 0f; elapsed < fade; elapsed += Time.unscaledDeltaTime)
            {
                source.volume = Mathf.Lerp(from, 0f, elapsed / fade);
                yield return null;
            }

            source.Stop();
        }

        source.volume = 0f;

        if (clip == null)
        {
            source.clip = null;
            fadeRoutine = null;
            yield break;
        }

        source.clip = clip;
        source.loop = library.loop;
        source.Play();

        float target = Mathf.Clamp01(library.trackVolume);

        for (float elapsed = 0f; elapsed < fade; elapsed += Time.unscaledDeltaTime)
        {
            source.volume = Mathf.Lerp(0f, target, elapsed / fade);
            yield return null;
        }

        source.volume = target;
        fadeRoutine = null;
    }
}
