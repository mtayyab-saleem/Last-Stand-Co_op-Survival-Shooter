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
        // Camera sensitivity / invert are PlayerPrefs-backed and apply immediately.
        // Deliberately called once per request, not per frame: JUGameSettings.
        // ApplySettings also calls Screen.SetResolution, which must not run in a loop.
        JUGameSettings.ApplySettings();

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
