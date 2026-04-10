using UnityEngine;
using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP;
using TMPro;

/// <summary>
/// Handles the Server Discovery screen safely, guaranteeing clean reconnections.
/// </summary>
public class ConnectionMenuUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ListView serverListView;
    [SerializeField] private ButtonManager refreshButton;
    [SerializeField] private ButtonManager backButton;

    private NetworkDiscovery _networkDiscovery;

    // Tracks servers to prevent duplicate buttons from appearing in the list
    private Dictionary<long, GameObject> _foundServers = new Dictionary<long, GameObject>();

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        InitializeDiscovery();
        StartDiscoverySearch();
    }

    private void OnDisable()
    {
        if (_networkDiscovery != null)
        {
            _networkDiscovery.StopDiscovery();
        }
    }

    private void InitializeDiscovery()
    {
        if (Mirror.NetworkManager.singleton != null)
        {
            _networkDiscovery = Mirror.NetworkManager.singleton.GetComponent<NetworkDiscovery>();
        }

        if (_networkDiscovery != null)
        {
            _networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
            _networkDiscovery.OnServerFound.AddListener(OnServerFound);
        }
        else
        {
            Debug.LogError("[ConnectionMenuUI] NetworkDiscovery component missing on Mirror NetworkManager!");
        }
    }

    private void SetupButtons()
    {
        if (refreshButton) refreshButton.onClick.AddListener(StartDiscoverySearch);
        if (backButton) backButton.onClick.AddListener(OnBackClick);
    }

    private void StartDiscoverySearch()
    {
        if (serverListView == null || serverListView.itemParent == null) return;

        foreach (Transform child in serverListView.itemParent)
        {
            Destroy(child.gameObject);
        }
        _foundServers.Clear();

        // Restart Mirror Discovery
        if (_networkDiscovery != null)
        {
            try
            {
                _networkDiscovery.StopDiscovery();
                Invoke(nameof(ExecuteStartDiscovery), 0.1f);
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ConnectionMenuUI] Failed to restart discovery: " + e.Message);
            }
        }
    }

    private void ExecuteStartDiscovery()
    {
        if (this.gameObject.activeInHierarchy && _networkDiscovery != null && !NetworkServer.active)
        {
            _networkDiscovery.StartDiscovery();
            Debug.Log("[ConnectionMenuUI] Scanning for LAN Servers...");
        }
    }

    private void OnServerFound(ServerResponse info)
    {
        if (_foundServers.ContainsKey(info.serverId)) return;
        if (serverListView == null || serverListView.itemPreset == null) return;

        GameObject newItem = Instantiate(serverListView.itemPreset, serverListView.itemParent);

        // Format the display host's IP 
        string serverName = info.EndPoint.Address.ToString();

        // Configure Button
        ButtonManager btnManager = newItem.GetComponent<ButtonManager>();
        if (btnManager != null)
        {
            btnManager.buttonText = serverName;
            if (btnManager.normalText != null) btnManager.normalText.text = serverName;

            btnManager.onClick.RemoveAllListeners();
            btnManager.onClick.AddListener(() => ConnectToFoundServer(info));

            btnManager.UpdateUI();
        }

        // Register the server 
        _foundServers.Add(info.serverId, newItem);
    }

    private void ConnectToFoundServer(ServerResponse info)
    {
        if (_networkDiscovery != null)
        {
            _networkDiscovery.StopDiscovery();
        }

        GameUIManager.Instance.ShowLoadingPanel();

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