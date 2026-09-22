using System.Collections;
using JUTPS;
using JUTPS.CameraSystems;
using Michsky.MUIP;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// End-of-match and elimination screen.
///
/// Two moments bring it up, and it upgrades from the first to the second in place:
///   - the local player is eliminated while the match continues,
///   - MatchTracker decides the match (last player in Solo, last team in Duo / Squad).
///
/// Either way the player is sent back to the main menu by a countdown, or by the button
/// as soon as they want. The one exception is a dead host in a running match: leaving
/// shuts the server down, so the countdown only starts once the match is over.
///
/// Built in the same style as the Host, Join and Settings popups - dimmed backdrop, dark
/// window, rules around the title, a boxed section and a MUIP rounded button. It draws on
/// its own overlay canvas rather than the JUTPS HUD canvas, which is switched off while
/// the local character is missing, and it has to sit above the joystick anyway.
/// </summary>
[DisallowMultipleComponent]
public class MatchResultUI : MonoBehaviour
{
    [Header("Look - same assets as the Host / Join / Settings popups")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("MUIP Rounded/Standard button, the one the other popups use.")]
    [SerializeField] private GameObject buttonPrefab;

    [SerializeField] private Sprite mainMenuIcon;
    [SerializeField] private Sprite trophyIcon;

    [Header("Behaviour")]
    [Tooltip("Seconds between the result and the popup appearing, so the final moment can be seen.")]
    [SerializeField] private float showDelay = 1f;

    [Tooltip("Seconds on the result screen before returning to the main menu on its own.")]
    [SerializeField] private float autoReturnSeconds = 15f;

    [Tooltip("Above the gameplay HUD and the mobile controls.")]
    [SerializeField] private int sortingOrder = 1000;

    [Header("Text")]
    [SerializeField] private string victoryText = "VICTORY";
    [SerializeField] private string defeatText = "DEFEAT";
    [SerializeField] private string matchOverText = "MATCH OVER";
    [SerializeField] private string mainMenuText = "MAIN MENU";

    // Palette of the existing popups (HostMatchPanel / JoinMatchPanel / SettingsPanel).
    private static readonly Color DimColor = new Color(0f, 0f, 0f, 0.82f);
    private static readonly Color WindowColor = new Color(0.051f, 0.063f, 0.078f, 0.98f);
    private static readonly Color BoxBorderColor = new Color(0.145f, 0.173f, 0.208f, 1f);
    private static readonly Color BoxInnerColor = new Color(0.063f, 0.078f, 0.098f, 1f);
    private static readonly Color TextColor = new Color(0.941f, 0.957f, 0.973f, 1f);
    private static readonly Color MutedTextColor = new Color(0.467f, 0.518f, 0.569f, 1f);
    private static readonly Color AccentColor = new Color(0.22f, 0.831f, 0.447f, 1f);
    private static readonly Color ButtonColor = new Color(0.22f, 0.729f, 0.447f, 1f);
    private static readonly Color DefeatColor = new Color(0.937f, 0.267f, 0.267f, 1f);

    private const float WindowWidth = 1000f;
    private const float WindowHeight = 640f;

    private enum ResultState
    {
        None,
        Eliminated,   // out, but the match is still being played
        MatchOver     // a winner (or nobody) has been decided
    }

    private ResultState state = ResultState.None;
    private float stateChangedAt;
    private bool waitingToShow;
    private bool built;
    private bool leavingMatch;

    private float returnAtTime = -1f;
    private int lastCountdownShown = -1;

    private RectTransform window;
    private CanvasGroup windowGroup;
    private TextMeshProUGUI titleLabel;
    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI boxLabel;
    private TextMeshProUGUI headlineLabel;
    private TextMeshProUGUI detailLabel;
    private TextMeshProUGUI modeLabel;
    private TextMeshProUGUI countdownLabel;
    private RectTransform ruleLeft;
    private RectTransform ruleRight;
    private RectTransform trophy;
    private ButtonManager mainMenuButton;

