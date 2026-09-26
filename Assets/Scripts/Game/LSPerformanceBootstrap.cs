using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Applies runtime performance limits at startup, before any scene loads.
///
/// Without a frame-rate cap Android renders as fast as the GPU allows. On a
/// 90/120 Hz phone that burns power for frames nobody asked for, the device heats
/// up, and thermal throttling produces the intermittent stutter that reads as a
/// framerate bug. Capping costs nothing and keeps the frame time stable.
///
/// Phones are split into two tiers. A low-end phone runs a steady 45 fps instead of
/// struggling for 60, and draws less: shorter forest and fog, cheaper terrain, lower
/// LODs. On every phone the render resolution follows the frame rate (see
/// DynamicResolution), so a GPU that cannot keep up renders fewer pixels.
/// </summary>
public static class LSPerformanceBootstrap
{
    public enum Tier { Low, High }

    /// <summary>PlayerPrefs override: 0 = Low, 1 = High, missing = detect from the device.</summary>
    public const string TierKey = "LSGraphicsTier";

    public static Tier CurrentTier { get; private set; }

    // High keeps what the scenes were authored with; Low trims what costs most on a
    // weak GPU - leaves (alpha-tested, so every overlap is paid for) and terrain.
    private const int HighFrameRate = 60;
    private const int LowFrameRate = 45;
    private const float HighRenderScale = 0.8f, HighMinRenderScale = 0.6f;
    private const float LowRenderScale = 0.7f, LowMinRenderScale = 0.5f;
    private const float LowForestDistance = 120f;
    private const float LowFogStart = 35f, LowFogEnd = 150f;
    private const float LowLodBias = 0.7f;
    private const float LowTerrainBasemapDistance = 60f;
    private const float LowTerrainPixelError = 20f;
    private const float LowPropCullDistance = 40f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Apply()
    {
        CurrentTier = PlayerPrefs.HasKey(TierKey) ? (Tier)Mathf.Clamp(PlayerPrefs.GetInt(TierKey), 0, 1) : DetectTier();
        bool low = CurrentTier == Tier.Low;

        // targetFrameRate is only honoured while vSync is off.
        QualitySettings.vSyncCount = 0;

        // On a device the cap is what stops the GPU racing to 120 fps, overheating, and
        // then thermal-throttling into stutter. The editor (with the phone as build
        // target) is capped too: characters move on physics steps, and at an uncapped
        // ~140 fps they moved on some frames and stood still on others, which felt like
        // lag. The Profiler still shows the real cost of each frame.
        int frameRate = low ? LowFrameRate : HighFrameRate;
#if UNITY_ANDROID || UNITY_IOS
        Application.targetFrameRate = frameRate;
#else
        Application.targetFrameRate = -1;
#endif

        // A slow frame may run at most this much physics to catch up. Unity's default
        // (1/3 s) lets a single hitch run up to 20 physics steps in the next frame,
        // which makes that frame slow too - on a phone that snowballs into seconds of
        // stutter. Past this the game briefly runs slower instead.
        Time.maximumDeltaTime = 0.1f;

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
        Time.fixedDeltaTime = 1f / frameRate;
#else
        Time.fixedDeltaTime = 1f / 50f;
#endif

        if (low)
        {
            QualitySettings.lodBias = LowLodBias;
            CharacterPropCulling.CullDistance = LowPropCullDistance;
            SceneManager.sceneLoaded += (scene, mode) => ApplyLowTierToScene();
        }

        StartDynamicResolution(low ? LowRenderScale : HighRenderScale, low ? LowMinRenderScale : HighMinRenderScale);
    }

    /// <summary>
    /// Old or small phones: little memory, few cores, or a GPU from an entry-level family.
    /// The editor counts as High; set the TierKey PlayerPref to test Low there.
    /// </summary>
    private static Tier DetectTier()
    {
#if UNITY_EDITOR
        return Tier.High;
#else
        // A "4 GB" phone reports about 3.6 GB.
        if (SystemInfo.systemMemorySize < 3400 || SystemInfo.processorCount <= 4)
            return Tier.Low;

        string gpu = SystemInfo.graphicsDeviceName.ToLowerInvariant();

        if (gpu.Contains("mali-4") || gpu.Contains("mali-t") || gpu.Contains("mali-g31") ||
            gpu.Contains("mali-g51") || gpu.Contains("mali-g52") || gpu.Contains("powervr"))
            return Tier.Low;

        // Adreno 3xx/4xx and the budget 5xx parts (505 - 512).
        int adreno = gpu.IndexOf("adreno");
        if (adreno >= 0)
        {
            string digits = System.Text.RegularExpressions.Regex.Match(gpu.Substring(adreno), @"\d{3}").Value;
            if (int.TryParse(digits, out int model) && model < 530)
                return Tier.Low;
        }

        return Tier.High;
#endif
    }

