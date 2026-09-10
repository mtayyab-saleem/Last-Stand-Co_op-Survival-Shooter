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

public class LSMatchManager : NetworkBehaviour
{
    public static LSMatchManager Instance;

    public enum GameMode { Solo, Duo, Squad }

    /// <summary>Team IDs cycle inside this range: 1 -> 2 -> 3 -> 4 -> 1.</summary>
    public const int MaxTeams = 4;

    [Header("Match Settings")]
    [SyncVar(hook = nameof(OnModeChanged))]
    public GameMode currentMode = GameMode.Solo;

    [SyncVar(hook = nameof(OnHostStartChanged))]
    public bool canHostStart = false;

    [Tooltip("Set once the host starts the match. New connections are rejected while this is true.")]
    [SyncVar(hook = nameof(OnMatchStartedChanged))]
    public bool matchStarted = false;

    [Header("Game Mode Rules")]
    [Tooltip("Optional rules asset. If not assigned, safe default values will be used.")]
    [SerializeField] private GameModeRulesSO gameModeRules;

    [Header("Debug")]
    [Tooltip("Enable simple console logs for mode and team testing.")]
    [SerializeField] private bool enableTeamDebug = true;

    [Header("Lobby Settings")]
    [Tooltip("Maximum players allowed in the lobby (Host + Clients).")]
    public int maxPlayers = 8;

    /// <summary>Live player objects. The server owns this list.</summary>
    public readonly SyncList<LSPlayer> players = new SyncList<LSPlayer>();

    /// <summary>
    /// Synced view of the roster. Clients read this list and its Callback to redraw
    /// lobby and overhead UI automatically whenever team data changes.
    /// </summary>
    public readonly SyncList<LobbyPlayer> lobbyPlayers = new SyncList<LobbyPlayer>();

    private NetworkDiscovery networkDiscovery;

    // Team layout stored by connection ID so it survives the lobby -> gameplay scene change,
    // where Mirror destroys and respawns every player object.
    private readonly Dictionary<int, int> savedTeamByConnection = new Dictionary<int, int>();
    private readonly Dictionary<int, int> savedMemberIndexByConnection = new Dictionary<int, int>();

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
        lobbyPlayers.Clear();
        savedTeamByConnection.Clear();
        savedMemberIndexByConnection.Clear();

        matchStarted = false;
        canHostStart = false;

        // Load the mode selected by the host from the Host Menu.
        int savedMode = PlayerPrefs.GetInt("HostSelectedMode", 0);
        currentMode = (GameMode)savedMode;

        if (enableTeamDebug)
        {
            Debug.Log($"[TEAM DEBUG] Selected Mode = {currentMode} | Saved Mode Value = {savedMode}");
        }
    }

    public override void OnStartClient()
    {
        // Requirement: every client UI redraws itself from the SyncList callback.
        lobbyPlayers.Callback -= OnLobbyPlayersChanged;
        lobbyPlayers.Callback += OnLobbyPlayersChanged;

        UpdateLocalUI();
        LSPlayer.NotifyTeamDataChanged();
    }

    // -------------------------
    // Roster
    // -------------------------

    [Server]
    public void RegisterPlayer(LSPlayer player)
    {
        if (player == null)
            return;

        if (!players.Contains(player))
        {
            players.Add(player);
        }

        // The host is the connection that owns the server, not "whoever registered first".
        // Registration order is not stable across the lobby -> gameplay scene change.
        player.isGameHost = player.connectionToClient == NetworkServer.localConnection;
        player.isReady = player.isGameHost;

        if (matchStarted)
        {
            // Gameplay scene reload: put the player back on the team it already had.
            RestoreSavedTeam(player);
        }
        else
        {
            RebuildTeams();
        }

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Player Registered = {player.playerName} | " +
                $"Host = {player.isGameHost} | Team = {player.teamID} | " +
                $"Slot = {player.teamMemberIndex} | Total Players = {players.Count}"
            );
        }

        UpdateReadyState();
        CheckDiscoveryState();
        PublishRoster();
    }

    [Server]
    public void UnregisterPlayer(LSPlayer player)
    {
        if (players.Contains(player))
        {
            players.Remove(player);
        }

        // Teams are only reshuffled in the lobby. A running match keeps its layout.
        if (!matchStarted)
        {
            RebuildTeams();
        }

        UpdateReadyState();
        CheckDiscoveryState();
        PublishRoster();
    }

