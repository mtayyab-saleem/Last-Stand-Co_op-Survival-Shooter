using UnityEngine;
using TMPro;
using Mirror;
using Michsky.MUIP;
using System.Collections.Generic;

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;

    [Header("UI Panels")]
    [SerializeField] private GameObject hostControlsPanel;
    [SerializeField] private GameObject clientControlsPanel;

    [Header("UI Elements")]
    [SerializeField] private ListView playerListView;
    [SerializeField] private TextMeshProUGUI modeText;

    [Header("Buttons")]
    [SerializeField] private ButtonManager hostStartButton;
    [SerializeField] private ButtonManager clientReadyButton;
    [SerializeField] private ButtonManager leaveLobbyButton;

    private void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        // When the lobby is opened, forcefully refresh their UI role to match actual network state
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.UpdateLocalUI();
        }
    }

    private void Start()
    {
        SetupButtons();

        // Hide panels initially until we confirm local player status
        hostControlsPanel.SetActive(false);
        clientControlsPanel.SetActive(false);

        // Ensure MUIP buttons update their visual state
        if (hostStartButton != null) hostStartButton.UpdateUI();
        if (clientReadyButton != null) clientReadyButton.UpdateUI();
    }

    private void SetupButtons()
    {
        if (leaveLobbyButton != null)
        {
            leaveLobbyButton.onClick.AddListener(OnLeaveLobbyClicked);
        }

        if (hostStartButton != null)
        {
            hostStartButton.onClick.AddListener(OnHostStartClicked);
        }

        if (clientReadyButton != null)
        {
            clientReadyButton.onClick.AddListener(OnClientReadyClicked);
        }
    }

    /// <summary>
    /// This will be called by LSMatchManager whenever a player joins, leaves, or changes ready state.
    /// </summary>
    public void RefreshLobbyUI(bool isLocalPlayerHost, LSPlayer localPlayer, List<LSPlayer> allPlayers, int gameModeInt, bool canHostStart)
    {
        // 1. Toggle correct control panels
        if (hostControlsPanel != null) hostControlsPanel.SetActive(isLocalPlayerHost);
        if (clientControlsPanel != null) clientControlsPanel.SetActive(!isLocalPlayerHost);

        // 2. Update Mode Text (Assuming 0=Solo, 1=Duo, 2=Squad based on your HostMenuUI)
        if (modeText != null)
        {
            if (gameModeInt == 0) modeText.text = "MODE: SOLO";
            else if (gameModeInt == 1) modeText.text = "MODE: DUO";
            else if (gameModeInt == 2) modeText.text = "MODE: SQUAD";
        }

        // 3. Update Host Start Button state
        if (isLocalPlayerHost && hostStartButton != null)
        {
            hostStartButton.Interactable(canHostStart);
        }

        // 4. Update Client Ready Button visual state (Red = Not Ready, Green = Ready)
        if (!isLocalPlayerHost && clientReadyButton != null && localPlayer != null)
        {
            if (localPlayer.isReady)
            {
                clientReadyButton.buttonText = "WAITING FOR HOST";
                if (clientReadyButton.normalImage != null) clientReadyButton.normalImage.color = Color.green;
            }
            else
            {
                clientReadyButton.buttonText = "READY UP";
                if (clientReadyButton.normalImage != null) clientReadyButton.normalImage.color = Color.red;
            }
            clientReadyButton.UpdateUI();
        }

        // 5. Rebuild the Player List View
        RefreshPlayerList(allPlayers);
    }

    public void ResetUI()
    {
        if (playerListView != null)
        {
            playerListView.listItems.Clear();
            playerListView.InitializeItems();
        }

        if (hostControlsPanel != null) hostControlsPanel.SetActive(false);
        if (clientControlsPanel != null) clientControlsPanel.SetActive(false);
        
        gameObject.SetActive(false); // Hides the lobby panel itself
    }

    private void RefreshPlayerList(List<LSPlayer> allPlayers)
    {
        if (playerListView == null) return;

        playerListView.listItems.Clear();

        for (int i = 0; i < allPlayers.Count; i++)
        {
            var player = allPlayers[i];
            if (player == null) continue;

            ListView.ListItem newItem = new ListView.ListItem();
            newItem.itemTitle = player.playerName;

            // --- Column 1: Name ---
            ListView.ListRow nameRow = new ListView.ListRow();
            nameRow.rowType = ListView.RowType.Text;
            nameRow.rowText = player.playerName;
            newItem.row0 = nameRow;

            // --- Column 2: Type (Host or Client) ---
            ListView.ListRow typeRow = new ListView.ListRow();
            typeRow.rowType = ListView.RowType.Text;
            // YAHAN BADLAAV KIYA HAI: isGameHost lagaya hai
            typeRow.rowText = player.isGameHost ? "Host" : "Client";
            newItem.row1 = typeRow;

            // --- Column 3: Status ---
            ListView.ListRow statusRow = new ListView.ListRow();
            statusRow.rowType = ListView.RowType.Text;

            // YAHAN BHI BADLAAV KIYA HAI
            if (player.isGameHost)
            {
                statusRow.rowText = "<color=#00FF00>Ready</color>";
            }
            else
            {
                statusRow.rowText = player.isReady ? "<color=#00FF00>Ready</color>" : "<color=#FF0000>Not Ready</color>";
            }
            newItem.row2 = statusRow;

            playerListView.listItems.Add(newItem);
        }

        playerListView.InitializeItems();
    }

    private void OnClientReadyClicked()
    {
        // Fallback method to perfectly find the local player even if Mirror glitches
        LSPlayer myPlayer = null;
        if (NetworkClient.localPlayer != null)
        {
            myPlayer = NetworkClient.localPlayer.GetComponent<LSPlayer>();
        }
        else
        {
            if (LSMatchManager.Instance != null)
            {
                foreach (var p in LSMatchManager.Instance.players)
                {
                    if (p.isLocalPlayer) myPlayer = p;
                }
            }
        }

        if (myPlayer != null)
        {
            myPlayer.CmdSetReady(!myPlayer.isReady);
            Debug.Log("Ready Status Toggled: " + !myPlayer.isReady);
        }
        else
        {
            Debug.LogWarning("Local Player not found to Ready Up!");
        }
    }

    private void OnHostStartClicked()
    {
        if (LSMatchManager.Instance != null)
        {
            LSMatchManager.Instance.StartMatch();
        }
    }

    private void OnLeaveLobbyClicked()
    {
        // We use your existing safe disconnect sequence!
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
        }
    }
}