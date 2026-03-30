// ============================================================
// NetworkEventBus.cs
// PURPOSE: Central hub for all game events.
//          Systems talk to each other through events,
//          NOT through direct script references.
//
// HOW IT WORKS (Simple analogy):
//   Think of this like a notice board.
//   - Any system can "post a notice" (raise an event)
//   - Any system can "read the board" (subscribe to an event)
//   - They never need to know WHO posted or WHO is reading
// ============================================================

using System;
using UnityEngine;

public static class NetworkEventBus
{
    // ── Player Lifecycle ─────────────────────────────────────
    // Fires when the local player spawns in the game world
    public static event Action<GameObject> OnLocalPlayerSpawned;

    // Fires when the local player leaves (disconnect / scene change)
    public static event Action OnLocalPlayerDespawned;

    // ── Health / Combat ──────────────────────────────────────
    // Fires when local player's health changes
    // Parameters: (float currentHP, float maxHP)
    public static event Action<float, float> OnHealthChanged;

    // Fires when local player dies
    public static event Action OnLocalPlayerDied;

    // ── Match Flow ───────────────────────────────────────────
    // Fires when the match officially begins
    public static event Action OnMatchStarted;

    // Fires when match ends — passes winner's name as string
    public static event Action<string> OnMatchEnded;

    // ════════════════════════════════════════════════════════
    // RAISE METHODS
    // Call these to fire events. The "?." means:
    // "only fire if someone is actually listening"
    // ════════════════════════════════════════════════════════

    public static void RaiseLocalPlayerSpawned(GameObject player)
    {
        Debug.Log("[NetworkEventBus] Local player spawned.");
        OnLocalPlayerSpawned?.Invoke(player);
    }

    public static void RaiseLocalPlayerDespawned()
    {
        Debug.Log("[NetworkEventBus] Local player despawned.");
        OnLocalPlayerDespawned?.Invoke();
    }

    public static void RaiseHealthChanged(float current, float max)
    {
        // No debug log here — this fires every time damage is taken
        OnHealthChanged?.Invoke(current, max);
    }

    public static void RaiseLocalPlayerDied()
    {
        Debug.Log("[NetworkEventBus] Local player died.");
        OnLocalPlayerDied?.Invoke();
    }

    public static void RaiseMatchStarted()
    {
        Debug.Log("[NetworkEventBus] Match started.");
        OnMatchStarted?.Invoke();
    }

    public static void RaiseMatchEnded(string winnerName)
    {
        Debug.Log($"[NetworkEventBus] Match ended. Winner: {winnerName}");
        OnMatchEnded?.Invoke(winnerName);
    }
}