    private void Update()
    {
        if (leavingMatch)
            return;

        ResultState desired = DesiredState();

        // Only ever moves forward: once the screen is up it stays up, and an eliminated
        // player's screen turns into the final result when the match is decided.
        if (desired != ResultState.None && desired != state)
        {
            state = desired;
            stateChangedAt = Time.unscaledTime;
            waitingToShow = true;

            // Controls stop the moment it happens; the popup follows after a beat.
            StopLocalGameplay();
        }

        if (state == ResultState.None)
            return;

        if (waitingToShow)
        {
            if (Time.unscaledTime - stateChangedAt < showDelay)
                return;

            waitingToShow = false;
            Build();
            Refresh();
        }

        UpdateCountdown();
    }

    private ResultState DesiredState()
    {
        MatchTracker tracker = MatchTracker.Instance;

        if (tracker != null && tracker.TrackingActive && tracker.MatchEnded)
            return ResultState.MatchOver;

        return IsLocalPlayerDead() ? ResultState.Eliminated : ResultState.None;
    }

    private static bool IsLocalPlayerDead()
    {
        PlayerHealthManager health = PlayerHealthManager.LocalInstance;

        if (health != null)
            return health.netIsDead;

        LSPlayer player = LSPlayer.LocalInstance;
        return player != null && !player.isAlive;
    }

    /// <summary>
    /// Leaving disconnects, and for a host that ends the match for everyone still
    /// playing. So a dead host waits for the result instead of being counted out.
    /// </summary>
    private bool CanReturnNow()
    {
        return state == ResultState.MatchOver || !NetworkServer.active;
    }

    // -------------------------
    // Content
    // -------------------------

    private void Refresh()
    {
        MatchTracker tracker = MatchTracker.Instance;
        LSMatchManager match = LSMatchManager.Instance;
        LSPlayer localPlayer = LSPlayer.LocalInstance;

        LSMatchManager.GameMode mode = match != null ? match.currentMode : LSMatchManager.GameMode.Solo;
        bool soloMode = mode == LSMatchManager.GameMode.Solo;

        string title;
        string subtitle;
        Color titleColor;
        string label;
        string headline;
        string detail;
        bool showTrophy = false;
        bool localVictory = false;

        if (state == ResultState.MatchOver && tracker != null)
        {
            bool hasWinner = soloMode
                ? !string.IsNullOrWhiteSpace(tracker.WinnerPlayerName)
                : tracker.WinningTeamId > 0;

            if (hasWinner && localPlayer != null)
            {
                // Solo: names can repeat, so identity is "the one still alive" instead.
                // Teams: dead members of the winning team still share the win.
                localVictory = soloMode
                    ? localPlayer.isAlive
                    : localPlayer.teamID > 0 && localPlayer.teamID == tracker.WinningTeamId;
            }

            if (!hasWinner)
            {
                title = matchOverText;
                subtitle = "NOBODY SURVIVED";
                titleColor = TextColor;
                label = "RESULT";
                headline = "NO SURVIVORS";
                detail = "EVERYONE WAS ELIMINATED";
            }
            else
            {
                title = localVictory ? victoryText : defeatText;
                titleColor = localVictory ? AccentColor : DefeatColor;
                subtitle = localVictory
                    ? (soloMode ? "YOU ARE THE LAST ONE STANDING" : "YOUR TEAM IS THE LAST ONE STANDING")
                    : "BETTER LUCK NEXT TIME";
                showTrophy = true;

                if (soloMode)
                {
                    label = "WINNER";
                    headline = tracker.WinnerPlayerName;
                    detail = "LAST PLAYER STANDING";
                }
                else
                {
                    label = "WINNING TEAM";
                    headline = "TEAM " + tracker.WinningTeamId;
                    detail = FormatMembers(tracker.WinningTeamMemberNames);
                }
            }
        }
        else
        {
            // Eliminated while the match is still running.
            title = defeatText;
            titleColor = DefeatColor;
            subtitle = "YOU WERE ELIMINATED";

            int left = tracker == null ? 0 : (soloMode ? tracker.AlivePlayers : tracker.AliveTeams);

            if (tracker == null || left <= 0)
            {
                label = "RESULT";
                headline = "YOU ARE OUT";
                detail = "THE MATCH IS STILL RUNNING";
            }
            else
            {
                label = soloMode ? "PLAYERS LEFT" : "TEAMS LEFT";
                headline = left.ToString();
                detail = soloMode ? "THE MATCH IS STILL RUNNING" : "YOUR TEAM CAN STILL TAKE THE WIN";
            }
        }

        titleLabel.text = title;
        titleLabel.color = titleColor;
        subtitleLabel.text = subtitle;
        boxLabel.text = label;

        headlineLabel.text = headline;
        headlineLabel.color = state == ResultState.MatchOver && showTrophy ? TextColor : MutedTextColor;

        detailLabel.text = detail;
        modeLabel.text = mode.ToString().ToUpperInvariant() + "  ·  BATTLE ROYALE";

        LayoutRules(title, titleColor);

        if (trophy != null)
        {
            trophy.gameObject.SetActive(showTrophy);
            trophy.GetComponent<Image>().color = localVictory ? AccentColor : TextColor;
        }

        // Every fresh result restarts the countdown, including an eliminated player's
        // screen turning into the final result.
        returnAtTime = CanReturnNow() ? Time.unscaledTime + Mathf.Max(1f, autoReturnSeconds) : -1f;
        lastCountdownShown = -1;

        if (returnAtTime < 0f)
            countdownLabel.text = "WAITING FOR THE MATCH TO FINISH";
    }

