// ============================================================
// LobbyUIController.cs
// PURPOSE: Handle server DISCOVERY and server LIST display.
//          When player clicks "Join", this script:
//          1. Starts broadcasting search on local network
//          2. Shows found servers as clickable buttons
//          3. Connects to selected server
//
// USED BY: NetworkUIController.cs calls StartSearch() on this
// SINGLE RESPONSIBILITY: Server discovery and list only.
// ============================================================

using UnityEngine;
using Mirror;
using Mirror.Discovery;
using System.Collections.Generic;
using Michsky.MUIP;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// Manages the "find servers" panel.
/// NOT inside Mirror namespace — it just uses Mirror's Discovery API.
/// </summary>
public class LobbyUIController : MonoBehaviour
{
    // ── Inspector Fields ─────────────────────────────────────
    [Header("Mirror Components (assign in Inspector)")]
    [SerializeField] private NetworkDiscovery _networkDiscovery;
    [SerializeField] private NetworkManager _networkManager;

    [Header("UI: Server List")]
    [Tooltip("The Michsky ListView that shows found servers")]
    [SerializeField] private ListView _serverListView;

    [Tooltip("Refresh button to search again")]
    [SerializeField] private ButtonManager _refreshButton;

    // ── Private State ────────────────────────────────────────
    // Tracks servers we've already shown so we don't add duplicates
    private readonly Dictionary<long, GameObject> _foundServers = new();

    // ── Events ───────────────────────────────────────────────
    // NetworkUIController listens to this to show loading screen
    public event System.Action<ServerResponse> OnServerSelected;

    // ════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        // Auto-find on same GameObject if not assigned
        if (_networkDiscovery == null)
            _networkDiscovery = GetComponent<NetworkDiscovery>();
        if (_networkManager == null)
            _networkManager = GetComponent<NetworkManager>();
    }

    private void OnEnable()
    {
        if (_networkDiscovery != null)
            _networkDiscovery.OnServerFound.AddListener(OnServerFound);

        if (_refreshButton != null)
            _refreshButton.onClick.AddListener(StartSearch);
    }

    private void OnDisable()
    {
        if (_networkDiscovery != null)
            _networkDiscovery.OnServerFound.RemoveListener(OnServerFound);

        if (_refreshButton != null)
            _refreshButton.onClick.RemoveListener(StartSearch);

        StopSearch();
    }

    // ════════════════════════════════════════════════════════
    // PUBLIC API — Called by NetworkUIController
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Start searching for local servers.
    /// Call this when the "Join" panel opens.
    /// </summary>
    public void StartSearch()
    {
        ClearServerList();

        if (_networkDiscovery == null)
        {
            Debug.LogError("[LobbyUIController] NetworkDiscovery is not assigned!");
            return;
        }

        _networkDiscovery.StopDiscovery();
        _networkDiscovery.StartDiscovery();
        Debug.Log("[LobbyUIController] Searching for local servers...");
    }

    /// <summary>
    /// Stop searching. Call this when closing the join panel.
    /// </summary>
    public void StopSearch()
    {
        _networkDiscovery?.StopDiscovery();
    }

    // ════════════════════════════════════════════════════════
    // SERVER DISCOVERY CALLBACK
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Mirror calls this automatically each time a server responds to our broadcast.
    /// </summary>
    private void OnServerFound(ServerResponse serverInfo)
    {
        // Skip if we already have a button for this server
        if (_foundServers.ContainsKey(serverInfo.serverId)) return;

        // Skip if list UI is missing
        if (_serverListView == null || _serverListView.itemParent == null) return;

        CreateServerButton(serverInfo);
    }

    // ════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════

    private void CreateServerButton(ServerResponse serverInfo)
    {
        if (_serverListView.itemPreset == null)
        {
            Debug.LogError("[LobbyUIController] ListView itemPreset is not assigned.");
            return;
        }

        // Create a new button from the preset
        var buttonObject = Instantiate(_serverListView.itemPreset, _serverListView.itemParent);
        string serverAddress = serverInfo.EndPoint.Address.ToString();

        // Try to set the button text via Michsky ButtonManager
        var btnManager = buttonObject.GetComponent<ButtonManager>();
        if (btnManager != null)
        {
            btnManager.buttonText = serverAddress;
            if (btnManager.normalText != null)
                btnManager.normalText.text = serverAddress;

            btnManager.onClick.RemoveAllListeners();
            btnManager.onClick.AddListener(() => ConnectToServer(serverInfo));
            btnManager.UpdateUI();
        }
        else
        {
            // Fallback: standard Unity Button
            var textComponent = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            if (textComponent != null) textComponent.text = serverAddress;

            var button = buttonObject.GetComponentInChildren<Button>();
            if (button != null) button.onClick.AddListener(() => ConnectToServer(serverInfo));
        }

        _foundServers.Add(serverInfo.serverId, buttonObject);
        Debug.Log($"[LobbyUIController] Found server: {serverAddress}");
    }

    private void ConnectToServer(ServerResponse serverInfo)
    {
        StopSearch();

        if (_networkManager == null)
        {
            Debug.LogError("[LobbyUIController] NetworkManager not assigned!");
            return;
        }

        _networkManager.StartClient(serverInfo.uri);

        // Notify NetworkUIController to show the loading screen
        OnServerSelected?.Invoke(serverInfo);

        Debug.Log($"[LobbyUIController] Connecting to {serverInfo.EndPoint.Address}...");
    }

    private void ClearServerList()
    {
        if (_serverListView?.itemParent == null) return;

        // Destroy all existing server buttons
        foreach (Transform child in _serverListView.itemParent)
            Destroy(child.gameObject);

        _foundServers.Clear();
    }
}