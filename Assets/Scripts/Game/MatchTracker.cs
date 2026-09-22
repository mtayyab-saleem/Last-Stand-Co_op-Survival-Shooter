using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Mirror;
using UnityEngine;

/// <summary>
/// Server-authoritative human match result tracker.
///
/// Add this component to the SAME persistent NetworkIdentity/GameObject that already
/// carries LSMatchManager. LSMatchManager snapshots the lobby roster immediately before
/// changing to GameScene, so player-object respawns cannot lose team/member information.
///
/// AI is intentionally not handled here yet. It can be added later without changing
/// the human elimination flow.
/// </summary>
[DisallowMultipleComponent]
public class MatchTracker : NetworkBehaviour
{
    public static MatchTracker Instance { get; private set; }

    [Header("Synced Match Result")]
    [SyncVar][SerializeField] private bool trackingActive = false;
    [SyncVar][SerializeField] private bool matchEnded = false;
    [SyncVar][SerializeField] private int winningTeamId = 0;
    [SyncVar][SerializeField] private string winnerPlayerName = string.Empty;
    [SyncVar][SerializeField] private string winningTeamMemberNames = string.Empty;
    [SyncVar][SerializeField] private int alivePlayers = 0;
    [SyncVar][SerializeField] private int aliveTeams = 0;

    [Header("Debug")]
    [SerializeField] private bool enableMatchDebug = false;

    public bool TrackingActive => trackingActive;
    public bool MatchEnded => matchEnded;
    public int WinningTeamId => winningTeamId;
    public string WinnerPlayerName => winnerPlayerName;

    /// <summary>Players still in the match, for the eliminated player's screen.</summary>
    public int AlivePlayers => alivePlayers;
    public int AliveTeams => aliveTeams;
    /// <summary>Winning team members, one name per line.</summary>
    public string WinningTeamMemberNames => winningTeamMemberNames;

    private LSMatchManager.GameMode matchMode = LSMatchManager.GameMode.Solo;
    private Coroutine pendingWinnerCheck;

    private sealed class Participant
    {
        public int connectionId;
        public string playerName;
        public int teamId;
        public int memberIndex;
        public bool alive;
    }