    private void UpdateCountdown()
    {
        if (countdownLabel == null || returnAtTime < 0f)
            return;

        float remaining = returnAtTime - Time.unscaledTime;

        if (remaining <= 0f)
        {
            OnMainMenuClicked();
            return;
        }

        int seconds = Mathf.CeilToInt(remaining);

        // Only touch the text when the number actually changes.
        if (seconds != lastCountdownShown)
        {
            lastCountdownShown = seconds;
            countdownLabel.text = "RETURNING TO MAIN MENU IN " + seconds;
        }
    }

    /// <summary>MatchTracker stores one name per line; shown on one wrapping row here.</summary>
    private static string FormatMembers(string names)
    {
        if (string.IsNullOrWhiteSpace(names))
            return string.Empty;

        string[] split = names.Split('\n');

        for (int i = 0; i < split.Length; i++)
            split[i] = split[i].Trim();

        return string.Join("   ·   ", split);
    }

    // -------------------------
    // Popup
    // -------------------------

    private void Build()
    {
        if (built)
            return;

        built = true;
        EnsureEventSystem();

        // Own overlay canvas, above the HUD and the mobile controls.
        var canvasGo = new GameObject("MatchResultCanvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);

        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;

        // Same scaler as the menu and HUD canvases, so sizes match the other popups.
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGo.AddComponent<GraphicRaycaster>();
        var root = (RectTransform)canvasGo.transform;

        // Dim: also swallows every touch meant for the HUD underneath. No click-to-close,
        // the screen stays until the player leaves.
        Stretch(NewImage("Dim", root, DimColor, true));

        window = NewImage("Window", root, WindowColor, true);
        Place(window, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(WindowWidth, WindowHeight));
        windowGroup = window.gameObject.AddComponent<CanvasGroup>();

        // Header: title with a rule on either side, then the subtitle - as on Host Match.
        titleLabel = NewText("Title", window, string.Empty, 50f, 10f, TextColor, TextAlignmentOptions.Center);
        PlaceTop((RectTransform)titleLabel.transform, -40f, 80f);

        ruleLeft = NewImage("RuleLeft", window, AccentColor, false);
        ruleRight = NewImage("RuleRight", window, AccentColor, false);

        subtitleLabel = NewText("Subtitle", window, string.Empty, 19f, 12f, MutedTextColor, TextAlignmentOptions.Center);
        PlaceTop((RectTransform)subtitleLabel.transform, -126f, 32f);

        // Result box, same border / inner pair as the Host Match sections.
        RectTransform box = NewImage("Box_Result", window, BoxBorderColor, false);
        Place(box, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -182f),
              new Vector2(WindowWidth - 96f, 280f), new Vector2(0f, 1f));

        RectTransform inner = NewImage("Inner", box, BoxInnerColor, false);
        Stretch(inner);
        inner.sizeDelta = new Vector2(-4f, -4f);

        boxLabel = NewText("Label", box, string.Empty, 24f, 4f, TextColor, TextAlignmentOptions.Left);
        Place((RectTransform)boxLabel.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
              new Vector2(24f, -16f), new Vector2(400f, 40f), new Vector2(0f, 1f));

        if (trophyIcon != null)
        {
            trophy = NewImage("Trophy", box, TextColor, false);

            Image trophyImage = trophy.GetComponent<Image>();
            trophyImage.sprite = trophyIcon;
            trophyImage.preserveAspect = true;

            Place(trophy, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f),
                  new Vector2(72f, 72f), new Vector2(0.5f, 1f));
        }

