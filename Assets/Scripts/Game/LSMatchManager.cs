using UnityEngine;
using Mirror;
using Mirror.Discovery; // Discovery ke liye
using System.Collections.Generic;
using System.Linq;

public class LSMatchManager : NetworkBehaviour
{
    public static LSMatchManager Instance;

    public enum GameMode { Solo, Duo, Squad }

    [Header("Match Settings")]
    [SyncVar(hook = nameof(OnModeChanged))] public GameMode currentMode = GameMode.Solo;
    [SyncVar(hook = nameof(OnHostStartChanged))] public bool canHostStart = false;

    [Header("Lobby Settings")]
    [Tooltip("Maximum players allowed in the match (Host + Clients)")]
    public int maxPlayers = 4; 

    private bool hasMatchStarted = false;

    public readonly SyncList<LSPlayer> players = new SyncList<LSPlayer>();
    private NetworkDiscovery networkDiscovery;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
        networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
    }

    public override void OnStartServer()
    {
        players.Clear(); // Force clean slate in case old client data remained

        players.Callback -= OnPlayersListChanged;
        players.Callback += OnPlayersListChanged;

        int savedMode = PlayerPrefs.GetInt("HostSelectedMode", 0);
        currentMode = (GameMode)savedMode;
    }

    [Server]
    public void RegisterPlayer(LSPlayer player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }

        if (players.Count == 1)
        {
            player.isGameHost = true;
            player.isReady = true;
        }
        else
        {
            player.isGameHost = false;
            player.isReady = false;
        }

        UpdateReadyState();
        CheckDiscoveryState();
        RpcRefreshUI();
    }

    [Server]
    public void UpdateReadyState()
    {
        int readyClients = 0;
        int totalClients = 0;

        foreach (var p in players)
        {
            // Ab hum isServer ki bajaye apna isGameHost check kar rahe hain
            if (!p.isGameHost)
            {
                totalClients++;
                if (p.isReady) readyClients++;
            }
            else
            {
                p.isReady = true; // Make sure host is strictly ready
            }
        }

        if (currentMode == GameMode.Solo && totalClients == 0)
        {
            canHostStart = true;
        }
        else
        {
            canHostStart = (totalClients > 0 && readyClients == totalClients);
        }

        RpcRefreshUI();
    }

    [Server]
    public void UnregisterPlayer(LSPlayer player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }
        UpdateReadyState();
        CheckDiscoveryState(); // Player chala gaya toh discovery wapas on
        RpcRefreshUI();
    }

    [Server]
    private void CheckDiscoveryState()
    {
        // Agar match start ho chuka hai, toh discovery kabhi on nahi hogi
        if (hasMatchStarted) return;

        if (networkDiscovery == null) networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
        if (networkDiscovery == null) return;

        if (players.Count >= maxPlayers)
        {
            // Lobby full ho gayi! Discovery band karo
            networkDiscovery.StopDiscovery();
            Debug.Log("[LSMatchManager] Lobby is Full. Discovery Stopped.");
        }
        else
        {
            // Space baqi hai! Discovery ko restart karo taake players join kar sakein
            networkDiscovery.StopDiscovery();
            networkDiscovery.AdvertiseServer();
            Debug.Log("[LSMatchManager] Space available. Discovery Advertising...");
        }
    }

    

    [Server]
    public void StartMatch()
    {
        if (!canHostStart) return;

        // Match start hone ka flag on kar diya
        hasMatchStarted = true;

        // Discovery pakki band
        if (networkDiscovery == null) networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
        if (networkDiscovery != null) networkDiscovery.StopDiscovery();

        AssignTeams();

        NetworkManager.singleton.ServerChangeScene("GameScene");
    }

    [Server]
    private void AssignTeams()
    {
        // Players ko completely random mix kar do
        var shuffledPlayers = players.OrderBy(x => Random.value).ToList();

        // Har mode ki team size limit kya hai?
        int teamSizeLimit = 1; // Solo = 1 player
        if (currentMode == GameMode.Duo) teamSizeLimit = 2; // Duo = 2 players
        else if (currentMode == GameMode.Squad) teamSizeLimit = 4; // Squad = 4 players

        for (int i = 0; i < shuffledPlayers.Count; i++)
        {
            // Yeh formula automatically players ko teams mein divide kar dega based on team limits.
            // Agar limit 4 hai aur i=0,1,2,3 toh answer 0 aayega (Team 0).
            // Jab i=4,5,6 hoga toh answer 1 aayega (Team 1).
            shuffledPlayers[i].teamID = i / teamSizeLimit;
        }
    }

    // --- UI Update Hooks ---

    private void OnPlayersListChanged(SyncList<LSPlayer>.Operation op, int index, LSPlayer oldItem, LSPlayer newItem)
    {
        if (isClient) UpdateLocalUI();
    }

    private void OnModeChanged(GameMode oldMode, GameMode newMode) { if (isClient) UpdateLocalUI(); }
    private void OnHostStartChanged(bool oldVal, bool newVal) { if (isClient) UpdateLocalUI(); }

    [ClientRpc]
    private void RpcRefreshUI()
    {
        UpdateLocalUI();
    }

    [Client]
    public void UpdateLocalUI()
    {
        if (LobbyUIManager.Instance == null) return;

        // Forcefully evaluate actual network state to prevent Identity Crisis
        bool isHost = NetworkServer.active;
        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();

        List<LSPlayer> allPlayers = new List<LSPlayer>();
        foreach (var p in players)
        {
            if (p == null) continue;
            allPlayers.Add(p);
        }

        LobbyUIManager.Instance.RefreshLobbyUI(isHost, localPlayer, allPlayers, (int)currentMode, canHostStart);
    }

    public override void OnStopServer()
    {
        players.Clear();
        hasMatchStarted = false;
        canHostStart = false;
        
        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }
        
        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        hasMatchStarted = false;
        canHostStart = false;
        
        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }
        
        base.OnStopClient();
    }
}