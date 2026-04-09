using UnityEngine;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP; // Michsky UI Namespace

/// <summary>
/// Handles the Host Match screen, including Map selection, Game Mode selection, and starting the Server/Host.
/// </summary>
public class HostMenuUI : MonoBehaviour
{
    [Header("Network Components")]
    [Tooltip("If left empty, the script will try to find it in the scene automatically.")]
    [SerializeField] private NetworkDiscovery networkDiscovery;

    [Header("Host Controls")]
    [SerializeField] private ButtonManager hostMatchButton;
    [SerializeField] private ButtonManager backButton;

    [Header("Selection Lists")]
    [SerializeField] private ButtonManager[] mapButtons;
    [SerializeField] private ButtonManager[] gameModeButtons;

    [Header("UI Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.0f, 0.8f, 0.2f, 1f);

    // State Tracking
    private int _selectedMapIndex = -1;
    private int _selectedModeIndex = -1;

    private void Start()
    {
        InitializeNetworkDiscovery();
        SetupButtons();
    }

    private void OnEnable()
    {
        // Reset selections every time this panel is opened
        _selectedMapIndex = -1;
        _selectedModeIndex = -1;
        
        UpdateMapButtonVisuals();
        UpdateModeButtonVisuals();
        ValidateHostButton();
    }

    private void InitializeNetworkDiscovery()
    {
        if (networkDiscovery == null)
        {
            networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
        }
    }

    private void SetupButtons()
    {
        // 1. Setup Map Buttons
        if (mapButtons != null)
        {
            for (int i = 0; i < mapButtons.Length; i++)
            {
                int index = i; // Cache index for the lambda closure
                if (mapButtons[i] != null) 
                {
                    mapButtons[i].onClick.AddListener(() => OnMapSelected(index));
                }
            }
        }

        // 2. Setup Game Mode Buttons
        if (gameModeButtons != null)
        {
            for (int i = 0; i < gameModeButtons.Length; i++)
            {
                int index = i; // Cache index for the lambda closure
                if (gameModeButtons[i] != null) 
                {
                    gameModeButtons[i].onClick.AddListener(() => OnGameModeSelected(index));
                }
            }
        }

        // 3. Setup Navigation/Action Buttons
        if (hostMatchButton) hostMatchButton.onClick.AddListener(OnHostMatchClick);
        if (backButton) backButton.onClick.AddListener(OnBackClick);
    }

    // =========================================================
    // SELECTION LOGIC
    // =========================================================

    private void OnMapSelected(int mapIndex)
    {
        _selectedMapIndex = mapIndex;
        UpdateMapButtonVisuals();
        ValidateHostButton();
    }

    private void OnGameModeSelected(int modeIndex)
    {
        _selectedModeIndex = modeIndex;
        UpdateModeButtonVisuals();
        ValidateHostButton();
    }

    private void ValidateHostButton()
    {
        // The host button is only clickable if the player has chosen both a map and a mode
        bool isReady = (_selectedMapIndex >= 0 && _selectedModeIndex >= 0);
        if (hostMatchButton) hostMatchButton.Interactable(isReady);
    }

    // =========================================================
    // VISUAL UPDATES
    // =========================================================

    private void UpdateMapButtonVisuals()
    {
        if (mapButtons == null) return;
        for (int i = 0; i < mapButtons.Length; i++)
        {
            if (mapButtons[i] != null && mapButtons[i].normalImage != null)
            {
                mapButtons[i].normalImage.color = (i == _selectedMapIndex) ? selectedColor : normalColor;
            }
        }
    }

    private void UpdateModeButtonVisuals()
    {
        if (gameModeButtons == null) return;
        for (int i = 0; i < gameModeButtons.Length; i++)
        {
            if (gameModeButtons[i] != null && gameModeButtons[i].normalImage != null)
            {
                gameModeButtons[i].normalImage.color = (i == _selectedModeIndex) ? selectedColor : normalColor;
            }
        }
    }

    // =========================================================
    // ACTION ROUTING
    // =========================================================

    private void OnHostMatchClick()
    {
        if (_selectedMapIndex < 0 || _selectedModeIndex < 0) return;

        // 1. Tell Router to switch to the Loading Screen
        GameUIManager.Instance.ShowLoadingPanel();

        // 2. Start advertising the server on the Local Network (LAN)
        if (networkDiscovery != null)
        {
            networkDiscovery.AdvertiseServer();
        }
        else
        {
            Debug.LogWarning("[HostMenuUI] NetworkDiscovery is missing! LAN Players won't find this match.");
        }

        // 3. Command Mirror to start hosting (Rule 6: Use Singleton)
        if (Mirror.NetworkManager.singleton != null)
        {
#if UNITY_WEBGL
            // WebGL cannot act as a host/server in Mirror
            NetworkServer.listen = false;
#endif
            Mirror.NetworkManager.singleton.StartHost();
        }
        else
        {
            Debug.LogError("[HostMenuUI] Mirror GameNetworkManager Singleton not found!");
        }
    }

    private void OnBackClick()
    {
        GameUIManager.Instance.ShowMainMenu();
    }
}