[Server]
    public void UpdateReadyState()
    {
        int totalPlayers = PlayerCount();
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

        // Match can start only when the mode has enough players and every client is ready.
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

    /// <summary>Republishes the synced roster, e.g. after a player name arrives.</summary>
    [Server]
    public void RefreshRoster()
    {
        PublishRoster();
    }

    [Server]
    private void PublishRoster()
    {
        lobbyPlayers.Clear();

        foreach (LSPlayer player in ActiveRoster())
        {
            lobbyPlayers.Add(new LobbyPlayer(
                player.netId,
                player.playerName,
                player.teamID,
                player.teamMemberIndex));
        }
    }

private List<LSPlayer> ActiveRoster()
    {
        List<LSPlayer> roster = new List<LSPlayer>();

        foreach (LSPlayer player in players)
        {
            if (player != null)
                roster.Add(player);
        }

        return roster;
    }

    /// <summary>
    /// Live player count. players.Count also counts entries whose object was already
    /// destroyed, so every capacity and ready check must go through here instead.
    /// </summary>
    private int PlayerCount()
    {
        int count = 0;

        foreach (LSPlayer player in players)
        {
            if (player != null)
                count++;
        }

        return count;
    }

    // -------------------------
    // Teams
    // -------------------------

    /// <summary>Changes the mode on the server and rebalances the lobby immediately.</summary>
    [Server]
    public void SetGameMode(GameMode mode)
    {
        if (currentMode == mode)
            return;

        currentMode = mode;

        RebuildTeams();
        UpdateReadyState();
        PublishRoster();
    }

    /// <summary>
    /// Spreads every connected player evenly across the minimum number of teams the
    /// mode needs, instead of filling the first team before starting the next one.
    /// Squad + 6 -> 3 + 3, Squad + 7 -> 4 + 3, Duo + 7 -> 2 + 2 + 2 + 1.
    /// </summary>
    [Server]
    private void RebuildTeams()
    {
        List<LSPlayer> roster = ActiveRoster();

        savedTeamByConnection.Clear();
        savedMemberIndexByConnection.Clear();

        if (roster.Count == 0)
            return;

        int maxTeamSize = GetMaximumPlayersPerTeam();

        // Fewest teams that can hold everyone without breaking the mode's team cap.
        int teamCount = Mathf.Max(1, Mathf.CeilToInt(roster.Count / (float)maxTeamSize));

        // Even split: the first (count % teamCount) teams take one extra player.
        int baseSize = roster.Count / teamCount;
        int remainder = roster.Count % teamCount;

        int rosterIndex = 0;

        for (int team = 0; team < teamCount; team++)
        {
            int teamSize = baseSize + (team < remainder ? 1 : 0);

            for (int slot = 0; slot < teamSize; slot++)
            {
                LSPlayer player = roster[rosterIndex++];
                player.teamID = team + 1;
                player.teamMemberIndex = slot + 1;
            }
        }

        CacheTeamLayout();

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Teams Rebuilt | Mode = {currentMode} | Players = {roster.Count} | " +
                $"Max Team Size = {maxTeamSize} | Teams = {teamCount} | " +
                $"Split = {DescribeTeamSizes()}"
            );
        }
    }

    /// <summary>
    /// Moves a player to the next team in the 1 -> 2 -> 3 -> 4 -> 1 cycle.
    /// A team that is already at the mode's capacity is skipped automatically.
    /// </summary>
    [Command(requiresAuthority = false)]
    public void CmdCyclePlayerTeam(uint targetNetId, NetworkConnectionToClient sender = null)
    {
        if (matchStarted)
            return;

        LSPlayer target = FindPlayerByNetId(targetNetId);

        if (target == null)
        {
            if (enableTeamDebug)
                Debug.LogWarning($"[TEAM DEBUG] CycleTeam: no player with netId {targetNetId}.");

            return;
        }

        // A client may only move itself. The host may move anybody.
        bool senderIsHost = sender != null && sender == NetworkServer.localConnection;
        bool senderOwnsTarget = sender != null && target.connectionToClient == sender;

        if (!senderIsHost && !senderOwnsTarget)
        {
            if (enableTeamDebug)
                Debug.LogWarning("[TEAM DEBUG] CycleTeam rejected: sender may only move its own player.");

            return;
        }

        int maxTeamSize = GetMaximumPlayersPerTeam();
        int teamRange = Mathf.Max(MaxTeams, HighestTeamId());

        for (int step = 1; step <= teamRange; step++)
        {
            // Wraps 4 -> 1. The extra modulo keeps an unassigned (0) team ID safe.
            int candidate = ((((target.teamID - 1 + step) % teamRange) + teamRange) % teamRange) + 1;

            if (candidate == target.teamID)
                continue;

            // Team is full: skip it and try the next one.
            if (CountTeamMembers(candidate, target) >= maxTeamSize)
                continue;

            int previousTeam = target.teamID;
            target.teamID = candidate;

            ReindexTeamMembers();
            CacheTeamLayout();
            PublishRoster();
            RpcRefreshUI();

            if (enableTeamDebug)
            {
                Debug.Log(
                    $"[TEAM DEBUG] CycleTeam | {target.playerName} moved T{previousTeam} -> " +
                    $"T{target.teamID} #{target.teamMemberIndex} | Split = {DescribeTeamSizes()}"
                );
            }

            return;
        }

        if (enableTeamDebug)
        {
            Debug.Log($"[TEAM DEBUG] CycleTeam | {target.playerName} stayed on T{target.teamID}: every other team is full.");
        }
    }

    /// <summary>Renumbers teamMemberIndex to 1, 2, 3, 4 inside every team.</summary>
    [Server]
    private void ReindexTeamMembers()
    {
        Dictionary<int, int> usedSlots = new Dictionary<int, int>();

        foreach (LSPlayer player in ActiveRoster())
        {
            if (player.teamID <= 0)
            {
                player.teamMemberIndex = 0;
                continue;
            }

            usedSlots.TryGetValue(player.teamID, out int slot);
            slot++;

            usedSlots[player.teamID] = slot;
            player.teamMemberIndex = slot;
        }
    }

    [Server]
    private void CacheTeamLayout()
    {
        savedTeamByConnection.Clear();
        savedMemberIndexByConnection.Clear();

        foreach (LSPlayer player in ActiveRoster())
        {
            if (player.connectionToClient == null)
                continue;

            int connectionId = player.connectionToClient.connectionId;
            savedTeamByConnection[connectionId] = player.teamID;
            savedMemberIndexByConnection[connectionId] = player.teamMemberIndex;
        }
    }

    [Server]
    private void RestoreSavedTeam(LSPlayer player)
    {
        if (player == null || player.connectionToClient == null)
            return;

        int connectionId = player.connectionToClient.connectionId;

        if (savedTeamByConnection.TryGetValue(connectionId, out int savedTeamID))
            player.teamID = savedTeamID;

        if (savedMemberIndexByConnection.TryGetValue(connectionId, out int savedSlot))
            player.teamMemberIndex = savedSlot;

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Team Restored | Player = {player.playerName} | " +
                $"ConnectionId = {connectionId} | Team = {player.teamID} #{player.teamMemberIndex}"
            );
        }
    }

    private LSPlayer FindPlayerByNetId(uint targetNetId)
    {
        foreach (LSPlayer player in players)
        {
            if (player != null && player.netId == targetNetId)
                return player;
        }

        return null;
    }

    private int CountTeamMembers(int teamId, LSPlayer ignore)
    {
        int count = 0;

        foreach (LSPlayer player in players)
        {
            if (player == null || player == ignore)
                continue;

            if (player.teamID == teamId)
                count++;
        }

        return count;
    }

    private int HighestTeamId()
    {
        int highest = 0;

        foreach (LSPlayer player in players)
        {
            if (player != null && player.teamID > highest)
                highest = player.teamID;
        }

        return highest;
    }

    private string DescribeTeamSizes()
    {
        Dictionary<int, int> sizes = new Dictionary<int, int>();

        foreach (LSPlayer player in ActiveRoster())
        {
            if (player.teamID <= 0)
                continue;

            sizes.TryGetValue(player.teamID, out int count);
            sizes[player.teamID] = count + 1;
        }

        List<string> parts = new List<string>();

        for (int team = 1; team <= HighestTeamId(); team++)
        {
            sizes.TryGetValue(team, out int count);
            parts.Add($"T{team}={count}");
        }

        return string.Join(" + ", parts);
    }

    // -------------------------
    // Match flow
    // -------------------------

