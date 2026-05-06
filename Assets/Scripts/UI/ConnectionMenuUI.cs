using UnityEngine;
using System.Collections;
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

    [Header("Discovery Settings")]
    [Tooltip("How often (seconds) to automatically re-scan for new servers while the panel is open.")]
    [SerializeField] private float autoRefreshInterval = 5f;

    private NetworkDiscovery _networkDiscovery;
    private Coroutine _autoRefreshCoroutine;

    private Dictionary<long, GameObject> _foundServers = new Dictionary<long, GameObject>();

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        InitializeDiscovery();
        StartDiscoverySearch();

        if (_autoRefreshCoroutine != null) StopCoroutine(_autoRefreshCoroutine);
        _autoRefreshCoroutine = StartCoroutine(PeriodicDiscoveryRefresh());
    }

    private void OnDisable()
    {
        if (_autoRefreshCoroutine != null)
        {
            StopCoroutine(_autoRefreshCoroutine);
            _autoRefreshCoroutine = null;
        }

        if (_networkDiscovery != null)
            _networkDiscovery.StopDiscovery();
    }

    private void InitializeDiscovery()
    {
        if (Mirror.NetworkManager.singleton != null)
            _networkDiscovery = Mirror.NetworkManager.singleton.GetComponent<NetworkDiscovery>();

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
        InitializeDiscovery();

        if (serverListView != null && serverListView.itemParent != null)
        {
            foreach (Transform child in serverListView.itemParent)
                Destroy(child.gameObject);
            _foundServers.Clear();
        }

        if (_networkDiscovery != null)
        {
            _networkDiscovery.StopDiscovery();
            Invoke(nameof(ExecuteStartDiscovery), 0.15f);
        }
    }

    private void ExecuteStartDiscovery()
    {
        if (this.gameObject.activeInHierarchy && _networkDiscovery != null && !NetworkServer.active)
        {
            _networkDiscovery.StartDiscovery();
            Debug.Log("[ConnectionMenuUI] Scanning for LAN servers...");
        }
    }

    private IEnumerator PeriodicDiscoveryRefresh()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoRefreshInterval);

            if (!this.gameObject.activeInHierarchy) yield break;
            if (NetworkServer.active) yield break;

            // Re-fetch in case reference went stale after a disconnect
            InitializeDiscovery();
            if (_networkDiscovery == null) continue;

            // --- Stop phase (no yield inside, so try-catch is legal) ---
            bool stopped = TryStopDiscovery();
            if (!stopped) continue;

            // --- Yield is OUTSIDE try-catch (C# requirement) ---
            yield return new WaitForSeconds(0.15f);

            // --- Start phase (no yield inside, so try-catch is legal) ---
            TryStartDiscovery();
        }
    }

    // Separate helper so try-catch wraps only non-yield code
    private bool TryStopDiscovery()
    {
        try
        {
            _networkDiscovery.StopDiscovery();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ConnectionMenuUI] StopDiscovery failed: " + e.Message);
            return false;
        }
    }

    // Separate helper so try-catch wraps only non-yield code
    private void TryStartDiscovery()
    {
        if (!this.gameObject.activeInHierarchy || NetworkServer.active) return;

        try
        {
            _networkDiscovery.StartDiscovery();
            Debug.Log("[ConnectionMenuUI] Auto-refreshed server discovery.");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ConnectionMenuUI] StartDiscovery failed: " + e.Message);
        }
    }

    private void OnServerFound(ServerResponse info)
    {
        if (_foundServers.ContainsKey(info.serverId)) return;
        if (serverListView == null || serverListView.itemPreset == null) return;

        GameObject newItem = Instantiate(serverListView.itemPreset, serverListView.itemParent);
        string serverName = info.EndPoint.Address.ToString();

        ButtonManager btnManager = newItem.GetComponent<ButtonManager>();
        if (btnManager != null)
        {
            btnManager.buttonText = serverName;
            if (btnManager.normalText != null) btnManager.normalText.text = serverName;
            btnManager.onClick.RemoveAllListeners();
            btnManager.onClick.AddListener(() => ConnectToFoundServer(info));
            btnManager.UpdateUI();
        }

        _foundServers.Add(info.serverId, newItem);
    }

    private void ConnectToFoundServer(ServerResponse info)
    {
        if (_autoRefreshCoroutine != null)
        {
            StopCoroutine(_autoRefreshCoroutine);
            _autoRefreshCoroutine = null;
        }

        if (_networkDiscovery != null) _networkDiscovery.StopDiscovery();

        GameUIManager.Instance.ShowLoadingPanel();

        if (Mirror.NetworkManager.singleton != null)
            Mirror.NetworkManager.singleton.StartClient(info.uri);
    }

    private void OnBackClick()
    {
        GameUIManager.Instance.ShowMainMenu();
    }
}