    // Mirror connection ID stays stable while Lobby player objects are replaced by
    // GameScene player objects, so it is safer here than netId.
    private readonly Dictionary<int, Participant> participants =
        new Dictionary<int, Participant>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            // Expected every time LobbyScene reloads: LSMatchManager keeps the original
            // persistent object and destroys this new copy in its own Awake.
            enabled = false;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        ResetServerState();
    }

    [Server]
    private void ResetServerState()
    {
        participants.Clear();
        trackingActive = false;
        matchEnded = false;
        winningTeamId = 0;
        winnerPlayerName = string.Empty;
        winningTeamMemberNames = string.Empty;
        alivePlayers = 0;
        aliveTeams = 0;

        if (pendingWinnerCheck != null)
        {
            StopCoroutine(pendingWinnerCheck);
            pendingWinnerCheck = null;
        }
    }

    /// <summary>
    /// Called once by LSMatchManager after its final team rebuild and before GameScene.
    /// </summary>
    [Server]
    public void ServerInitializeMatch(IEnumerable<LSPlayer> roster, LSMatchManager.GameMode mode)
    {
        ResetServerState();
        matchMode = mode;

        if (roster != null)
        {
            foreach (LSPlayer player in roster)
                CaptureParticipant(player, true);
        }

        trackingActive = participants.Count > 0;
        RefreshAliveCounts();

        if (enableMatchDebug)
        {
            Debug.Log(
                $"[MATCH] Tracking started | Mode={matchMode} | " +
                $"Players={participants.Count} | Teams={AliveTeamCount()}"
            );
        }
    }

    /// <summary>Called by PlayerHealthManager exactly when server health reaches zero.</summary>
    [Server]
    public void ServerNotifyPlayerEliminated(LSPlayer player)
    {
        if (!trackingActive || matchEnded || player == null)
            return;

        Participant participant = CaptureParticipant(player, false);
        if (participant == null)
            return;

        // Duplicate death callbacks must not produce duplicate winner checks/logs.
        if (!participant.alive)
            return;

        participant.alive = false;

        if (enableMatchDebug)
        {
            Debug.Log(
                $"[MATCH] Player Eliminated | {participant.playerName} | " +
                $"Team={participant.teamId}"
            );
        }

        QueueWinnerCheck();
    }

    /// <summary>
    /// Called only for a real Mirror connection loss, never for a scene change. Takes the
    /// connection ID rather than the player object, which may not exist any more.
    /// </summary>
    [Server]
    public void ServerNotifyConnectionLost(int connectionId)
    {
        if (!trackingActive || matchEnded)
            return;

        // Connections that were never part of the match (rejected late joiners) are ignored.
        if (!participants.TryGetValue(connectionId, out Participant participant))
            return;

        if (!participant.alive)
            return;

        participant.alive = false;

        if (enableMatchDebug)
        {
            Debug.Log(
                $"[MATCH] Player Disconnected | {participant.playerName} | " +
                $"Team={participant.teamId}"
            );
        }

        QueueWinnerCheck();
    }

    [Server]
    private Participant CaptureParticipant(LSPlayer player, bool initialAlive)
    {
        if (player == null || player.connectionToClient == null)
            return null;

        int connectionId = player.connectionToClient.connectionId;

        if (!participants.TryGetValue(connectionId, out Participant participant))
        {
            participant = new Participant
            {
                connectionId = connectionId,
                alive = initialAlive
            };

            participants.Add(connectionId, participant);
        }

        // The name may still arrive late, so it is refreshed whenever we see the player.
        if (!string.IsNullOrWhiteSpace(player.playerName))
            participant.playerName = player.playerName;
        else if (string.IsNullOrWhiteSpace(participant.playerName))
            participant.playerName = $"Player {connectionId}";

        // Team and slot come from the lobby snapshot only. The GameScene player object
        // gets its team restored after spawning, so reading it again later could record
        // 0 and silently drop a whole team out of the winner check.
        if (initialAlive)
        {
            participant.teamId = player.teamID;
            participant.memberIndex = player.teamMemberIndex;
        }

        return participant;
    }

    [Server]
    private void RefreshAliveCounts()
    {
        alivePlayers = participants.Values.Count(p => p.alive);
        aliveTeams = AliveTeamCount();
    }

    [Server]
    private void QueueWinnerCheck()
    {
        RefreshAliveCounts();

        if (pendingWinnerCheck != null || matchEnded)
            return;

        pendingWinnerCheck = StartCoroutine(CheckWinnerNextFrame());
    }

    private IEnumerator CheckWinnerNextFrame()
    {
        // Several players can die from the same Safe Zone tick/frame. Waiting one frame
        // prevents the first processed death from incorrectly declaring a winner before
        // the other same-frame deaths are recorded.
        yield return null;

        pendingWinnerCheck = null;

        if (NetworkServer.active)
            EvaluateWinner();
    }

    [Server]
    private void EvaluateWinner()
    {
        if (!trackingActive || matchEnded)
            return;

        List<Participant> alive = participants.Values.Where(p => p.alive).ToList();

        if (enableMatchDebug)
        {
            Debug.Log(
                $"[MATCH] Winner Check | AlivePlayers={alive.Count} | " +
                $"AliveTeams={alive.Where(p => p.teamId > 0).Select(p => p.teamId).Distinct().Count()}"
            );
        }

        if (matchMode == LSMatchManager.GameMode.Solo)
        {
            // A one-human Solo test match must not instantly finish on scene load.
            // Checks are event-driven, and at least two starting humans are required
            // before 'last surviving human' can be meaningful. AI will extend this later.
            if (participants.Count >= 2 && alive.Count == 1)
            {
                EndSolo(alive[0]);
            }
            else if (participants.Count >= 1 && alive.Count == 0)
            {
                EndWithoutWinner();
            }

            return;
        }

        List<int> aliveTeams = alive
            .Where(p => p.teamId > 0)
            .Select(p => p.teamId)
            .Distinct()
            .ToList();

        int startingTeams = participants.Values
            .Where(p => p.teamId > 0)
            .Select(p => p.teamId)
            .Distinct()
            .Count();

        if (startingTeams >= 2 && aliveTeams.Count == 1)
        {
            EndTeam(aliveTeams[0]);
        }
        else if (startingTeams >= 1 && aliveTeams.Count == 0)
        {
            EndWithoutWinner();
        }
    }

    [Server]
    private void EndSolo(Participant winner)
    {
        matchEnded = true;
        winningTeamId = 0;
        winnerPlayerName = winner.playerName;
        winningTeamMemberNames = winner.playerName;

        if (enableMatchDebug)
            Debug.Log($"[MATCH RESULT] SOLO WINNER: {winnerPlayerName}");
    }

    [Server]
    private void EndTeam(int teamId)
    {
        matchEnded = true;
        winningTeamId = teamId;
        winnerPlayerName = string.Empty;

        List<Participant> members = participants.Values
            .Where(p => p.teamId == teamId)
            .OrderBy(p => p.memberIndex)
            .ToList();

        // One per line: player names can contain commas, newlines they cannot.
        winningTeamMemberNames = string.Join("\n", members.Select(p => p.playerName));

        if (enableMatchDebug)
            Debug.Log($"[MATCH RESULT] TEAM {teamId} WINS | Members: {winningTeamMemberNames}");
    }

    [Server]
    private void EndWithoutWinner()
    {
        matchEnded = true;
        winningTeamId = 0;
        winnerPlayerName = string.Empty;
        winningTeamMemberNames = string.Empty;

        if (enableMatchDebug)
            Debug.Log("[MATCH RESULT] NO WINNER - all tracked players were eliminated/disconnected.");
    }

    private int AliveTeamCount()
    {
        return participants.Values
            .Where(p => p.alive && p.teamId > 0)
            .Select(p => p.teamId)
            .Distinct()
            .Count();
    }
}
