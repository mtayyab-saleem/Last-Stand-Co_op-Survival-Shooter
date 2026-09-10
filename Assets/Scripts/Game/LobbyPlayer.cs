using System;

/// <summary>
/// Network serialisable snapshot of one lobby member.
/// LSMatchManager keeps these inside a SyncList so every client can redraw
/// team UI without walking the player objects itself.
/// Team IDs are 1-based; 0 means "not assigned yet".
/// </summary>
[Serializable]
public struct LobbyPlayer : IEquatable<LobbyPlayer>
{
    public uint netId;
    public string playerName;
    public int teamId;

    /// <summary>Sequential slot inside the team: 1, 2, 3, 4.</summary>
    public int teamMemberIndex;

    public LobbyPlayer(uint netId, string playerName, int teamId, int teamMemberIndex)
    {
        this.netId = netId;
        this.playerName = playerName;
        this.teamId = teamId;
        this.teamMemberIndex = teamMemberIndex;
    }

    public bool Equals(LobbyPlayer other)
    {
        return netId == other.netId
            && teamId == other.teamId
            && teamMemberIndex == other.teamMemberIndex
            && playerName == other.playerName;
    }

    public override bool Equals(object obj)
    {
        return obj is LobbyPlayer other && Equals(other);
    }

    public override int GetHashCode()
    {
        return (int)netId;
    }

    /// <summary>Overhead / lobby label format, e.g. "Ali - T1 - #2".</summary>
    public override string ToString()
    {
        return $"{playerName} - T{teamId} - #{teamMemberIndex}";
    }
}
