//using UnityEngine;
//using Mirror;
//using Mirror.Discovery; // Discovery ke liye
//using System.Collections.Generic;
//using System.Linq;

//public class LSMatchManager : NetworkBehaviour
//{
//    public static LSMatchManager Instance;

//    public enum GameMode { Solo, Duo, Squad }

//    [Header("Match Settings")]
//    [SyncVar(hook = nameof(OnModeChanged))] public GameMode currentMode = GameMode.Solo;
//    [SyncVar(hook = nameof(OnHostStartChanged))] public bool canHostStart = false;

//    [Header("Lobby Settings")]
//    [Tooltip("Maximum players allowed in the match (Host + Clients)")]
//    public int maxPlayers = 4; 

//    private bool hasMatchStarted = false;

//    public readonly SyncList<LSPlayer> players = new SyncList<LSPlayer>();
//    private NetworkDiscovery networkDiscovery;

//    void Awake()
//    {
//        if (Instance == null) Instance = this;
//        else Destroy(gameObject);

//        DontDestroyOnLoad(gameObject);
//        networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
//    }

//    public override void OnStartServer()
//    {
//        players.Clear(); // Force clean slate in case old client data remained

//        players.Callback -= OnPlayersListChanged;
//        players.Callback += OnPlayersListChanged;

//        int savedMode = PlayerPrefs.GetInt("HostSelectedMode", 0);
//        currentMode = (GameMode)savedMode;
//    }

//    [Server]
//    public void RegisterPlayer(LSPlayer player)
//    {
//        if (!players.Contains(player))
//        {
//            players.Add(player);
//        }

//        if (players.Count == 1)
//        {
//            player.isGameHost = true;
//            player.isReady = true;
//        }
//        else
//        {
//            player.isGameHost = false;
//            player.isReady = false;
//        }

//        UpdateReadyState();
//        CheckDiscoveryState();
//        RpcRefreshUI();
//    }

//    [Server]
//    public void UpdateReadyState()
//    {
//        int readyClients = 0;
//        int totalClients = 0;

//        foreach (var p in players)
//        {
//            // Ab hum isServer ki bajaye apna isGameHost check kar rahe hain
//            if (!p.isGameHost)
//            {
//                totalClients++;
//                if (p.isReady) readyClients++;
//            }
//            else
//            {
//                p.isReady = true; // Make sure host is strictly ready
//            }
//        }

//        if (currentMode == GameMode.Solo && totalClients == 0)
//        {
//            canHostStart = true;
//        }
//        else
//        {
//            canHostStart = (totalClients > 0 && readyClients == totalClients);
//        }

//        RpcRefreshUI();
//    }

//    [Server]
//    public void UnregisterPlayer(LSPlayer player)
//    {
//        if (players.Contains(player))
//        {
//            players.Remove(player);
//        }
//        UpdateReadyState();
//        CheckDiscoveryState(); // Player chala gaya toh discovery wapas on
//        RpcRefreshUI();
//    }

//    [Server]
//    private void CheckDiscoveryState()
//    {
//        // Agar match start ho chuka hai, toh discovery kabhi on nahi hogi
//        if (hasMatchStarted) return;

//        if (networkDiscovery == null) networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
//        if (networkDiscovery == null) return;

//        if (players.Count >= maxPlayers)
//        {
//            // Lobby full ho gayi! Discovery band karo
//            networkDiscovery.StopDiscovery();
//            Debug.Log("[LSMatchManager] Lobby is Full. Discovery Stopped.");
//        }
//        else
//        {
//            // Space baqi hai! Discovery ko restart karo taake players join kar sakein
//            networkDiscovery.StopDiscovery();
//            networkDiscovery.AdvertiseServer();
//            Debug.Log("[LSMatchManager] Space available. Discovery Advertising...");
//        }
//    }



//    [Server]
//    public void StartMatch()
//    {
//        if (!canHostStart) return;

//        // Match start hone ka flag on kar diya
//        hasMatchStarted = true;

//        // Discovery pakki band
//        if (networkDiscovery == null) networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();
//        if (networkDiscovery != null) networkDiscovery.StopDiscovery();

//        AssignTeams();

//        NetworkManager.singleton.ServerChangeScene("GameScene");
//    }

//    [Server]
//    private void AssignTeams()
//    {
//        // Players ko completely random mix kar do
//        var shuffledPlayers = players.OrderBy(x => Random.value).ToList();