[Server]
    private void CheckDiscoveryState()
    {
        // Never advertise the lobby after the match has started.
        if (matchStarted)
            return;

        if (networkDiscovery == null)
            networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();

        if (networkDiscovery == null)
            return;

        if (PlayerCount() >= maxPlayers)
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

        // Final team layout, then the lobby is closed for good.
        RebuildTeams();
        matchStarted = true;

        if (networkDiscovery == null)
            networkDiscovery = FindFirstObjectByType<NetworkDiscovery>();

        if (networkDiscovery != null)
            networkDiscovery.StopDiscovery();

        // Anything that connected but never made it into the roster is dropped here,
        // which closes the "connected exactly as the match started" race.
        DropConnectionsOutsideMatch();

        PublishRoster();

        if (enableTeamDebug)
        {
            Debug.Log(
                $"[TEAM DEBUG] Starting Match | Mode = {currentMode} | " +
                $"Players = {players.Count} | Split = {DescribeTeamSizes()}"
            );
        }

        NetworkManager.singleton.ServerChangeScene("GameScene");
    }

    [Server]
    private void DropConnectionsOutsideMatch()
    {
        List<NetworkConnectionToClient> stale = new List<NetworkConnectionToClient>();

        foreach (NetworkConnectionToClient connection in NetworkServer.connections.Values)
        {
            if (connection == null || connection == NetworkServer.localConnection)
                continue;

            if (savedTeamByConnection.ContainsKey(connection.connectionId))
                continue;

            stale.Add(connection);
        }

        foreach (NetworkConnectionToClient connection in stale)
        {
            Debug.LogWarning(
                $"[LSMatchManager] Connection {connection.connectionId} arrived as the match started. Disconnecting."
            );

            connection.Disconnect();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadMode)
    {
        if (!NetworkServer.active || !matchStarted)
            return;

        // Wait briefly so Mirror can finish moving/spawning player objects.
        StartCoroutine(RestoreTeamsAfterSceneLoad());
    }

    private IEnumerator RestoreTeamsAfterSceneLoad()
    {
        // Two frames are enough for the normal Mirror scene transition flow.
        yield return null;
        yield return null;

        if (!NetworkServer.active || !matchStarted)
            yield break;

        foreach (LSPlayer player in players)
        {
            if (player != null)
            {
                RestoreSavedTeam(player);
            }
        }

        PublishRoster();

        if (enableTeamDebug)
        {
            Debug.Log("[TEAM DEBUG] Team restore check completed after scene load.");
        }
    }

    // -------------------------
    // Mode rules
    // -------------------------

private int GetMinimumPlayersToStart()
    {
        int maxTeamSize = GetMaximumPlayersPerTeam();

        // Two teams minimum: one full team plus one more player.
        // Without this a Duo match of 2 or a Squad match of 4 would put everybody
        // on one team, and friendly fire would leave nobody left to fight.
        // Solo = 2, Duo = 3, Squad = 5.
        int fairMinimum = maxTeamSize + 1;

        if (gameModeRules != null)
        {
            GameModeRulesSO.ModeRule rule = gameModeRules.GetRule(currentMode);

            if (rule != null)
                return Mathf.Max(fairMinimum, rule.minimumPlayersToStart);
        }

        return fairMinimum;
    }

    /// <summary>Solo = 1, Duo = 2, Squad = 4 unless the rules asset says otherwise.</summary>
    private int GetMaximumPlayersPerTeam()
    {
        if (gameModeRules != null)
        {
            GameModeRulesSO.ModeRule rule = gameModeRules.GetRule(currentMode);

            if (rule != null)
                return Mathf.Max(1, rule.maximumPlayersPerTeam);
        }

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

    private void OnLobbyPlayersChanged(
        SyncList<LobbyPlayer>.Operation op,
        int index,
        LobbyPlayer oldItem,
        LobbyPlayer newItem)
    {
        if (!isClient)
            return;

        UpdateLocalUI();

        // Wakes every PlayerOverheadUI so name plates redraw themselves.
        LSPlayer.NotifyTeamDataChanged();
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

    private void OnMatchStartedChanged(bool oldValue, bool newValue)
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
        lobbyPlayers.Clear();
        savedTeamByConnection.Clear();
        savedMemberIndexByConnection.Clear();

        matchStarted = false;
        canHostStart = false;

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }

        base.OnStopServer();
    }

    public override void OnStopClient()
    {
        // SyncVars are server-owned, so nothing is written back here.
        lobbyPlayers.Callback -= OnLobbyPlayersChanged;

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }

        base.OnStopClient();
    }
}
