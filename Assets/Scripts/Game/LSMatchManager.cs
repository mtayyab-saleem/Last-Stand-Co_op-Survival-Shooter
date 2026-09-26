using UnityEngine;
using Mirror;
using Mirror.Discovery;
using JUTPS;
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
    [Tooltip("Console logs for mode and team testing. Off by default: Debug.Log is expensive in a build.")]
    [SerializeField] private bool enableTeamDebug = false;

    [Header("Lobby Settings")]
    [Tooltip("Maximum players allowed in the lobby (Host + Clients).")]
    public int maxPlayers = 8;

    /// <summary>PlayerPrefs key the Host popup's AI checkbox writes.</summary>
    public const string FillWithAIKey = "HostFillWithAI";

    private const string GameSceneName = "GameScene";

    [Header("AI Players")]
    [Tooltip("Set from the Host popup. Empty slots are filled with AI players up to maxPlayers.")]
    [SyncVar(hook = nameof(OnFillWithAIChanged))]
    public bool fillWithAI = false;

    [SerializeField] private LSBotSettings botSettings = new LSBotSettings();

    // Bots decided at match start, spawned once GameScene has loaded.
    private readonly List<LSBotPlanner.BotSlot> plannedBots = new List<LSBotPlanner.BotSlot>();

    [Header("Spawning")]
    [Tooltip("Every team starts at least this far from every other team. Never less than the bots' detect range + 10, so no fight starts the moment the match loads.")]
    [SerializeField] private float spawnSeparation = 60f;

    [Tooltip("Teammates start within this distance of their team's spot.")]
    [SerializeField] private float teammateSpread = 4f;

    [Tooltip("Spawns are picked inside this part of the safe zone's starting radius.")]
    [Range(0.1f, 1f)]
    [SerializeField] private float spawnZoneDepth = 0.8f;

    [Header("Loot")]
    [Tooltip("Power-ups (ammo and health) scattered over the map when the match starts.")]
    [Min(0)] [SerializeField] private int startingLoot = 80;

    [Tooltip("Seconds between loot waves.")]
    [Min(10f)] [SerializeField] private float lootWaveInterval = 120f;

    [Tooltip("Power-ups added inside the current safe zone each wave.")]
    [Min(0)] [SerializeField] private int lootPerWave = 20;

    [Tooltip("Waves stop adding once this many power-ups lie on the map, so a long match cannot pile them up on phones.")]
    [Min(1)] [SerializeField] private int maxLootOnMap = 160;

    private readonly List<GameObject> spawnedLoot = new List<GameObject>();

    // One random spot per team for the current match; teammates start around it.
    private readonly Dictionary<int, Vector3> teamSpawns = new Dictionary<int, Vector3>();
    private int looseSpawnKey;

    /// <summary>Live player objects. The server owns this list.</summary>
    public readonly SyncList<LSPlayer> players = new SyncList<LSPlayer>();

    /// <summary>
    /// Synced view of the roster. Clients read this list and its Callback to redraw
    /// lobby and overhead UI automatically whenever team data changes.
    /// </summary>
    public readonly SyncList<LobbyPlayer> lobbyPlayers = new SyncList<LobbyPlayer>();

    private CustomNetworkDiscovery networkDiscovery;

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
            // LobbyScene brings a new copy every time it loads, but the first one lives
            // on (DontDestroyOnLoad). Mirror switches scene objects on and then spawns
            // them in the same call, so this copy could still be spawned before Destroy
            // took effect - and destroying a spawned object runs OnStopClient and
            // OnStopServer, which hid the new lobby's UI on the second visit. Without a
            // scene id Mirror never spawns it at all.
            if (TryGetComponent(out NetworkIdentity identity))
                identity.sceneId = 0;

            Destroy(gameObject);
            return;
        }

        DontDestroyOnLoad(gameObject);

        // Listen for scene changes so team IDs can be restored safely.
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>A copy that is destroying itself in Awake; it must touch nothing shared.</summary>
    private bool IsDuplicate => Instance != this;

    public override void OnStartServer()
    {
        if (IsDuplicate)
            return;

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

        fillWithAI = PlayerPrefs.GetInt(FillWithAIKey, 0) == 1;
        plannedBots.Clear();

        if (enableTeamDebug)
        {
            Debug.Log($"[TEAM DEBUG] Selected Mode = {currentMode} | Saved Mode Value = {savedMode}");
        }
    }

    public override void OnStartClient()
    {
        if (IsDuplicate)
            return;

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

    /// <summary>
    /// Full lobbies are hidden from the server list by CustomNetworkDiscovery, which asks
    /// this on every discovery request instead of the lobby restarting its advertiser.
    /// </summary>
    public bool IsLobbyFull => PlayerCount() >= maxPlayers;

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

        // Snapshot the original match roster before the scene change.
        // MatchTracker keys humans by Mirror connection ID, so the snapshot survives
        // the Lobby -> GameScene player-object respawn.
        // AI players for every empty slot, planned now so the tracker counts them
        // from the very start. They are spawned once GameScene has loaded.
        plannedBots.Clear();
        teamSpawns.Clear();

        if (fillWithAI)
            plannedBots.AddRange(PlanBots());

        if (MatchTracker.Instance != null)
        {
            MatchTracker.Instance.ServerInitializeMatch(ActiveRoster(), currentMode);

            foreach (LSBotPlanner.BotSlot bot in plannedBots)
                MatchTracker.Instance.ServerAddBot(bot.key, bot.name, bot.teamId, bot.memberIndex);
        }
        else
        {
            Debug.LogWarning("[MATCH] MatchTracker is not attached to the persistent match object.");
        }

        matchStarted = true;

        // Close the advertiser for the rest of the match. CustomNetworkDiscovery also
        // refuses to answer once matchStarted is set, in case anything restarts it.
        if (networkDiscovery == null && NetworkManager.singleton != null)
            networkDiscovery = NetworkManager.singleton.GetComponent<CustomNetworkDiscovery>();

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

        if (scene.name == GameSceneName && plannedBots.Count > 0)
            StartCoroutine(SpawnPlannedBots());

        if (scene.name == GameSceneName)
            StartCoroutine(ServerLootLoop());
    }

    // -------------------------
    // AI players
    // -------------------------

    /// <summary>How many AI players the match would get right now. Drives the lobby hint.</summary>
    public int BotsToFill => fillWithAI ? Mathf.Max(0, maxPlayers - PlayerCount()) : 0;

    [Server]
    private List<LSBotPlanner.BotSlot> PlanBots()
    {
        var humans = new List<LSBotPlanner.HumanSlot>();

        foreach (LSPlayer player in ActiveRoster())
            humans.Add(new LSBotPlanner.HumanSlot(player.teamID, player.teamMemberIndex));

        return LSBotPlanner.Plan(humans, maxPlayers, GetMaximumPlayersPerTeam(),
                                 botSettings.names, new System.Random());
    }

    [Server]
    private IEnumerator SpawnPlannedBots()
    {
        // Let the scene finish loading and Mirror spawn its scene objects first.
        yield return new WaitForSeconds(1f);

        var toSpawn = new List<LSBotPlanner.BotSlot>(plannedBots);
        plannedBots.Clear();   // exactly once per match

        foreach (LSBotPlanner.BotSlot bot in toSpawn)
        {
            if (!NetworkServer.active || !matchStarted)
                yield break;

            if (MatchTracker.Instance != null && MatchTracker.Instance.MatchEnded)
                yield break;

            // A bot that could not be spawned must not stay "alive" in the tracker,
            // or the match could never be decided.
            if (!SpawnBot(bot) && MatchTracker.Instance != null)
                MatchTracker.Instance.ServerNotifyConnectionLost(bot.key);

            yield return null;   // one per frame, to spread the cost
        }
    }

    /// <summary>
    /// A bot is the ordinary player prefab spawned without a connection. Mirror syncs it
    /// to everyone like any player; the server drives it through LSBotBrain.
    /// </summary>
    [Server]
    private bool SpawnBot(LSBotPlanner.BotSlot bot)
    {
        NetworkManager manager = NetworkManager.singleton;

        if (manager == null || manager.playerPrefab == null)
            return false;

        ServerSpawnPose(bot.teamId, out Vector3 position, out Quaternion rotation);

        GameObject botObject = Instantiate(manager.playerPrefab, position, rotation);
        botObject.name = bot.name;

        if (!botObject.TryGetComponent(out LSPlayer player))
        {
            Destroy(botObject);
            return false;
        }

        // Before Spawn, so every client receives it in the spawn message.
        player.isBot = true;
        player.botKey = bot.key;
        player.playerName = bot.name;
        player.teamID = bot.teamId;
        player.teamMemberIndex = bot.memberIndex;
        player.isReady = true;

        if (botObject.TryGetComponent(out JUCharacterController character))
        {
            // Before the brain is added: LSBotBrain relies on this flag to make JUTPS
            // drop the camera the character picked up in its own Awake.
            character.IsArtificialIntelligence = true;
            character.UseDefaultControllerInput = false;
        }

        // Server only, and not a NetworkBehaviour, so the network layout is untouched.
        botObject.AddComponent<LSBotBrain>().Initialise(botSettings);

        NetworkServer.Spawn(botObject);
        return true;
    }

    // -------------------------
    // Loot
    // -------------------------

    /// <summary>
    /// Scatters power-ups over the whole zone at the start, then tops up inside the
    /// current safe zone every wave. The power-ups are the ones registered with the
    /// NetworkManager; picking one up is handled by PlayerNetworkSetup.CmdCollectPowerup.
    /// </summary>
    [Server]
    private IEnumerator ServerLootLoop()
    {
        spawnedLoot.Clear();

        // After the scene objects (and the safe zone) exist.
        yield return new WaitForSeconds(1.5f);

        List<GameObject> prefabs = LootPrefabs();

        if (prefabs.Count == 0)
        {
            Debug.LogWarning("[LSMatchManager] No power-up prefabs registered with the NetworkManager; no loot spawned.");
            yield break;
        }

        yield return SpawnLoot(prefabs, startingLoot);

        var wait = new WaitForSeconds(lootWaveInterval);

        while (true)
        {
            yield return wait;

            if (!LootCanSpawn())
                yield break;

            spawnedLoot.RemoveAll(item => item == null);
            yield return SpawnLoot(prefabs, Mathf.Min(lootPerWave, maxLootOnMap - spawnedLoot.Count));
        }
    }

    private bool LootCanSpawn()
    {
        return NetworkServer.active && matchStarted &&
               SceneManager.GetActiveScene().name == GameSceneName &&
               (MatchTracker.Instance == null || !MatchTracker.Instance.MatchEnded);
    }

    private static List<GameObject> LootPrefabs()
    {
        var prefabs = new List<GameObject>();

        if (NetworkManager.singleton == null)
            return prefabs;

        foreach (GameObject prefab in NetworkManager.singleton.spawnPrefabs)
        {
            if (prefab != null &&
                (prefab.GetComponent<JUTPS.WeaponSystem.AmmoBox>() != null ||
                 prefab.GetComponent<JUTPS.PowerUps.HealthPowerUp>() != null))
                prefabs.Add(prefab);
        }

        return prefabs;
    }

    /// <summary>Spawns up to <paramref name="count"/> power-ups inside the current safe zone, a few per frame.</summary>
    [Server]
    private IEnumerator SpawnLoot(List<GameObject> prefabs, int count)
    {
        SafeZoneController zone = FindAnyObjectByType<SafeZoneController>();
        Vector3 centre = zone != null ? zone.ZoneCenter : Vector3.zero;
        float radius = (zone != null ? zone.CurrentRadius : 100f) * 0.9f;

        for (int i = 0; i < count; i++)
        {
            if (!LootCanSpawn())
                yield break;

            for (int attempt = 0; attempt < 20; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;

                if (!TryGetSpawnGround(new Vector3(centre.x + offset.x, 0f, centre.z + offset.y), out Vector3 ground))
                    continue;

                GameObject prefab = prefabs[Random.Range(0, prefabs.Count)];
                GameObject item = Instantiate(prefab, ground + Vector3.up * 0.5f, Quaternion.identity);
                NetworkServer.Spawn(item);
                spawnedLoot.Add(item);
                break;
            }

            if (i % 4 == 3)
                yield return null;   // spread the cost over a few frames
        }
    }

    // -------------------------
    // Spawning
    // -------------------------

    /// <summary>
    /// Where a human player starts in GameScene. False outside a running match, where
    /// the normal start positions are used.
    /// </summary>
    [Server]
    public bool ServerTryGetPlayerSpawn(int connectionId, out Vector3 position, out Quaternion rotation)
    {
        position = Vector3.zero;
        rotation = Quaternion.identity;

        if (!matchStarted || SceneManager.GetActiveScene().name != GameSceneName)
            return false;

        savedTeamByConnection.TryGetValue(connectionId, out int teamId);
        ServerSpawnPose(teamId, out position, out rotation);
        return true;
    }

    /// <summary>
    /// A random start for a member of this team. The first member of a team picks the
    /// team's spot, at least spawnSeparation from every other team; the rest start a
    /// few metres around it.
    /// </summary>
    [Server]
    private void ServerSpawnPose(int teamId, out Vector3 position, out Quaternion rotation)
    {
        SafeZoneController zone = FindAnyObjectByType<SafeZoneController>();
        Vector3 centre = zone != null ? zone.ZoneCenter : Vector3.zero;

        // No team (should not happen in a match): a spot of its own.
        int key = teamId > 0 ? teamId : --looseSpawnKey;

        if (!teamSpawns.TryGetValue(key, out Vector3 teamSpot))
        {
            teamSpot = PickTeamSpawn(zone, centre);
            teamSpawns[key] = teamSpot;
            position = teamSpot;
        }
        else
        {
            position = PickNear(teamSpot);
        }

        Vector3 toCentre = centre - position;
        toCentre.y = 0f;
        rotation = toCentre.sqrMagnitude > 1f ? Quaternion.LookRotation(toCentre) : Quaternion.identity;
    }

    private Vector3 PickTeamSpawn(SafeZoneController zone, Vector3 centre)
    {
        float radius = (zone != null ? zone.CurrentRadius : 100f) * spawnZoneDepth;
        float separation = Mathf.Max(spawnSeparation, botSettings.detectRange + 10f);

        // Relax the spacing only if the map genuinely has no room left.
        for (float relax = 1f; relax > 0.3f; relax -= 0.2f)
        {
            float minSqr = separation * relax * separation * relax;

            for (int attempt = 0; attempt < 60; attempt++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;

                if (!TryGetSpawnGround(new Vector3(centre.x + offset.x, 0f, centre.z + offset.y), out Vector3 spot))
                    continue;

                bool crowded = false;
                foreach (Vector3 other in teamSpawns.Values)
                {
                    float dx = other.x - spot.x, dz = other.z - spot.z;
                    if (dx * dx + dz * dz < minSqr) { crowded = true; break; }
                }

                if (!crowded)
                    return spot;
            }
        }

        Debug.LogWarning("[LSMatchManager] No free random spawn found; using a start position.");
        Transform start = NetworkManager.singleton != null ? NetworkManager.singleton.GetStartPosition() : null;
        return start != null ? start.position : centre;
    }

    private Vector3 PickNear(Vector3 teamSpot)
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.5f, teammateSpread);

            if (TryGetSpawnGround(teamSpot + new Vector3(offset.x, 0f, offset.y), out Vector3 spot))
                return spot;
        }

        return teamSpot;
    }

    /// <summary>
    /// Ground under this XZ where a character fits: on the terrain, away from its
    /// edge, not on a steep slope, and clear of trees, props and other players.
    /// </summary>
    private static bool TryGetSpawnGround(Vector3 point, out Vector3 ground)
    {
        ground = point;
        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null)
        {
            // PlayerBoundary stops players 20 m from the edge; stay clear of that.
            const float edge = 30f;
            Vector3 origin = terrain.GetPosition();
            Vector3 size = terrain.terrainData.size;

            if (point.x < origin.x + edge || point.x > origin.x + size.x - edge ||
                point.z < origin.z + edge || point.z > origin.z + size.z - edge)
                return false;

            float nx = (point.x - origin.x) / size.x;
            float nz = (point.z - origin.z) / size.z;

            if (terrain.terrainData.GetSteepness(nx, nz) > 30f)
                return false;

            ground.y = terrain.SampleHeight(point) + origin.y;
        }
        else if (Physics.Raycast(point + Vector3.up * 500f, Vector3.down, out RaycastHit hit, 1000f,
                                 Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            ground = hit.point;
        }
        else
        {
            return false;
        }

        // Body-sized capsule starting knee-high, so the ground itself never counts but
        // a tree trunk, a rock or another player does.
        if (Physics.CheckCapsule(ground + Vector3.up * 0.9f, ground + Vector3.up * 1.7f, 0.45f,
                                 Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return false;

        ground.y += 0.05f;
        return true;
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

    /// <summary>How many players this mode needs before the host can start.</summary>
    public int MinimumPlayersToStart => GetMinimumPlayersToStart();

    private int GetMinimumPlayersToStart()
    {
        // Solo can start with only the host, and with AI fill the bots make up the
        // teams, so any mode can.
        if (currentMode == GameMode.Solo || fillWithAI)
            return 1;

        int maxTeamSize = GetMaximumPlayersPerTeam();

        // Team modes need at least two different teams.
        // Duo: max team size 2 -> minimum 3 players.
        // Squad: max team size 4 -> minimum 5 players.
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

    private void OnFillWithAIChanged(bool oldValue, bool newValue)
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

        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// The session is over (host stopped or client left). This persistent object has
    /// done its job; the next LobbyScene's own copy takes over as a fresh manager, so no
    /// state - or a second copy fighting this one - ever carries into the next match.
    /// </summary>
    private void EndSession()
    {
        Destroy(gameObject);
    }

    public override void OnStopServer()
    {
        if (IsDuplicate)
        {
            base.OnStopServer();
            return;
        }

        players.Clear();
        lobbyPlayers.Clear();
        savedTeamByConnection.Clear();
        savedMemberIndexByConnection.Clear();
        plannedBots.Clear();

        matchStarted = false;
        canHostStart = false;

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }

        base.OnStopServer();
        EndSession();
    }

    public override void OnStopClient()
    {
        if (IsDuplicate)
        {
            base.OnStopClient();
            return;
        }

        // SyncVars are server-owned, so nothing is written back here.
        lobbyPlayers.Callback -= OnLobbyPlayersChanged;

        if (LobbyUIManager.Instance != null)
        {
            LobbyUIManager.Instance.ResetUI();
        }

        base.OnStopClient();
        EndSession();
    }
}