//        // Har mode ki team size limit kya hai?
//        int teamSizeLimit = 1; // Solo = 1 player
//        if (currentMode == GameMode.Duo) teamSizeLimit = 2; // Duo = 2 players
//        else if (currentMode == GameMode.Squad) teamSizeLimit = 4; // Squad = 4 players

//        for (int i = 0; i < shuffledPlayers.Count; i++)
//        {
//            // Yeh formula automatically players ko teams mein divide kar dega based on team limits.
//            // Agar limit 4 hai aur i=0,1,2,3 toh answer 0 aayega (Team 0).
//            // Jab i=4,5,6 hoga toh answer 1 aayega (Team 1).
//            shuffledPlayers[i].teamID = i / teamSizeLimit;
//        }
//    }

//    // --- UI Update Hooks ---

//    private void OnPlayersListChanged(SyncList<LSPlayer>.Operation op, int index, LSPlayer oldItem, LSPlayer newItem)
//    {
//        if (isClient) UpdateLocalUI();
//    }

//    private void OnModeChanged(GameMode oldMode, GameMode newMode) { if (isClient) UpdateLocalUI(); }
//    private void OnHostStartChanged(bool oldVal, bool newVal) { if (isClient) UpdateLocalUI(); }

//    [ClientRpc]
//    private void RpcRefreshUI()
//    {
//        UpdateLocalUI();
//    }

//    [Client]
//    public void UpdateLocalUI()
//    {
//        if (LobbyUIManager.Instance == null) return;

//        // Forcefully evaluate actual network state to prevent Identity Crisis
//        bool isHost = NetworkServer.active;
//        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();

//        List<LSPlayer> allPlayers = new List<LSPlayer>();
//        foreach (var p in players)
//        {
//            if (p == null) continue;
//            allPlayers.Add(p);
//        }

//        LobbyUIManager.Instance.RefreshLobbyUI(isHost, localPlayer, allPlayers, (int)currentMode, canHostStart);
//    }

//    public override void OnStopServer()
//    {
//        players.Clear();
//        hasMatchStarted = false;
//        canHostStart = false;

//        if (LobbyUIManager.Instance != null)
//        {
//            LobbyUIManager.Instance.ResetUI();
//        }

//        base.OnStopServer();
//    }

//    public override void OnStopClient()
//    {
//        hasMatchStarted = false;
//        canHostStart = false;

//        if (LobbyUIManager.Instance != null)
//        {
//            LobbyUIManager.Instance.ResetUI();
//        }

//        base.OnStopClient();
//    }
//}





