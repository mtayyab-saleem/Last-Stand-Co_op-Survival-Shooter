using UnityEngine;
using Mirror;
using Mirror.Discovery;
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

    // Memory System
    public readonly Dictionary<int, int> connectionToTeam = new Dictionary<int, int>();
    private int hostConnectionId = -1;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
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

        // Host ko lock karna
        if (!hasMatchStarted && players.Count == 1 && hostConnectionId == -1)
        {
            if (player.connectionToClient != null)
            {
                hostConnectionId = player.connectionToClient.connectionId;
            }
        }

        if (player.connectionToClient != null && player.connectionToClient.connectionId == hostConnectionId)
        {
            player.isGameHost = true;
            player.isReady = true;
        }
        else
        {
            player.isGameHost = false;
            if (!hasMatchStarted) player.isReady = false;
        }

        // Team memory restore
        if (player.connectionToClient != null && connectionToTeam.ContainsKey(player.connectionToClient.connectionId))
        {
            player.teamID = connectionToTeam[player.connectionToClient.connectionId];
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
            if (!p.isGameHost)
            {
                totalClients++;
                if (p.isReady) readyClients++;
            }
            else
            {
                p.isReady = true;
            }
        }

        int totalPlayers = players.Count;
        bool allClientsReady = (totalClients == 0) || (readyClients == totalClients);

        // --- STRICT START CONDITIONS ---
        if (currentMode == GameMode.Solo)
        {
            canHostStart = allClientsReady;
        }
        else if (currentMode == GameMode.Duo)
        {
            canHostStart = (totalPlayers >= 2) && allClientsReady;
        }
        else if (currentMode == GameMode.Squad)
        {
            canHostStart = (totalPlayers >= 3) && allClientsReady;
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
        if (hasMatchStarted) return;

        var discovery = FindFirstObjectByType<NetworkDiscovery>();
        if (discovery == null) return;

        if (players.Count >= maxPlayers)
        {
            discovery.StopDiscovery();
            Debug.Log("[LSMatchManager] Lobby is Full. Discovery Stopped.");
        }
        else
        {
            discovery.StopDiscovery();
            discovery.AdvertiseServer();
            Debug.Log("[LSMatchManager] Space available. Discovery Advertising...");
        }
    }

    [Server]
    public void StartMatch()
    {
        if (!canHostStart) return;

        hasMatchStarted = true;

        var discovery = FindFirstObjectByType<NetworkDiscovery>();
        if (discovery != null) discovery.StopDiscovery();

        AssignTeams();

        NetworkManager.singleton.ServerChangeScene("GameScene");
    }

    [Server]
    private void AssignTeams()
    {
        var shuffledPlayers = players.OrderBy(x => Random.value).ToList();
        int totalPlayers = shuffledPlayers.Count;

        connectionToTeam.Clear();

        // ==========================================
        // HUMARA APNA PERFECT TEAM MAKING RULE
        // ==========================================
        if (currentMode == GameMode.Solo)
        {
            // Solo: Har player dushman hai (0, 1, 2, 3...)
            for (int i = 0; i < totalPlayers; i++)
            {
                shuffledPlayers[i].teamID = i;
                connectionToTeam[shuffledPlayers[i].connectionToClient.connectionId] = i;
            }
        }
        else if (currentMode == GameMode.Duo)
        {
            // Duo: Har 2 players ko mila kar ek team banani hai (0,0 - 1,1 - 2,2)
            for (int i = 0; i < totalPlayers; i++)
            {
                int assignedTeam = i / 2;
                shuffledPlayers[i].teamID = assignedTeam;
                connectionToTeam[shuffledPlayers[i].connectionToClient.connectionId] = assignedTeam;
            }
        }
        else if (currentMode == GameMode.Squad)
        {
            // Squad: Fair Card Dealing logic (3,3,3 bajaye 4,4,1 ke)
            int numberOfTeams = Mathf.CeilToInt((float)totalPlayers / 4f);
            for (int i = 0; i < totalPlayers; i++)
            {
                int assignedTeam = i % numberOfTeams;
                shuffledPlayers[i].teamID = assignedTeam;
                connectionToTeam[shuffledPlayers[i].connectionToClient.connectionId] = assignedTeam;
            }
        }
    }

    [Server]
    public void CheckWinCondition()
    {
        if (!hasMatchStarted) return;

        HashSet<int> aliveTeams = new HashSet<int>();

        foreach (var p in players)
        {
            if (p != null && p.isAlive)
            {
                aliveTeams.Add(p.teamID);
            }
        }

        if (aliveTeams.Count == 1)
        {
            int winningTeamID = aliveTeams.First();
            Debug.Log($"[Victory] Match Over! Team {winningTeamID} is the last team standing!");
            RpcAnnounceWinner(winningTeamID);
            EndMatchSequence();
        }
        else if (aliveTeams.Count == 0)
        {
            Debug.Log("[Victory] Match Over! Draw. No one survived.");
            RpcAnnounceWinner(-1);
            EndMatchSequence();
        }
    }

    [ClientRpc]
    private void RpcAnnounceWinner(int winningTeamID)
    {
        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();
        if (localPlayer == null) return;

        if (winningTeamID == -1) Debug.Log("MATCH DRAW! Everyone died.");
        else if (localPlayer.teamID == winningTeamID) Debug.Log("VICTORY! You are the last team standing!");
        else Debug.Log("DEFEAT! Another team won the match.");
    }

    [Server]
    private void EndMatchSequence()
    {
        hasMatchStarted = false;
        Debug.Log("[LSMatchManager] Match has officially ended.");
    }

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

        bool isHost = isServer;
        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();

        List<LSPlayer> allPlayers = new List<LSPlayer>();
        foreach (var p in players) allPlayers.Add(p);

        LobbyUIManager.Instance.RefreshLobbyUI(isHost, localPlayer, allPlayers, (int)currentMode, canHostStart);
    }
}