        headlineLabel = NewText("Headline", box, string.Empty, 40f, 2f, TextColor, TextAlignmentOptions.Center);
        PlaceTop((RectTransform)headlineLabel.transform, -112f, 60f, 48f);
        headlineLabel.enableAutoSizing = true;
        headlineLabel.fontSizeMin = 20f;
        headlineLabel.fontSizeMax = 40f;
        headlineLabel.overflowMode = TextOverflowModes.Ellipsis;

        detailLabel = NewText("Detail", box, string.Empty, 20f, 2f, MutedTextColor, TextAlignmentOptions.Top);
        PlaceTop((RectTransform)detailLabel.transform, -180f, 84f, 48f);
        detailLabel.textWrappingMode = TextWrappingModes.Normal;
        detailLabel.enableAutoSizing = true;
        detailLabel.fontSizeMin = 14f;
        detailLabel.fontSizeMax = 20f;
        detailLabel.overflowMode = TextOverflowModes.Ellipsis;

        modeLabel = NewText("Mode", window, string.Empty, 15f, 4f, MutedTextColor, TextAlignmentOptions.Center);
        PlaceTop((RectTransform)modeLabel.transform, -484f, 30f);

        countdownLabel = NewText("Countdown", window, string.Empty, 15f, 4f, MutedTextColor, TextAlignmentOptions.Center);
        Place((RectTransform)countdownLabel.transform, new Vector2(0f, 0f), new Vector2(1f, 0f),
              new Vector2(0f, 116f), new Vector2(0f, 28f), new Vector2(0.5f, 0f));

        mainMenuButton = CreateMainMenuButton(window);