using UnityEngine;
using Mirror;
using Mirror.Discovery;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class LSMatchManager : NetworkBehaviour
{
    public static LSMatchManager Instance;

    public enum GameMode { Solo, Duo, Squad }

    [Header("Match Settings")]
    [SyncVar(hook = nameof(OnModeChanged))]
    public GameMode currentMode = GameMode.Solo;

    [SyncVar(hook = nameof(OnHostStartChanged))]
    public bool canHostStart = false;

    [Header("Game Mode Rules")]
    [Tooltip("Optional rules asset. If not assigned, safe default values will be used.")]
    [SerializeField] private GameModeRulesSO gameModeRules;

    [Header("Debug")]
    [Tooltip("Enable simple console logs for mode and team testing.")]
    [SerializeField] private bool enableTeamDebug = true;

    [Header("Lobby Settings")]
    [Tooltip("Maximum players allowed in the lobby (Host + Clients).")]
    public int maxPlayers = 4;

    private bool hasMatchStarted = false;

    public readonly SyncList<LSPlayer> players = new SyncList<LSPlayer>();
    private NetworkDiscovery networkDiscovery;

    // Stores the assigned team by network connection.
    // This keeps the team ID safe when the gameplay scene loads.
    private readonly Dictionary<int, int> savedTeamByConnection = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();

        // Listen for scene changes so team IDs can be restored safely.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    public override void OnStartServer()
    {
        // Start every new server session with clean match data.
        players.Clear();
        savedTeamByConnection.Clear();

        players.Callback -= OnPlayersListChanged;
        players.Callback += OnPlayersListChanged;

        // Load the mode selected by the host from the Host Menu.
        int savedMode = PlayerPrefs.GetInt("HostSelectedMode", 0);
        currentMode = (GameMode)savedMode;

        if (enableTeamDebug)
        {
            Debug.Log($"[TEAM DEBUG] Selected Mode = {currentMode} | Saved Mode Value = {savedMode}");
        }
    }

    [Server]
    public void RegisterPlayer(LSPlayer player)
    {
        if (!players.Contains(player))
        {
            players.Add(player);
        }

        // If the match already started, restore the saved team for this connection.
        RestoreSavedTeam(player);

        // The first registered player is always the host.
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

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Player Registered = {player.playerName} | " +
                $"Host = {player.isGameHost} | Total Players = {players.Count}"
            );
        }

        UpdateReadyState();
        CheckDiscoveryState();
        RpcRefreshUI();
    }

    [Server]
    public void UpdateReadyState()
    {
        int totalPlayers = players.Count;
        int minimumPlayers = GetMinimumPlayersToStart();

        bool allClientsReady = true;

        foreach (var player in players)
        {
            if (player == null)
                continue;

            // The host is always considered ready.
            if (player.isGameHost)
            {
                player.isReady = true;
                continue;
            }

            // If even one client is not ready, the match cannot start.
            if (!player.isReady)
            {
                allClientsReady = false;
            }
        }

        // Match can start only when:
        // 1. The selected mode has enough players.
        // 2. Every connected client is ready.
        canHostStart = totalPlayers >= minimumPlayers && allClientsReady;

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Ready Check | Mode = {currentMode} | " +
                $"Players = {totalPlayers} | Minimum = {minimumPlayers} | " +
                $"All Clients Ready = {allClientsReady} | Can Start = {canHostStart}"
            );
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
        CheckDiscoveryState();
        RpcRefreshUI();
    }

    [Server]
    private void CheckDiscoveryState()
    {
        // Never advertise the lobby after the match has started.
        if (hasMatchStarted)
            return;

        if (networkDiscovery == null)
            networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();

        if (networkDiscovery == null)
            return;

        if (players.Count >= maxPlayers)
        {
            networkDiscovery.StopDiscovery();
            Debug.Log("[LSMatchManager] Lobby is full. Discovery stopped.");
        }
        else
        {
            networkDiscovery.StopDiscovery();
            networkDiscovery.AdvertiseServer();
            Debug.Log("[LSMatchManager] Lobby has space. Discovery advertising.");
        }
    }

    [Server]
    public void StartMatch()
    {
        if (!canHostStart)
        {
            if (enableTeamDebug)
            {
                Debug.LogWarning(
                    $"[TEAM DEBUG] Start blocked | Mode = {currentMode} | Players = {players.Count}"
                );
            }

            return;
        }

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Starting Match | Mode = {currentMode} | Players = {players.Count}"
            );
        }

        hasMatchStarted = true;

        // Stop advertising before entering the gameplay scene.
        if (networkDiscovery == null)
            networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();

        if (networkDiscovery != null)
            networkDiscovery.StopDiscovery();

        AssignTeams();

        NetworkManager.singleton.ServerChangeScene("GameScene");
    }

    [Server]
    private void AssignTeams()
    {
        if (players.Count == 0)
            return;

        // Randomize players before assigning teams.
        List<LSPlayer> shuffledPlayers = players
            .Where(player => player != null)
            .OrderBy(player => Random.value)
            .ToList();

        int maxTeamSize = GetMaximumPlayersPerTeam();

        // Example:
        // Squad with 9 players and max team size 4:
        // ceil(9 / 4) = 3 teams.
        int teamCount = Mathf.CeilToInt(shuffledPlayers.Count / (float)maxTeamSize);
        teamCount = Mathf.Max(1, teamCount);

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Assigning Teams | Mode = {currentMode} | " +
                $"Players = {shuffledPlayers.Count} | Max Team Size = {maxTeamSize} | " +
                $"Team Count = {teamCount}"
            );
        }

        // Clear old team records before creating a new match setup.
        savedTeamByConnection.Clear();

        // Deal players to teams one-by-one like dealing cards.
        // This keeps team sizes as balanced as possible.
        for (int i = 0; i < shuffledPlayers.Count; i++)
        {
            LSPlayer player = shuffledPlayers[i];
            int teamID = i % teamCount;

            player.teamID = teamID;

            // Save the team using the player's network connection ID.
            // Connection ID remains the useful link when the scene changes.
            if (player.connectionToClient != null)
            {
                int connectionId = player.connectionToClient.connectionId;
                savedTeamByConnection[connectionId] = teamID;
            }

            if (enableTeamDebug)
            {
                int connectionId = player.connectionToClient != null
                    ? player.connectionToClient.connectionId
                    : -1;

                Debug.Log(
                    $"[TEAM DEBUG] Player = {player.playerName} | " +
                    $"NetId = {player.netId} | ConnectionId = {connectionId} | TeamID = {player.teamID}"
                );
            }
        }
    }

    [Server]
    private void RestoreSavedTeam(LSPlayer player)
    {
        if (player == null || player.connectionToClient == null)
            return;

        int connectionId = player.connectionToClient.connectionId;

        if (savedTeamByConnection.TryGetValue(connectionId, out int savedTeamID))
        {
            player.teamID = savedTeamID;

            if (enableTeamDebug)
            {
                Debug.Log(
                    $"[TEAM DEBUG] Team Restored | Player = {player.playerName} | " +
                    $"ConnectionId = {connectionId} | TeamID = {savedTeamID}"
                );
            }
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!NetworkServer.active || !hasMatchStarted)
            return;

        // Wait briefly so Mirror can finish moving/spawning player objects.
        StartCoroutine(RestoreTeamsAfterSceneLoad());
    }

    private IEnumerator RestoreTeamsAfterSceneLoad()
    {
        // Two frames are enough for the normal Mirror scene transition flow.
        yield return null;
        yield return null;

        if (!NetworkServer.active || !hasMatchStarted)
            yield break;

        foreach (LSPlayer player in players)
        {
            if (player != null)
            {
                RestoreSavedTeam(player);
            }
        }

        if (enableTeamDebug)
        {
            Debug.Log($"[TEAM DEBUG] Team restore check completed after scene load.");
        }
    }

    private int GetMinimumPlayersToStart()
    {
        // Use the ScriptableObject value when available.
        if (gameModeRules != null)
        {
            GameModeRulesSO.ModeRule rule = gameModeRules.GetRule(currentMode);

            if (rule != null)
                return Mathf.Max(1, rule.minimumPlayersToStart);
        }

        // Safe fallback values keep the current game flow working
        // even if the rules asset is not assigned.
        switch (currentMode)
        {
            case GameMode.Duo:
                return 2;

            case GameMode.Squad:
                return 4;

            default:
                return 1;
        }
    }

    private int GetMaximumPlayersPerTeam()
    {
        // Use the ScriptableObject value when available.
        if (gameModeRules != null)
        {
            GameModeRulesSO.ModeRule rule = gameModeRules.GetRule(currentMode);

            if (rule != null)
                return Mathf.Max(1, rule.maximumPlayersPerTeam);
        }

        // Safe fallback values.
        switch (currentMode)
        {
            case GameMode.Duo:
                return 2;

            case GameMode.Squad:
                return 4;

            default:
                return 1;
        }
    }

    // -------------------------
    // UI Update Hooks
    // -------------------------

    private void OnPlayersListChanged(
        SyncList<LSPlayer>.Operation op,
        int index,
        LSPlayer oldItem,
        LSPlayer newItem)
    {
        if (isClient)
            UpdateLocalUI();
    }

    private void OnModeChanged(GameMode oldMode, GameMode newMode)
    {
        if (isClient)
            UpdateLocalUI();
    }

    private void OnHostStartChanged(bool oldValue, bool newValue)
    {
        if (isClient)
            UpdateLocalUI();
    }

    [ClientRpc]
    private void RpcRefreshUI()
    {
        UpdateLocalUI();
    }

    [Client]
    public void UpdateLocalUI()
    {
        if (LobbyUIManager.Instance == null)
            return;

        // The active server is the local host.
        bool isHost = NetworkServer.active;

        LSPlayer localPlayer =
            NetworkClient.localPlayer != null
                ? NetworkClient.localPlayer.GetComponent<LSPlayer>()
                : null;

        List<LSPlayer> allPlayers = new List<LSPlayer>();

        foreach (var player in players)
        {
            if (player != null)
                allPlayers.Add(player);
        }

        LobbyUIManager.Instance.RefreshLobbyUI(
            isHost,
            localPlayer,
            allPlayers,
            (int)currentMode,
            canHostStart
        );
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    public override void OnStopServer()
    {
        players.Clear();
        savedTeamByConnection.Clear();
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
