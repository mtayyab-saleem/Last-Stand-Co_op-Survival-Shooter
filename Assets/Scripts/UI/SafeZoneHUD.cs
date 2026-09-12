using TMPro;
using UnityEngine;

/// <summary>
/// On-screen safe zone status: what the zone is doing and how long until it changes.
///
/// The countdown is derived from the controller's synced phase end time, so every
/// client shows the same number without any extra network traffic. Builds its own
/// labels at runtime when none are assigned, so no manual UI wiring is needed.
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
    [SerializeField] private float statusFontSize = 22f;
    [SerializeField] private float timerFontSize = 34f;
    [SerializeField] private Vector2 anchoredOffset = new Vector2(0f, -28f);

    [Header("Colours")]
    [SerializeField] private Color zoneColor = new Color32(0x00, 0xD2, 0xFF, 0xFF);
    [SerializeField] private Color warningColor = new Color32(0xFF, 0x6B, 0x4A, 0xFF);

    [Header("Text")]
    [SerializeField] private string shrinkingText = "ZONE SHRINKING";
    [SerializeField] private string waitingText = "NEXT ZONE IN";
    [SerializeField] private string finalText = "FINAL ZONE";
    [SerializeField] private string outsideSuffix = "  -  GET INSIDE";

    private RectTransform builtRoot;

    private void Awake()
    {
        if (statusLabel == null || timerLabel == null)
            BuildLabels();
    }

    private void Update()
    {
        if (zone == null)
        {
            // The zone lives in the gameplay scene, so it appears after a scene load.
            zone = FindFirstObjectByType<SafeZoneController>();

            if (zone == null)
            {
                SetVisible(false);
                return;
            }
        }

        SafeZoneController.ZonePhase phase = zone.Phase;

        if (phase == SafeZoneController.ZonePhase.Idle)
        {
            SetVisible(false);
            return;
        }

        SetVisible(true);

        bool outside = IsLocalPlayerOutside();
        Color tint = outside ? warningColor : zoneColor;

        if (statusLabel != null)
        {
            string label =
                phase == SafeZoneController.ZonePhase.Shrinking ? shrinkingText :
                phase == SafeZoneController.ZonePhase.Final ? finalText : waitingText;

            int total = zone.TotalCases;
            if (total > 0 && phase != SafeZoneController.ZonePhase.Final)
                label += "   " + Mathf.Clamp(zone.CurrentCaseIndex + 1, 1, total) + "/" + total;

            if (outside)
                label += outsideSuffix;

            statusLabel.text = label;
            statusLabel.color = tint;
        }

        if (timerLabel != null)
        {
            // The final stage never ends, so a countdown there would be meaningless.
            timerLabel.text = phase == SafeZoneController.ZonePhase.Final
                ? string.Empty
                : FormatTime(zone.SecondsRemaining);

            timerLabel.color = tint;
        }
    }

    private bool IsLocalPlayerOutside()
    {
        LSPlayer local = LSPlayer.LocalInstance;

        if (local == null || zone == null)
            return false;

        return !zone.IsInsideZone(local.transform.position);
    }

    private static string FormatTime(float seconds)
    {
        int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
        return (whole / 60).ToString("0") + ":" + (whole % 60).ToString("00");
    }

    private void SetVisible(bool visible)
    {
        if (builtRoot != null && builtRoot.gameObject.activeSelf != visible)
            builtRoot.gameObject.SetActive(visible);

        if (builtRoot == null)
        {
            if (statusLabel != null) statusLabel.enabled = visible;
            if (timerLabel != null) timerLabel.enabled = visible;
        }
    }

    /// <summary>Top-centre status block, built so the HUD needs no manual setup.</summary>
    private void BuildLabels()
    {
        Canvas canvas = GetComponentInParent<Canvas>();

        if (canvas == null)
        {
            Debug.LogWarning("[SafeZoneHUD] No Canvas in parents; labels cannot be built.");
            return;
        }

        var rootGo = new GameObject("SafeZoneStatus", typeof(RectTransform));
        rootGo.transform.SetParent(transform, false);

        builtRoot = (RectTransform)rootGo.transform;
        builtRoot.anchorMin = new Vector2(0.5f, 1f);
        builtRoot.anchorMax = new Vector2(0.5f, 1f);
        builtRoot.pivot = new Vector2(0.5f, 1f);
        builtRoot.anchoredPosition = anchoredOffset;
        builtRoot.sizeDelta = new Vector2(760f, 100f);

        statusLabel = CreateLabel("Status", builtRoot, 0f, 34f, statusFontSize);
        statusLabel.characterSpacing = 6f;

        timerLabel = CreateLabel("Timer", builtRoot, -34f, 52f, timerFontSize);
    }

    private TMP_Text CreateLabel(string name, RectTransform parent, float y, float height, float size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(0f, height);

        var text = go.AddComponent<TextMeshProUGUI>();
        if (font != null) text.font = font;
        text.fontSize = size;
        text.alignment = TextAlignmentOptions.Center;
        text.color = zoneColor;
        text.raycastTarget = false;
        text.text = string.Empty;

        return text;
    }
}
