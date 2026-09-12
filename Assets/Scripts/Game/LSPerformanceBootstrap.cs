using UnityEngine;

/// <summary>
/// Applies runtime performance limits at startup, before any scene loads.
///
/// Without a frame-rate cap Android renders as fast as the GPU allows. On a
/// 90/120 Hz phone that burns power for frames nobody asked for, the device heats
/// up, and thermal throttling produces the intermittent stutter that reads as a
/// framerate bug. Capping costs nothing and keeps the frame time stable.
/// </summary>
public static class LSPerformanceBootstrap
{
    /// <summary>Frame cap used on phones and tablets.</summary>
    private const int MobileTargetFrameRate = 60;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        // targetFrameRate is only honoured while vSync is off.
        QualitySettings.vSyncCount = 0;

        // The cap is deliberately NOT applied in the editor. Capping there hides how
        // much headroom the game actually has, and editor FPS is not comparable to a
        // phone anyway. On a real device the cap is what stops the GPU racing to 120
        // fps, overheating, and then thermal-throttling into stutter.
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Application.targetFrameRate = MobileTargetFrameRate;
#else
        // Editor and desktop: uncapped, so profiling shows the real frame cost.
        Application.targetFrameRate = -1;
#endif

        // Physics rate. JUTPS's camera library used to force this to 0.015 (66.7 Hz)
        // from its own Start, which is why an earlier attempt at this never stuck;
        // that line is gone, so this is now the single owner of the value.
        // Remote characters are kinematic and the only real rigidbodies are bullets
        // and pickups, so mobile does not need more than 40 Hz.
#if UNITY_ANDROID || UNITY_IOS
        Time.fixedDeltaTime = 1f / 40f;
#else
        Time.fixedDeltaTime = 1f / 50f;
#endif
    }
}