        StartCoroutine(AnimateIn(window, windowGroup));
    }

    /// <summary>Rules sit either side of the title, so they follow its width.</summary>
    private void LayoutRules(string title, Color color)
    {
        float halfTitle = titleLabel.GetPreferredValues(title, 4000f, 80f).x * 0.5f;

        PlaceRule(ruleLeft, -(halfTitle + 36f), new Vector2(1f, 0.5f), color);
        PlaceRule(ruleRight, halfTitle + 36f, new Vector2(0f, 0.5f), color);
    }

    private static void PlaceRule(RectTransform rule, float x, Vector2 pivot, Color color)
    {
        if (rule == null)
            return;

        rule.GetComponent<Image>().color = color;
        Place(rule, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, -80f), new Vector2(120f, 3f), pivot);
    }

    private ButtonManager CreateMainMenuButton(RectTransform parent)
    {
        if (buttonPrefab == null)
        {
            Debug.LogWarning("[MatchResultUI] No button prefab assigned; the screen has no MAIN MENU button.");
            return null;
        }

        GameObject go = Instantiate(buttonPrefab, parent, false);
        go.name = "MainMenuButton";

        var rect = (RectTransform)go.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 44f);
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, 62f);

        // MUIP Manager has dynamic update on, so UIManagerButton repaints every button in
        // the theme colour each frame. The Host popup's buttons opt out the same way;
        // without this the green below would be overwritten on the very next frame.
        if (go.TryGetComponent(out UIManagerButton theme))
            theme.overrideColors = true;

        // The same recolour the Host popup applies to its green START HOST button.
        Transform background = go.transform.Find("Normal/Background");
        if (background != null && background.TryGetComponent(out Image backgroundImage))
            backgroundImage.color = ButtonColor;

        foreach (Image image in go.GetComponentsInChildren<Image>(true))
        {
            if (image.name == "Icon")
                image.color = Color.white;
        }

        ButtonManager button = go.GetComponent<ButtonManager>();

        if (button != null)
        {
            button.buttonText = mainMenuText;
            button.buttonIcon = mainMenuIcon;
            button.enableIcon = mainMenuIcon != null;
            button.onClick.RemoveListener(OnMainMenuClicked);
            button.onClick.AddListener(OnMainMenuClicked);

            // White on green, as on START HOST.
            if (button.normalText != null) button.normalText.color = Color.white;
            if (button.highlightedText != null) button.highlightedText.color = Color.white;

            button.UpdateUI();
        }

        return button;
    }

    private static IEnumerator AnimateIn(RectTransform window, CanvasGroup group)
    {
        const float duration = 0.25f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            group.alpha = t;
            window.localScale = Vector3.one * Mathf.Lerp(0.92f, 1f, t);
            yield return null;
        }

        group.alpha = 1f;
        window.localScale = Vector3.one;
    }

    /// <summary>
    /// The only EventSystem in GameScene lives on the JUTPS HUD canvas, which is switched
    /// off while the local character is missing. The button must still work then.
    /// </summary>
    private static void EnsureEventSystem()
    {
        if (EventSystem.current != null)
            return;

        new GameObject("EventSystem (Match Result)", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    // -------------------------
    // UI helpers
    // -------------------------

    private RectTransform NewImage(string name, Transform parent, Color color, bool raycastTarget)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = raycastTarget;

        return (RectTransform)go.transform;
    }

    private TextMeshProUGUI NewText(string name, Transform parent, string text, float size, float spacing,
                                    Color color, TextAlignmentOptions alignment)
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

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        Place(rect, anchorMin, anchorMax, position, size, new Vector2(0.5f, 0.5f));
    }

    private static void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 position,
                              Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    /// <summary>Full-width row hanging from the parent's top edge, with an optional side margin.</summary>
    private static void PlaceTop(RectTransform rect, float y, float height, float sideMargin = 0f)
    {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(-2f * sideMargin, height);
    }

    // -------------------------
    // Gameplay / leaving
    // -------------------------

    private static void StopLocalGameplay()
    {
        JUCharacterController character = JUGameManager.PlayerController;

        if (character != null)
        {
            character.DisableLocomotion();
            character.UseDefaultControllerInput = false;
            character.FiringMode = false;
            character.IsAiming = false;
        }

        // Stop camera look while the result is on screen.
        if (CameraManager.MainCam != null)
            CameraManager.MainCam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnMainMenuClicked()
    {
        if (leavingMatch)
            return;

        leavingMatch = true;
        returnAtTime = -1f;

        if (mainMenuButton != null)
            mainMenuButton.Interactable(false);

        if (countdownLabel != null)
            countdownLabel.text = "LEAVING THE MATCH";

        // The project's existing host/client shutdown, same path as losing connection.
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
            return;
        }

        // Fallback only if the shared UI manager is unavailable.
        if (NetworkManager.singleton == null)
            return;

        if (NetworkServer.active && NetworkClient.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}