    /// <summary>Scene lighting and terrain are per scene, so this runs on every load.</summary>
    private static void ApplyLowTierToScene()
    {
        // Only the battle map has the forest; the lobby and menu keep their look.
        ForestRenderer forest = Object.FindAnyObjectByType<ForestRenderer>();

        if (forest == null)
            return;

        forest.drawDistance = Mathf.Min(forest.drawDistance, LowForestDistance);

        // Pulled in with the forest so the shorter tree cut-off still hides in the fog.
        if (RenderSettings.fog && RenderSettings.fogMode == FogMode.Linear)
        {
            RenderSettings.fogStartDistance = Mathf.Min(RenderSettings.fogStartDistance, LowFogStart);
            RenderSettings.fogEndDistance = Mathf.Min(RenderSettings.fogEndDistance, LowFogEnd);
        }

        foreach (Terrain terrain in Terrain.activeTerrains)
        {
            // Past the basemap distance the terrain is one pre-blended texture instead
            // of three blended layers with normal maps.
            terrain.basemapDistance = Mathf.Min(terrain.basemapDistance, LowTerrainBasemapDistance);
            terrain.heightmapPixelError = Mathf.Max(terrain.heightmapPixelError, LowTerrainPixelError);
        }
    }

    private static void StartDynamicResolution(float startScale, float minScale)
    {
        if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp))
            return;

        var runner = new GameObject("[DynamicResolution]");
        runner.hideFlags = HideFlags.HideInHierarchy;
        Object.DontDestroyOnLoad(runner);
        runner.AddComponent<DynamicResolution>().Init(urp, startScale, minScale);
    }

    /// <summary>
    /// Lowers the render resolution in steps while the game runs under its frame cap
    /// because of the GPU, and raises it back once there is headroom. A CPU-bound
    /// frame is left alone: fewer pixels would only blur the picture.
    /// </summary>
    private class DynamicResolution : MonoBehaviour
    {
        private const float Step = 0.05f;
        private const float Window = 2f;          // seconds per measurement
        private const float CalmBeforeRaise = 10f;

        private UniversalRenderPipelineAsset urp;
        private float authoredScale, maxScale, minScale;
        private float elapsed, longestFrame, calm, lastRaise = -100f;
        private int frames;
        private readonly FrameTiming[] timing = new FrameTiming[1];

        public void Init(UniversalRenderPipelineAsset asset, float startScale, float min)
        {
            urp = asset;
            authoredScale = asset.renderScale;
            maxScale = startScale;
            minScale = min;
            urp.renderScale = startScale;
        }

        private void Update()
        {
            FrameTimingManager.CaptureFrameTimings();

            elapsed += Time.unscaledDeltaTime;
            longestFrame = Mathf.Max(longestFrame, Time.unscaledDeltaTime);
            frames++;

            if (elapsed < Window)
                return;

            float fps = frames / elapsed;
            bool hitch = longestFrame > 0.25f;
            elapsed = 0f;
            longestFrame = 0f;
            frames = 0;

            // A scene load or a one-off hitch is not the GPU running short.
            if (hitch)
                return;

            float target = Application.targetFrameRate > 0 ? Application.targetFrameRate : 60f;
            float scale = urp.renderScale;

            if (fps < target * 0.9f)
            {
                calm = 0f;

                if (scale > minScale + 0.001f && GpuIsTheLimit())
                {
                    // Too slow right after going up: that step does not fit, stay below it.
                    if (Time.unscaledTime - lastRaise < Window * 3f)
                        maxScale = scale - Step;

                    urp.renderScale = Mathf.Max(minScale, scale - Step);
                }
            }
            else if (fps > target * 0.97f)
            {
                calm += Window;

                if (calm >= CalmBeforeRaise && scale < maxScale - 0.001f)
                {
                    calm = 0f;
                    lastRaise = Time.unscaledTime;
                    urp.renderScale = Mathf.Min(maxScale, scale + Step);
                }
            }
            else
            {
                calm = 0f;
            }
        }

        private bool GpuIsTheLimit()
        {
            // No GPU timing on this device: resolution is the only lever there is.
            if (FrameTimingManager.GetLatestTimings(1, timing) < 1 || timing[0].gpuFrameTime <= 0)
                return true;

            return timing[0].gpuFrameTime >= timing[0].cpuMainThreadFrameTime * 0.85;
        }

        // The pipeline asset is a project file: in the editor a runtime change would be
        // saved into it.
        private void OnApplicationQuit()
        {
            if (urp != null)
                urp.renderScale = authoredScale;
        }
    }
}
