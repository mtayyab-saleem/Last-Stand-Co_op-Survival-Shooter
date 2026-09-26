using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The match status bar at the top of the screen: how many players are still alive out
/// of how many started, what the safe zone is doing, how long until it changes, and a
/// warning with the distance back when the local player is outside it.
///
/// Everything is read from synced state - MatchTracker's alive/total counts and the
/// zone controller's phase end time - so every client shows the same numbers without
/// any extra network traffic. The bar builds itself at runtime; nothing to wire up.
/// </summary>
[DisallowMultipleComponent]
public class SafeZoneHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Found automatically when left empty.")]
    [SerializeField] private SafeZoneController zone;

    [Tooltip("Optional. Created at runtime when empty.")]
    [SerializeField] private TMP_Text statusLabel;
    [SerializeField] private TMP_Text timerLabel;

    [Header("Style")]
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float statusFontSize = 17f;
    [SerializeField] private float timerFontSize = 38f;
    [SerializeField] private Vector2 anchoredOffset = new Vector2(0f, -18f);

    [Header("Colours")]
    [SerializeField] private Color zoneColor = new Color32(0x3D, 0x9B, 0xFF, 0xFF);
    [SerializeField] private Color warningColor = new Color32(0xFF, 0x5A, 0x4A, 0xFF);

    [Header("Text")]
    [SerializeField] private string shrinkingText = "ZONE SHRINKING";
    [SerializeField] private string waitingText = "NEXT ZONE IN";
    [SerializeField] private string finalText = "FINAL ZONE";
    [SerializeField] private string outsideSuffix = "GET INSIDE";

    private const float ChipHeight = 84f;
    private const float AliveWidth = 150f;
    private const float ZoneWidth = 360f;
    private const float Gap = 10f;

    private static readonly Color ChipColor = new Color(0.035f, 0.047f, 0.063f, 0.9f);

    private RectTransform builtRoot;
    private RectTransform aliveChip;
    private RectTransform zoneChip;
    private TMP_Text aliveLabel;
    private Image zoneAccent;
    private RectTransform progressFill;
    private Image progressImage;

    // Length of the phase being counted down, so the bar can show how much is left.
    private SafeZoneController.ZonePhase trackedPhase;
    private int trackedCase = int.MinValue;
    private float phaseLength = 1f;

    private void Awake()
    {
        if (statusLabel == null || timerLabel == null)
            BuildBar();
    }

    private void Update()
    {
        bool aliveShown = UpdateAliveChip();
        bool zoneShown = UpdateZoneChip();

        if (builtRoot != null)
        {
            bool show = aliveShown || zoneShown;
            if (builtRoot.gameObject.activeSelf != show)
                builtRoot.gameObject.SetActive(show);
        }
    }

    // -------------------------
    // Alive count
    // -------------------------

    private bool UpdateAliveChip()
    {
        MatchTracker tracker = MatchTracker.Instance;
        bool show = tracker != null && tracker.TrackingActive && !tracker.MatchEnded && tracker.TotalPlayers > 0;

        SetActive(aliveChip, show);

        // Texts are only rebuilt when what they show changes: a new string every frame
        // is garbage the phone has to collect.
        if (show && aliveLabel != null && Changed(ref shownAlive, tracker.AlivePlayers * 1000 + tracker.TotalPlayers))
            aliveLabel.text = $"{tracker.AlivePlayers}<size=72%><color=#8A96A3> / {tracker.TotalPlayers}</color></size>";

        return show;
    }

    // -------------------------
    // Safe zone
    // -------------------------

    private bool UpdateZoneChip()
    {
        if (zone == null)
        {
            // The zone lives in the gameplay scene, so it appears after a scene load.
            zone = FindFirstObjectByType<SafeZoneController>();
        }

        SafeZoneController.ZonePhase phase = zone != null ? zone.Phase : SafeZoneController.ZonePhase.Idle;
        bool show = phase != SafeZoneController.ZonePhase.Idle && phase != SafeZoneController.ZonePhase.Ended;

        SetActive(zoneChip, show);
        if (statusLabel != null && builtRoot == null) statusLabel.enabled = show;
        if (timerLabel != null && builtRoot == null) timerLabel.enabled = show;

        if (!show)
            return false;

        float remaining = zone.SecondsRemaining;

        if (phase != trackedPhase || zone.CurrentCaseIndex != trackedCase)
        {
            trackedPhase = phase;
            trackedCase = zone.CurrentCaseIndex;
            phaseLength = Mathf.Max(remaining, 0.01f);
        }

        float outsideBy = DistanceOutsideZone();
        bool outside = outsideBy > 0f;
        Color tint = outside ? warningColor : zoneColor;

        int statusKey = outside ? 100000 + Mathf.CeilToInt(outsideBy) : (int)phase * 1000 + zone.CurrentCaseIndex;

        if (statusLabel != null && Changed(ref shownStatus, statusKey))
        {
            string label;

            if (outside)
            {
                label = $"{outsideSuffix}  ·  {Mathf.CeilToInt(outsideBy)} M";
            }
            else
            {
                label =
                    phase == SafeZoneController.ZonePhase.Shrinking ? shrinkingText :
                    phase == SafeZoneController.ZonePhase.Final ? finalText : waitingText;

                int total = zone.TotalCases;
                if (total > 0 && phase != SafeZoneController.ZonePhase.Final)
                    label += "  ·  " + Mathf.Clamp(zone.CurrentCaseIndex + 1, 1, total) + "/" + total;
            }

            statusLabel.text = label;
        }

        if (statusLabel != null)
            statusLabel.color = tint;

        if (timerLabel != null)
        {
            // The final stage never ends, so a countdown there would be meaningless.
            int timerKey = phase == SafeZoneController.ZonePhase.Final ? -1 : Mathf.CeilToInt(Mathf.Max(0f, remaining));
            if (Changed(ref shownTimer, timerKey))
                timerLabel.text = timerKey < 0 ? "--:--" : FormatTime(remaining);
            timerLabel.color = outside ? warningColor : Color.white;
        }

        if (zoneAccent != null)
            zoneAccent.color = tint;

        if (progressFill != null)
        {
            float left = phase == SafeZoneController.ZonePhase.Final ? 1f : Mathf.Clamp01(remaining / phaseLength);
            progressFill.anchorMax = new Vector2(left, 1f);
            progressImage.color = tint;
        }

        return true;
    }

    /// <summary>Metres the local player has to go to get back inside; 0 when inside.</summary>
    private float DistanceOutsideZone()
    {
        LSPlayer local = LSPlayer.LocalInstance;

        if (local == null || zone == null || !local.isAlive)
            return 0f;

        Vector3 centre = zone.ZoneCenter;
        Vector3 position = local.transform.position;
        float fromCentre = new Vector2(position.x - centre.x, position.z - centre.z).magnitude;

        return Mathf.Max(0f, fromCentre - zone.CurrentRadius);
    }

    private static string FormatTime(float seconds)
    {
        int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (whole / 60).ToString("0") + ":" + (whole % 60).ToString("00");
    }

    // Last values the labels were built from; int.MinValue forces the first build.
    private int shownAlive = int.MinValue, shownStatus = int.MinValue, shownTimer = int.MinValue;

    private static bool Changed(ref int shown, int value)
    {
        if (shown == value)
            return false;

        shown = value;
        return true;
    }

    private static void SetActive(Component target, bool active)
    {
        if (target != null && target.gameObject.activeSelf != active)
            target.gameObject.SetActive(active);
    }

    // -------------------------
    // Layout
    // -------------------------

    /// <summary>
    /// Two dark chips hanging from the top centre, clear of the settings button on the
    /// left and the health and weapon panel on the right:
    /// [ 7 / 8  ALIVE ] [ NEXT ZONE IN · 1/4        1:25 ]
    /// </summary>
    private void BuildBar()
    {
        if (GetComponentInParent<Canvas>() == null)
        {
            Debug.LogWarning("[SafeZoneHUD] No Canvas in parents; the status bar cannot be built.");
            return;
        }

        builtRoot = new GameObject("MatchStatusBar", typeof(RectTransform)).GetComponent<RectTransform>();
        builtRoot.SetParent(transform, false);
        float width = AliveWidth + Gap + ZoneWidth;
        LSUITheme.Place(builtRoot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), anchoredOffset,
                        new Vector2(width, ChipHeight), new Vector2(0.5f, 1f));

        // Alive chip
        aliveChip = LSUITheme.Panel("Alive", builtRoot, ChipColor);
        LSUITheme.Place(aliveChip, new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero,
                        new Vector2(AliveWidth, ChipHeight), new Vector2(0f, 1f));

        RectTransform aliveAccent = LSUITheme.Panel("Accent", aliveChip, LSUITheme.Accent);
        LSUITheme.Place(aliveAccent, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f), new Vector2(0f, 0.5f));

        aliveLabel = LSUITheme.Label("Count", aliveChip, font, "-", timerFontSize + 4f, 0f, LSUITheme.Text, TextAlignmentOptions.Center);
        LSUITheme.Place(aliveLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(2f, -4f),
                        new Vector2(-4f, 50f), new Vector2(0.5f, 1f));

        TMP_Text aliveCaption = LSUITheme.Label("Caption", aliveChip, font, "ALIVE", statusFontSize - 2f, 5f, LSUITheme.Muted, TextAlignmentOptions.Center);
        LSUITheme.Place(aliveCaption.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(2f, 8f),
                        new Vector2(-4f, 20f), new Vector2(0.5f, 0f));

        // Zone chip
        zoneChip = LSUITheme.Panel("Zone", builtRoot, ChipColor);
        LSUITheme.Place(zoneChip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(AliveWidth + Gap, 0f),
                        new Vector2(ZoneWidth, ChipHeight), new Vector2(0f, 1f));

        zoneAccent = LSUITheme.Panel("Accent", zoneChip, zoneColor).GetComponent<Image>();
        LSUITheme.Place(zoneAccent.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero, new Vector2(4f, 0f), new Vector2(0f, 0.5f));

        statusLabel = LSUITheme.Label("Status", zoneChip, font, string.Empty, statusFontSize, 3f, zoneColor, TextAlignmentOptions.Left);
        LSUITheme.Place(statusLabel.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -10f),
                        new Vector2(-36f, 24f), new Vector2(0f, 1f));

        timerLabel = LSUITheme.Label("Timer", zoneChip, font, string.Empty, timerFontSize, 2f, Color.white, TextAlignmentOptions.Left);
        LSUITheme.Place(timerLabel.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(20f, 10f),
                        new Vector2(-36f, 46f), new Vector2(0f, 0f));

        // Time left in the current phase, along the bottom edge.
        RectTransform track = LSUITheme.Panel("Progress", zoneChip, new Color(1f, 1f, 1f, 0.08f));
        LSUITheme.Place(track, new Vector2(0f, 0f), new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, 4f), new Vector2(0.5f, 0f));

        progressFill = LSUITheme.Panel("Fill", track, zoneColor);
        progressFill.anchorMin = Vector2.zero;
        progressFill.anchorMax = Vector2.one;
        progressFill.offsetMin = progressFill.offsetMax = Vector2.zero;
        progressImage = progressFill.GetComponent<Image>();

        builtRoot.gameObject.SetActive(false);
    }
}
