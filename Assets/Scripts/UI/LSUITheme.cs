using Michsky.MUIP;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// The look of the game's screens in one place: the palette of the Host, Join and
/// Settings popups, plus the small builders used to put a screen together in code.
///
/// Runtime-built screens keep layout and wiring in the script instead of a hand-made
/// hierarchy, which is why the lobby and the match result look the same without anyone
/// having to keep two scenes in sync by hand.
/// </summary>
public static class LSUITheme
{
    public static readonly Color Dim = new Color(0f, 0f, 0f, 0.82f);
    public static readonly Color Window = new Color(0.051f, 0.063f, 0.078f, 0.98f);
    public static readonly Color BoxBorder = new Color(0.145f, 0.173f, 0.208f, 1f);
    public static readonly Color BoxInner = new Color(0.063f, 0.078f, 0.098f, 1f);
    public static readonly Color Text = new Color(0.941f, 0.957f, 0.973f, 1f);
    public static readonly Color Muted = new Color(0.467f, 0.518f, 0.569f, 1f);
    public static readonly Color Accent = new Color(0.22f, 0.831f, 0.447f, 1f);
    public static readonly Color ButtonGreen = new Color(0.22f, 0.729f, 0.447f, 1f);
    public static readonly Color ButtonIdle = new Color(0.165f, 0.196f, 0.235f, 1f);
    public static readonly Color Danger = new Color(0.937f, 0.267f, 0.267f, 1f);

    private static readonly Color[] Teams =
    {
        new Color(0.22f, 0.831f, 0.447f, 1f),   // T1 green
        new Color(0.247f, 0.663f, 0.961f, 1f),  // T2 blue
        new Color(1f, 0.624f, 0.263f, 1f),      // T3 orange
        new Color(0.651f, 0.42f, 1f, 1f)        // T4 purple
    };

    /// <summary>Colour of a team, or the muted grey of "no team yet".</summary>
    public static Color TeamColor(int teamId)
    {
        if (teamId <= 0 || teamId > Teams.Length)
            return Muted;

        return Teams[teamId - 1];
    }

    // -------------------------
    // Building blocks
    // -------------------------

    /// <summary>Screen-space canvas scaled like every other canvas in the project.</summary>
    public static RectTransform Canvas(string name, Transform parent, int sortingOrder)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        go.AddComponent<GraphicRaycaster>();

        return (RectTransform)go.transform;
    }

    public static RectTransform Panel(string name, Transform parent, Color color, bool raycastTarget = false)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return (RectTransform)go.transform;
    }

    public static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string text,
                                        float size, float spacing, Color color, TextAlignmentOptions alignment)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var label = go.AddComponent<TextMeshProUGUI>();
        if (font != null) label.font = font;
        label.text = text;
        label.fontSize = size;
        label.characterSpacing = spacing;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;

        return label;
    }

    /// <summary>
    /// A MUIP rounded button in the project's colours.
    ///
    /// MUIP Manager has dynamic update switched on, so UIManagerButton repaints every
    /// button in the theme colour each frame; the Host popup's buttons opt out the same
    /// way, otherwise the colour below would be gone by the next frame.
    /// </summary>
    public static ButtonManager Button(GameObject prefab, Transform parent, string name, string text,
                                       Sprite icon, Color color, UnityAction onClick)
    {
        if (prefab == null)
        {
            Debug.LogWarning("[LSUITheme] No MUIP button prefab assigned for " + name + ".");
            return null;
        }

        GameObject go = Object.Instantiate(prefab, parent, false);
        go.name = name;

        if (go.TryGetComponent(out UIManagerButton theme))
            theme.overrideColors = true;

        Transform background = go.transform.Find("Normal/Background");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
            backgroundImage.color = color;

        foreach (Image image in go.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Icon")
                image.color = Color.white;
        }

        ButtonManager button = go.GetComponent<ButtonManager>();

        if (button != null)
        {
            button.buttonText = text;
            button.buttonIcon = icon;
            button.enableIcon = icon != null;

            if (onClick != null)
            {
                button.onClick.RemoveListener(onClick);
                button.onClick.AddListener(onClick);
            }

            if (button.normalText != null) button.normalText.color = Color.white;
            if (button.highlightedText != null) button.highlightedText.color = Color.white;

            button.UpdateUI();
        }

        return button;
    }

    /// <summary>Recolours a button already built by <see cref="Button"/>.</summary>
    public static void SetButtonColor(ButtonManager button, Color color)
    {
        if (button == null)
            return;

        Transform background = button.transform.Find("Normal/Background");

        if (background != null && background.TryGetComponent(out Image backgroundImage))
            backgroundImage.color = color;
    }

    // -------------------------
    // Placement
    // -------------------------

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    public static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
                             Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>Full-width row hanging from the parent's top edge, with an optional side margin.</summary>
    public static void PlaceTop(RectTransform rect, float y, float height, float sideMargin = 0f)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-2f * sideMargin, height);
    }

    /// <summary>
    /// A column pinned to one screen edge that stretches with the screen height. Phones
    /// are far shorter than the 1080 reference once the canvas matches on width, so a
    /// fixed height would either overflow or leave a hole.
    /// </summary>
    public static void PlaceSideColumn(RectTransform rect, float width, float fromRight, float topInset, float bottomInset)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.offsetMin = new Vector2(-width - fromRight, bottomInset);
        rect.offsetMax = new Vector2(-fromRight, -topInset);
    }

    /// <summary>
    /// The scene's own EventSystem can be switched off (the JUTPS HUD carries it and is
    /// hidden while there is no local character), and buttons need one to receive taps.
    /// </summary>
    public static void EnsureEventSystem()
    {
        // The JUTPS UI brings its own EventSystem, but UISafetyWrapper keeps that UI
        // switched off until the local player spawns, so on the lobby's first frame it
        // was not current. A second one was made, and Unity then warned about two event
        // systems every frame. One that is merely waiting to be switched on counts.
        if (EventSystem.current != null ||
            Object.FindAnyObjectByType<EventSystem>(FindObjectsInactive.Include) != null)
            return;

        new GameObject("EventSystem (LS UI)", typeof(EventSystem), typeof(StandaloneInputModule));
    }
}
