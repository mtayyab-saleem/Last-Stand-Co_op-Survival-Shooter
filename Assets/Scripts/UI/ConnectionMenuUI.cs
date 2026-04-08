using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP; // Michsky UI Namespace
using TMPro;

/// <summary>
/// Handles the Server Discovery screen, allowing players to find and connect to LAN matches.
/// </summary>
public class ConnectionMenuUI : MonoBehaviour
{
    [Header("Network Discovery")]
    [Tooltip("If left empty, the script will automatically find it in the scene.")]
    [SerializeField] private NetworkDiscovery networkDiscovery;

    [Header("UI References")]
    [SerializeField] private ListView serverListView;
    [SerializeField] private ButtonManager refreshButton;
    [SerializeField] private ButtonManager backButton;

    // Tracks servers to prevent duplicate buttons from appearing in the list
    private Dictionary<long, GameObject> _foundServers = new Dictionary<long, GameObject>();

    private void Start()
    {
        InitializeDiscovery();
        SetupButtons();
    }

    private void OnEnable()
    {
        // Automatically start searching for servers the moment this panel is opened
        StartDiscoverySearch();
    }

    private void OnDisable()
    {
        // Save network bandwidth by stopping the search when the panel is closed
        if (networkDiscovery != null)
        {
            networkDiscovery.StopDiscovery();
        }
    }

    private void InitializeDiscovery()
    {
        if (networkDiscovery == null)
        {
            networkDiscovery = Object.FindFirstObjectByType<NetworkDiscovery>();
        }

        // Securely bind the event listener to avoid double-subscriptions
        if (networkDiscovery != null)
        {
            networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
            networkDiscovery.OnServerFound.AddListener(OnServerFound);
        }
        else
        {
            Debug.LogError("[ConnectionMenuUI] NetworkDiscovery component is missing from the scene!");
        }
    }

    private void SetupButtons()
    {
        if (refreshButton) refreshButton.onClick.AddListener(StartDiscoverySearch);
        if (backButton) backButton.onClick.AddListener(OnBackClick);
    }

    // =========================================================
    // SERVER DISCOVERY LOGIC
    // =========================================================

    private void StartDiscoverySearch()
    {
        if (serverListView == null || serverListView.itemParent == null) return;

        // 1. Clear old UI buttons
        foreach (Transform child in serverListView.itemParent)
        {
            Destroy(child.gameObject);
        }
        _foundServers.Clear();

        // 2. Restart the Mirror Discovery listener
        if (networkDiscovery != null)
        {
            networkDiscovery.StopDiscovery();
            networkDiscovery.StartDiscovery();
        }
    }

    private void OnServerFound(ServerResponse info)
    {
        // Prevent duplicate server listings
        if (_foundServers.ContainsKey(info.serverId)) return;
        if (serverListView == null || serverListView.itemPreset == null) return;

        // 1. Instantiate the UI button manually
        GameObject newItem = Instantiate(serverListView.itemPreset, serverListView.itemParent);

        // 2. Format the display name (e.g., using the host's IP Address)
        string serverName = info.EndPoint.Address.ToString();

        // 3. Configure the Michsky Button
        ButtonManager btnManager = newItem.GetComponent<ButtonManager>();
        if (btnManager != null)
        {
            btnManager.buttonText = serverName; 
            if (btnManager.normalText != null) btnManager.normalText.text = serverName;

            btnManager.onClick.RemoveAllListeners();
            btnManager.onClick.AddListener(() => ConnectToFoundServer(info));

            btnManager.UpdateUI();
        }
        else
        {
            // Standard Unity UI Fallback just in case Michsky is missing
            TextMeshProUGUI txt = newItem.GetComponentInChildren<TextMeshProUGUI>();
            if (txt) txt.text = serverName;

            UnityEngine.UI.Button btn = newItem.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn) btn.onClick.AddListener(() => ConnectToFoundServer(info));
        }

        // Register the server to prevent duplicates
        _foundServers.Add(info.serverId, newItem);
    }

    // =========================================================
    // ACTION ROUTING
    // =========================================================

    private void ConnectToFoundServer(ServerResponse info)
    {
        if (networkDiscovery != null)
        {
            networkDiscovery.StopDiscovery();
        }

        // 1. Tell Router to switch to the Loading Screen
        GameUIManager.Instance.ShowLoadingPanel();

        // 2. Command Mirror to connect to the specific server URI
        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StartClient(info.uri);
        }
    }

    private void OnBackClick()
    {
        GameUIManager.Instance.ShowMainMenu();
    }
}