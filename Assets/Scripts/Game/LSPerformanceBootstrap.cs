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
        // On phones it matches the frame cap. JUTPS moves the player (and bots on the
        // host) through its Rigidbody, so they only move on physics steps; at 40 Hz
        // under a 60 fps cap that was two frames out of three - the jerky sprint.
        // Rigidbody interpolation cannot fix it: JUTPS also turns the character by
        // writing its Transform every frame, which cancels interpolation and made the
        // turn jitter. One step per frame keeps both movement and turning smooth.
#if UNITY_ANDROID || UNITY_IOS
        Time.fixedDeltaTime = 1f / MobileTargetFrameRate;
#else
        Time.fixedDeltaTime = 1f / 50f;
#endif
    }
}
