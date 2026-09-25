using System.Collections.Generic;
using Michsky.MUIP;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The lobby screen: who is in the match, what they are waiting for, and the two things
/// a player can do there - ready up (or start, as host) and leave.
///
/// Built in code in the shared theme (see <see cref="LSUITheme"/>), so it matches the
/// Host, Join and Settings popups instead of the stock MUIP list it replaced. The old
/// hand-made hierarchy is switched off at startup and is no longer used.
///
/// Everything sits along the top of the screen, and the roster starts collapsed to a
/// single bar: players can still walk around the lobby, and the joystick on the left
/// and the action buttons on the right stay clear.
///
/// LSMatchManager drives it: every join, leave, ready toggle and team change ends in
/// RefreshLobbyUI, which redraws the roster from the synced list.
/// </summary>
public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;

    [Header("Look - same assets as the Host / Join / Settings popups")]
    [SerializeField] private TMP_FontAsset font;

    [Tooltip("MUIP Rounded/Standard button, the one the other popups use.")]
    [SerializeField] private GameObject buttonPrefab;

    [SerializeField] private Sprite startIcon;
    [SerializeField] private Sprite readyIcon;
    [SerializeField] private Sprite leaveIcon;

    [Tooltip("Arrow shown on the roster bar. Points down when collapsed, up when open.")]
    [SerializeField] private Sprite chevronIcon;

    [Header("Scene")]
    [Tooltip("The old lobby canvas. Switched off at startup; the screen is built in code now.")]
    [SerializeField] private GameObject legacyUiRoot;

    [Tooltip("Draw order. The JUTPS HUD canvas in this scene uses 0.")]
    [SerializeField] private int sortingOrder = 100;

    [Range(0f, 0.8f)]
    [Tooltip("How much the 3D lobby behind the UI is darkened.")]
    [SerializeField] private float scrimStrength = 0.25f;

    [Tooltip("Roster starts collapsed so it never covers the lobby or the controls.")]
    [SerializeField] private bool startExpanded = false;

    // Gameplay-only HUD pieces that have no business being on screen in a lobby.
    // The joystick is deliberately left alone: players can still walk around.
    private static readonly string[] GameplayHudToHide =
    {
        "Health Bar",
        "Current Item Information",
        "Crosshair Dynamic",
        "Crosshair Resizeable",
        "Hit Marker",
        "FPS Counter"
    };

    private const float PanelWidth = 560f;
    private const float HeaderHeight = 56f;
    private const float RowHeight = 54f;
    private const float RowSpacing = 6f;
    private const float BodyPadding = 12f;
    private const float EmptyRowHeight = 44f;

    private bool built;
    private bool expanded;

    private RectTransform panel;
    private RectTransform body;
    private RectTransform rowsRoot;
    private RectTransform chevron;
    private RectTransform titleRuleLeft;
    private RectTransform titleRuleRight;

    private TextMeshProUGUI subtitleLabel;
    private TextMeshProUGUI countLabel;
    private TextMeshProUGUI hintLabel;
    private TextMeshProUGUI emptyLabel;

    private ButtonManager primaryButton;
    private ButtonManager leaveButton;

    private readonly List<GameObject> rows = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        Build();
        HideGameplayHud();

        // Draw whatever the match manager already knows about.
        if (LSMatchManager.Instance != null)
            LSMatchManager.Instance.UpdateLocalUI();
    }

    private void OnEnable()
    {
        // Coming back to the lobby view: re-read the current network state.
        if (LSMatchManager.Instance != null)
            LSMatchManager.Instance.UpdateLocalUI();
    }

    // -------------------------
    // Refresh
    // -------------------------

    /// <summary>
    /// Called by LSMatchManager whenever a player joins, leaves, readies up or changes team.
    /// </summary>
    public void RefreshLobbyUI(bool isLocalPlayerHost, LSPlayer localPlayer, List<LSPlayer> allPlayers,
                               int gameModeInt, bool canHostStart)
    {
        Build();

        LSMatchManager match = LSMatchManager.Instance;
        int maxPlayers = match != null ? match.maxPlayers : 8;
        int minimumPlayers = match != null ? match.MinimumPlayersToStart : 1;
        int playerCount = allPlayers != null ? allPlayers.Count : 0;

        string mode =
            gameModeInt == 1 ? "DUO" :
            gameModeInt == 2 ? "SQUAD" : "SOLO";

        bool aiFill = match != null && match.fillWithAI;
        subtitleLabel.text = mode + "  ·  BATTLE ROYALE" + (aiFill ? "  ·  AI FILL" : string.Empty);

        countLabel.text = playerCount + "/" + maxPlayers;
        countLabel.color = playerCount >= maxPlayers ? LSUITheme.Accent : LSUITheme.Text;

        DrawRows(allPlayers, localPlayer);
        ApplyExpandedState();

        if (isLocalPlayerHost)
            ShowHostControls(canHostStart, playerCount, minimumPlayers);
        else
            ShowClientControls(localPlayer);
    }

    private void ShowHostControls(bool canHostStart, int playerCount, int minimumPlayers)
    {
        if (primaryButton == null)
            return;

        primaryButton.buttonText = "START MATCH";
        primaryButton.buttonIcon = startIcon;
        primaryButton.enableIcon = startIcon != null;
        primaryButton.UpdateUI();
        primaryButton.Interactable(canHostStart);

        LSUITheme.SetButtonColor(primaryButton, canHostStart ? LSUITheme.ButtonGreen : LSUITheme.ButtonIdle);

        int bots = LSMatchManager.Instance != null ? LSMatchManager.Instance.BotsToFill : 0;

        if (canHostStart)
            hintLabel.text = bots > 0 ? "READY  ·  AI FILLS " + bots + " SLOTS" : "EVERYONE IS READY";
        else
            hintLabel.text = "NEED " + minimumPlayers + " PLAYERS TO START  ·  " + playerCount + "/" + minimumPlayers;

        hintLabel.color = canHostStart ? LSUITheme.Accent : LSUITheme.Muted;
    }

    private void ShowClientControls(LSPlayer localPlayer)
    {
        if (primaryButton == null)
            return;

        bool ready = localPlayer != null && localPlayer.isReady;

        primaryButton.buttonText = ready ? "WAITING FOR HOST" : "READY UP";
        primaryButton.buttonIcon = readyIcon;
        primaryButton.enableIcon = readyIcon != null;
        primaryButton.UpdateUI();
        primaryButton.Interactable(true);

        LSUITheme.SetButtonColor(primaryButton, ready ? LSUITheme.ButtonIdle : LSUITheme.ButtonGreen);

        hintLabel.text = ready ? "WAITING FOR THE HOST TO START" : "TAP READY WHEN YOU ARE SET";
        hintLabel.color = LSUITheme.Muted;
    }

    /// <summary>One row per player: team stripe, name, team slot and status.</summary>
    private void DrawRows(List<LSPlayer> allPlayers, LSPlayer localPlayer)
    {
        foreach (GameObject row in rows)
        {
            // Hidden first: Destroy only takes effect at the end of the frame, and the
            // replacements are created right below at the same positions.
            row.SetActive(false);
            Destroy(row);
        }

        rows.Clear();

        int count = allPlayers != null ? allPlayers.Count : 0;
        emptyLabel.gameObject.SetActive(count == 0);

        for (int i = 0; i < count; i++)
        {
            LSPlayer player = allPlayers[i];

            if (player == null)
                continue;

            rows.Add(BuildRow(player, player == localPlayer, rows.Count));
        }
    }

    private GameObject BuildRow(LSPlayer player, bool isLocal, int index)
    {
        RectTransform row = LSUITheme.Panel("Row_" + index, rowsRoot, LSUITheme.BoxInner);
        LSUITheme.PlaceTop(row, -index * (RowHeight + RowSpacing), RowHeight);

        // Team colour down the left edge, so squads read at a glance.
        Color teamColor = LSUITheme.TeamColor(player.teamID);
        RectTransform stripe = LSUITheme.Panel("Stripe", row, teamColor);
        LSUITheme.Place(stripe, new Vector2(0f, 0f), new Vector2(0f, 1f), Vector2.zero,
                        new Vector2(6f, 0f), new Vector2(0f, 0.5f));

        string name = string.IsNullOrWhiteSpace(player.playerName) ? "PLAYER" : player.playerName;

        if (isLocal)
            name += "  <size=60%><color=#38D472>YOU</color></size>";

        TextMeshProUGUI nameLabel = LSUITheme.Label("Name", row, font, name, 20f, 0f,
                                                    LSUITheme.Text, TextAlignmentOptions.Left);
        LSUITheme.Place((RectTransform)nameLabel.transform, new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                        new Vector2(20f, 0f), new Vector2(-20f, 0f), new Vector2(0f, 0.5f));
        nameLabel.overflowMode = TextOverflowModes.Ellipsis;

        // Team slot, only once teams have been handed out.
        string slot = player.teamID > 0 ? "T" + player.teamID + "  ·  #" + player.teamMemberIndex : "NO TEAM";
        TextMeshProUGUI slotLabel = LSUITheme.Label("Slot", row, font, slot, 15f, 2f,
                                                    teamColor, TextAlignmentOptions.Right);
        LSUITheme.Place((RectTransform)slotLabel.transform, new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                        new Vector2(-130f, 0f), new Vector2(-16f, 0f), new Vector2(1f, 0.5f));

        BuildStatusBadge(row, player);

        return row.gameObject;
    }

    private void BuildStatusBadge(RectTransform row, LSPlayer player)
    {
        string text;
        Color color;

        if (player.isGameHost)
        {
            text = "HOST";
            color = LSUITheme.Accent;
        }
        else if (player.isReady)
        {
            text = "READY";
            color = LSUITheme.Accent;
        }
        else
        {
            text = "NOT READY";
            color = LSUITheme.Danger;
        }

        Color badgeColor = color;
        badgeColor.a = 0.16f;

        RectTransform badge = LSUITheme.Panel("Status", row, badgeColor);
        LSUITheme.Place(badge, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-10f, 0f),
                        new Vector2(112f, 28f), new Vector2(1f, 0.5f));

        TextMeshProUGUI label = LSUITheme.Label("Text", badge, font, text, 13f, 2f, color, TextAlignmentOptions.Center);
        LSUITheme.Stretch((RectTransform)label.transform);
    }

    // -------------------------
    // Collapsing
    // -------------------------

    private void ToggleExpanded()
    {
        expanded = !expanded;
        ApplyExpandedState();
    }

    /// <summary>The bar alone when collapsed; the bar plus exactly as many rows as there are players when open.</summary>
    private void ApplyExpandedState()
    {
        if (panel == null)
            return;

        body.gameObject.SetActive(expanded);

        float bodyHeight = 0f;

        if (expanded)
        {
            int count = rows.Count;

            float contents = count > 0
                ? count * RowHeight + (count - 1) * RowSpacing
                : EmptyRowHeight;

            bodyHeight = contents + 2f * BodyPadding;
            body.sizeDelta = new Vector2(-4f, bodyHeight);
        }

        panel.sizeDelta = new Vector2(PanelWidth, HeaderHeight + bodyHeight);

        if (chevron != null)
            chevron.localEulerAngles = new Vector3(0f, 0f, expanded ? 180f : 0f);
    }

    // -------------------------
    // Screen
    // -------------------------

    private void Build()
    {
        if (built)
            return;

        built = true;
        expanded = startExpanded;

        if (legacyUiRoot != null)
            legacyUiRoot.SetActive(false);

        LSUITheme.EnsureEventSystem();
        RectTransform root = LSUITheme.Canvas("LobbyScreenCanvas", transform, sortingOrder);

        // Takes the glare off the lobby ground and sky so the text reads clearly.
        // Not a raycast target: looking around with the camera still works.
        RectTransform scrim = LSUITheme.Panel("Scrim", root, new Color(0f, 0f, 0f, scrimStrength));
        LSUITheme.Stretch(scrim);

        BuildHeader(root);
        BuildRosterPanel(root);
        BuildActions(root);

        ApplyExpandedState();
    }

    private void BuildHeader(RectTransform root)
    {
        TextMeshProUGUI title = LSUITheme.Label("Title", root, font, "MATCH LOBBY", 40f, 10f,
                                                LSUITheme.Text, TextAlignmentOptions.Center);
        LSUITheme.PlaceTop((RectTransform)title.transform, -36f, 58f);

        titleRuleLeft = LSUITheme.Panel("RuleLeft", root, LSUITheme.Accent);
        titleRuleRight = LSUITheme.Panel("RuleRight", root, LSUITheme.Accent);

        const float half = 175f;   // "MATCH LOBBY" at 40 with 10 spacing

        LSUITheme.Place(titleRuleLeft, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(-(half + 32f), -65f), new Vector2(110f, 3f), new Vector2(1f, 0.5f));

        LSUITheme.Place(titleRuleRight, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                        new Vector2(half + 32f, -65f), new Vector2(110f, 3f), new Vector2(0f, 0.5f));

        subtitleLabel = LSUITheme.Label("Subtitle", root, font, string.Empty, 17f, 12f,
                                        LSUITheme.Muted, TextAlignmentOptions.Center);
        LSUITheme.PlaceTop((RectTransform)subtitleLabel.transform, -94f, 26f);
    }

    /// <summary>
    /// Top-left, under the settings button, and collapsed to a single bar by default so
    /// it never sits over the lobby or the controls. Tapping the bar opens it.
    /// </summary>
    private void BuildRosterPanel(RectTransform root)
    {
        panel = LSUITheme.Panel("PlayersPanel", root, LSUITheme.BoxBorder, true);
        LSUITheme.Place(panel, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -96f),
                        new Vector2(PanelWidth, HeaderHeight), new Vector2(0f, 1f));

        RectTransform bar = LSUITheme.Panel("Bar", panel, LSUITheme.Window, true);
        LSUITheme.Place(bar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -2f),
                        new Vector2(-4f, HeaderHeight - 4f), new Vector2(0.5f, 1f));

        // The whole bar is the toggle, so it is an easy target on a phone.
        var toggle = bar.gameObject.AddComponent<Button>();
        toggle.transition = Selectable.Transition.None;
        toggle.onClick.AddListener(ToggleExpanded);

        TextMeshProUGUI playersLabel = LSUITheme.Label("PlayersLabel", bar, font, "PLAYERS", 21f, 4f,
                                                       LSUITheme.Text, TextAlignmentOptions.Left);
        LSUITheme.Place((RectTransform)playersLabel.transform, new Vector2(0f, 0f), new Vector2(0.6f, 1f),
                        new Vector2(20f, 0f), new Vector2(-20f, 0f), new Vector2(0f, 0.5f));

        countLabel = LSUITheme.Label("Count", bar, font, "0/8", 21f, 4f,
                                     LSUITheme.Text, TextAlignmentOptions.Right);
        LSUITheme.Place((RectTransform)countLabel.transform, new Vector2(1f, 0f), new Vector2(1f, 1f),
                        new Vector2(-56f, 0f), new Vector2(200f, 0f), new Vector2(1f, 0.5f));

        if (chevronIcon != null)
        {
            chevron = LSUITheme.Panel("Chevron", bar, LSUITheme.Muted);

            Image image = chevron.GetComponent<Image>();
            image.sprite = chevronIcon;
            image.preserveAspect = true;

            LSUITheme.Place(chevron, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f, 0f),
                            new Vector2(22f, 22f), new Vector2(1f, 0.5f));
        }

        body = LSUITheme.Panel("Body", panel, LSUITheme.BoxInner, true);
        LSUITheme.Place(body, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -HeaderHeight),
                        new Vector2(-4f, 0f), new Vector2(0.5f, 1f));

        rowsRoot = LSUITheme.Panel("Rows", body, Color.clear);
        LSUITheme.Stretch(rowsRoot);
        rowsRoot.offsetMin = new Vector2(BodyPadding, BodyPadding);
        rowsRoot.offsetMax = new Vector2(-BodyPadding, -BodyPadding);

        emptyLabel = LSUITheme.Label("Empty", rowsRoot, font, "WAITING FOR PLAYERS", 17f, 6f,
                                     LSUITheme.Muted, TextAlignmentOptions.Center);
        LSUITheme.PlaceTop((RectTransform)emptyLabel.transform, 0f, EmptyRowHeight);
    }

    /// <summary>
    /// Top-right: the bottom of the screen belongs to the joystick on the left and the
    /// action buttons on the right.
    /// </summary>
    private void BuildActions(RectTransform root)
    {
        RectTransform actions = LSUITheme.Panel("Actions", root, Color.clear);
        LSUITheme.Place(actions, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -96f),
                        new Vector2(600f, 62f), new Vector2(1f, 1f));

        var layout = actions.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = 14f;
        layout.childAlignment = TextAnchor.MiddleRight;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        var fitter = actions.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;

        leaveButton = LSUITheme.Button(buttonPrefab, actions, "LeaveButton", "LEAVE", leaveIcon,
                                       LSUITheme.Danger, OnLeaveLobbyClicked);
        primaryButton = LSUITheme.Button(buttonPrefab, actions, "PrimaryButton", "READY UP", readyIcon,
                                         LSUITheme.ButtonGreen, OnPrimaryClicked);

        SetButtonHeight(leaveButton, 58f);
        SetButtonHeight(primaryButton, 58f);

        hintLabel = LSUITheme.Label("Hint", root, font, string.Empty, 15f, 4f,
                                    LSUITheme.Muted, TextAlignmentOptions.Right);
        LSUITheme.Place((RectTransform)hintLabel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                        new Vector2(-24f, -166f), new Vector2(600f, 24f), new Vector2(1f, 1f));
    }

    private static void SetButtonHeight(ButtonManager button, float height)
    {
        if (button == null)
            return;

        var rect = (RectTransform)button.transform;
        rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
    }

    /// <summary>
    /// The JUTPS HUD lives in this scene too, so the crosshair, health bar, ammo panel,
    /// hit marker and FPS counter used to hang over the lobby. Hidden here; the mobile
    /// joystick stays so players can still move around.
    /// </summary>
    private void HideGameplayHud()
    {
        UISafetyWrapper wrapper = FindFirstObjectByType<UISafetyWrapper>(FindObjectsInactive.Include);

        if (wrapper == null || wrapper.UIPanel == null)
            return;

        foreach (Transform child in wrapper.UIPanel.GetComponentsInChildren<Transform>(true))
        {
            for (int i = 0; i < GameplayHudToHide.Length; i++)
            {
                if (child.name == GameplayHudToHide[i])
                {
                    child.gameObject.SetActive(false);
                    break;
                }
            }
        }
    }

    public void ResetUI()
    {
        foreach (GameObject row in rows)
            Destroy(row);

        rows.Clear();

        gameObject.SetActive(false);
    }

    // -------------------------
    // Actions
    // -------------------------

    /// <summary>
    /// Hook this to a lobby button's OnClick. Moves the local player to the next team
    /// that still has a free slot: 1 -> 2 -> 3 -> 4 -> 1.
    /// </summary>
    public void CycleLocalPlayerTeam()
    {
        if (LSMatchManager.Instance == null)
            return;

        LSPlayer myPlayer = LocalPlayer();

        if (myPlayer == null)
        {
            Debug.LogWarning("[Lobby] Local player not found to cycle team.");
            return;
        }

        LSMatchManager.Instance.CmdCyclePlayerTeam(myPlayer.netId);
    }

    private void OnPrimaryClicked()
    {
        // The same button is START MATCH for the host and READY UP for everyone else.
        if (NetworkServer.active)
        {
            if (LSMatchManager.Instance != null)
                LSMatchManager.Instance.StartMatch();

            return;
        }

        LSPlayer myPlayer = LocalPlayer();

        if (myPlayer == null)
        {
            Debug.LogWarning("[Lobby] Local player not found to ready up.");
            return;
        }

        myPlayer.CmdSetReady(!myPlayer.isReady);
    }

    private static LSPlayer LocalPlayer()
    {
        if (NetworkClient.localPlayer != null)
            return NetworkClient.localPlayer.GetComponent<LSPlayer>();

        if (LSMatchManager.Instance == null)
            return null;

        foreach (LSPlayer player in LSMatchManager.Instance.players)
        {
            if (player != null && player.isLocalPlayer)
                return player;
        }

        return null;
    }

    private void OnLeaveLobbyClicked()
    {
        // The project's existing safe disconnect sequence.
        if (GameUIManager.Instance != null)
            GameUIManager.Instance.TriggerDisconnectSequence();
    }
}
