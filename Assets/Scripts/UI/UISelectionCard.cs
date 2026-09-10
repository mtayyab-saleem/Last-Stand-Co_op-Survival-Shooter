using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One selectable card in a popup, e.g. a match mode or a match type.
/// The card owns its own selected / unselected look, so the menu scripts only
/// deal with "which index is chosen" instead of poking at colours themselves.
/// </summary>
[RequireComponent(typeof(Button))]
public class UISelectionCard : MonoBehaviour
{
    [Header("Parts")]
    [SerializeField] private Image background;
    [SerializeField] private Image border;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private Image icon;

    [Header("Idle")]
    [SerializeField] private Color idleBackground = new Color32(0x1A, 0x1E, 0x24, 0xFF);
    [SerializeField] private Color idleBorder = new Color32(0x2E, 0x35, 0x3E, 0xFF);
    [SerializeField] private Color idleTitle = new Color32(0xE6, 0xEC, 0xF2, 0xFF);
    [SerializeField] private Color idleIcon = new Color32(0x9A, 0xA6, 0xB2, 0xFF);

    [Header("Selected")]
    [SerializeField] private Color selectedBackground = new Color32(0x14, 0x33, 0x22, 0xFF);
    [SerializeField] private Color selectedBorder = new Color32(0x38, 0xD4, 0x72, 0xFF);
    [SerializeField] private Color selectedTitle = new Color32(0x38, 0xD4, 0x72, 0xFF);
    [SerializeField] private Color selectedIcon = new Color32(0x38, 0xD4, 0x72, 0xFF);

    private Button button;

    public Button Button
    {
        get
        {
            if (button == null) button = GetComponent<Button>();
            return button;
        }
    }

    public bool IsSelected { get; private set; }

    private void Awake()
    {
        button = GetComponent<Button>();
        SetSelected(IsSelected);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = selected;

        if (background != null) background.color = selected ? selectedBackground : idleBackground;
        if (border != null) border.color = selected ? selectedBorder : idleBorder;
        if (titleText != null) titleText.color = selected ? selectedTitle : idleTitle;
        if (icon != null) icon.color = selected ? selectedIcon : idleIcon;
    }

    public void SetContent(string title, string description)
    {
        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;
    }

    /// <summary>Used by the panel builder so the parts need not be dragged in by hand.</summary>
    public void Bind(Image cardBackground, Image cardBorder, TMP_Text title, TMP_Text description, Image cardIcon)
    {
        background = cardBackground;
        border = cardBorder;
        titleText = title;
        descriptionText = description;
        icon = cardIcon;
    }
}
