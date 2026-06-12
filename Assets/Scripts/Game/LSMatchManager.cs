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

    // Memory dictionaries to safely store data when changing scenes
    public readonly Dictionary<int, int> connectionToTeam = new Dictionary<int, int>();

    // Stores the host connection ID to prevent host reassignment issues
    private int hostConnectionId = -1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[LSMatchManager] Instance initialized and set to DontDestroyOnLoad.");
        }
        else
        {
            Destroy(gameObject);
        }
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
        if (!players.Contains(player)) players.Add(player);

        // Lock the Host Connection ID the first time the host enters the lobby
        if (!hasMatchStarted && players.Count == 1 && hostConnectionId == -1)
        {
            if (player.connectionToClient != null)
            {
                hostConnectionId = player.connectionToClient.connectionId;
            }
        }

        // Reassign the Host status strictly based on the saved connection ID
        if (player.connectionToClient != null && player.connectionToClient.connectionId == hostConnectionId)
        {
            player.isGameHost = true;
            player.isReady = true;
            Debug.Log($"[LSMatchManager] Player '{player.playerName}' recognized and set as HOST.");
        }
        else
        {
            player.isGameHost = false;
            if (!hasMatchStarted) player.isReady = false;
            Debug.Log($"[LSMatchManager] Player '{player.playerName}' recognized as CLIENT.");
        }

        // Restore the preserved Team ID to the newly spawned player object after a scene change
        if (player.connectionToClient != null && connectionToTeam.ContainsKey(player.connectionToClient.connectionId))
        {
            player.teamID = connectionToTeam[player.connectionToClient.connectionId];
            Debug.Log($"[LSMatchManager] Restored Team ID {player.teamID} for player {player.playerName}");
        }

        UpdateReadyState();
        CheckDiscoveryState();
        RpcRefreshUI();
    }

    [Server]
    public void UnregisterPlayer(LSPlayer player)
    {
        if (players.Contains(player)) players.Remove(player);
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

        if (currentMode == GameMode.Solo) canHostStart = allClientsReady;
        else if (currentMode == GameMode.Duo) canHostStart = (totalPlayers >= 2) && allClientsReady;
        else if (currentMode == GameMode.Squad) canHostStart = (totalPlayers >= 3) && allClientsReady;

        RpcRefreshUI();
    }

    [Server]
    private void CheckDiscoveryState()
    {
        if (hasMatchStarted) return;

        var discovery = FindFirstObjectByType<NetworkDiscovery>();
        if (discovery == null) return;

        if (players.Count >= maxPlayers) discovery.StopDiscovery();
        else
        {
            discovery.StopDiscovery();
            discovery.AdvertiseServer();
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

        if (currentMode == GameMode.Solo)
        {
            for (int i = 0; i < totalPlayers; i++)
            {
                shuffledPlayers[i].teamID = i;
                connectionToTeam[shuffledPlayers[i].connectionToClient.connectionId] = i;
            }
        }
        else if (currentMode == GameMode.Duo)
        {
            for (int i = 0; i < totalPlayers; i++)
            {
                int assignedTeam = i / 2;
                shuffledPlayers[i].teamID = assignedTeam;
                connectionToTeam[shuffledPlayers[i].connectionToClient.connectionId] = assignedTeam;
            }
        }
        else if (currentMode == GameMode.Squad)
        {
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

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
        }
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

        // ==============================================================
        // CORE FIX: Prevent Raycast interaction and blood effects on teammates
        // ==============================================================
        if (localPlayer != null)
        {
            foreach (var p in players)
            {
                // Skip self
                if (p == null || p.gameObject == localPlayer.gameObject) continue;

                // Evaluate if the evaluated player shares the same team ID
                bool isTeammate = (currentMode != GameMode.Solo) && (p.teamID != -1) && (p.teamID == localPlayer.teamID);

                // Apply the Ignore Raycast layer state
                p.SetAsTeammate(isTeammate);
            }
        }
    }
}