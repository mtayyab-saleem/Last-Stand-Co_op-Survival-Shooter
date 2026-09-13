using JUTPS.GameSettings;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies the player's saved settings at startup and on every scene load,
/// independent of the settings panel.
///
/// Two separate things used to stop saved settings taking effect until the panel
/// was opened:
///
/// 1. The panel applied them from its own Awake, but the panel is authored
///    disabled in the scene, so Unity never ran that Awake.
/// 2. AudioMixer exposed parameters accept a value on the first frame and then get
///    reset again as Unity's audio system finishes starting up, so one successful
///    SetFloat is not enough.
///
/// The mixer is therefore verified on a slow timer and re-applied only when it has
/// drifted - one GetFloat every half second, and a write only when something
/// actually reset it.
/// </summary>
[DisallowMultipleComponent]
public class LSSettingsRuntime : MonoBehaviour
{
    /// <summary>How often the mixer is checked against the saved values.</summary>
    private const float VerifyInterval = 0.5f;

    private static LSSettingsRuntime instance;
    private float nextVerifyTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (instance != null)
            return;

        GameObject host = new GameObject("[LSSettingsRuntime]");
        DontDestroyOnLoad(host);

        // Awake assigns the instance itself. AddComponent runs Awake synchronously,
        // before this assignment would complete, so anything Awake does that needs
        // the singleton has to not depend on the return value of AddComponent.
        host.AddComponent<LSSettingsRuntime>();
    }

    private void Awake()
    {
        instance = this;

        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;

        RequestApply();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        if (instance == this)
            instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // A new scene brings a new camera and new audio sources.
        RequestApply();
    }

    /// <summary>Applies everything now, and schedules the next mixer verification.</summary>
    public static void RequestApply()
    {
        // Camera sensitivity / invert are PlayerPrefs-backed; JUApplyCameraSettings reads
        // them whenever this event fires.
        //
        // Deliberately NOT JUGameSettings.ApplySettings(): that also re-applies JUTPS's
        // render scale and quality level. Render scale calls Screen.SetResolution from
        // Screen.resolutions, which on Android is the portrait panel size - the game then
        // rendered into a 75% portrait buffer stretched over a landscape screen (blurry,
        // stretched UI, popups cut off). And it forced quality level 1 ("PC") on phones.
        // Neither setting is exposed in this game, so the project defaults must stand.
        JUGameSettings.OnApplySettings?.Invoke();

        LSAudioSettings.Apply();

        if (instance != null)
            instance.nextVerifyTime = Time.unscaledTime + VerifyInterval;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextVerifyTime)
            return;

        nextVerifyTime = Time.unscaledTime + VerifyInterval;

        // One GetFloat twice a second. Only writes when the mixer has drifted, so
        // this self-heals the startup reset without fighting the audio system.
        if (!LSAudioSettings.IsApplied())
            LSAudioSettings.Apply();
    }
}
