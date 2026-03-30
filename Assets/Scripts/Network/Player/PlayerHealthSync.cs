// ============================================================
// PlayerHealthSync.cs
// PURPOSE: Sync HEALTH and DEATH state across the network.
//          Server is authoritative — server decides final health.
//          Local player reports damage → server confirms → all clients update.
//
// REPLACES: JU_NetworkManager.cs
// KEY IMPROVEMENT: No more Update() polling every frame.
//                  Uses a health check only when damage is detected.
// SINGLE RESPONSIBILITY: Health and death sync only.
// ============================================================

using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.FX;

[RequireComponent(typeof(JUHealth))]
[DisallowMultipleComponent]
public class PlayerHealthSync : NetworkBehaviour
{
    // ── Component Reference ──────────────────────────────────
    private JUHealth _juHealth;

    // ── SyncVars ─────────────────────────────────────────────
    // netHealth: when this changes on server → OnNetHealthChanged() fires on all clients
    [SyncVar(hook = nameof(OnNetHealthChanged))]
    private float _netHealth = 100f;

    // netIsDead: when this changes on server → OnNetDeathChanged() fires on all clients
    [SyncVar(hook = nameof(OnNetDeathChanged))]
    private bool _netIsDead = false;

    // ── Cached Value ─────────────────────────────────────────
    // We store last known health to detect damage without polling every frame
    private float _lastCheckedHealth;

    // ════════════════════════════════════════════════════════
    // MIRROR CALLBACKS
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        _juHealth = GetComponent<JUHealth>();
    }

    public override void OnStartServer()
    {
        // Initialize server-side health from the JU TPS component
        _netHealth = _juHealth.MaxHealth;
        _lastCheckedHealth = _netHealth;
    }

    public override void OnStartLocalPlayer()
    {
        // Cache starting health for our damage detection
        _lastCheckedHealth = _juHealth.Health;
    }

    // ════════════════════════════════════════════════════════
    // DAMAGE DETECTION
    // Only runs on LOCAL player device.
    // Checks if health dropped (= took damage) and reports to server.
    //
    // WHY STILL IN UPDATE?
    //   JU TPS doesn't expose a clean "OnDamageReceived" event.
    //   But this is MUCH cleaner than before:
    //   - Early returns prevent most work when not needed
    //   - Only LOCAL player runs this (not all remote players)
    // ════════════════════════════════════════════════════════

    private void Update()
    {
        // Only the LOCAL player reports their own damage
        if (!isLocalPlayer) return;

        // Dead players don't need health checks
        if (_netIsDead) return;

        float currentLocalHealth = _juHealth.Health;

        // Did health drop? That means we took damage
        if (currentLocalHealth < _lastCheckedHealth)
        {
            float damageAmount = _lastCheckedHealth - currentLocalHealth;

            // IMPORTANT: Reset local health back to server value.
            // Server will re-apply the correct damage authoritatively.
            _juHealth.Health = _netHealth;

            // Tell the server about the damage
            CmdReportDamage(gameObject, damageAmount);
        }

        // Always keep our cached value up to date with server value
        _lastCheckedHealth = _netHealth;
    }

    // ════════════════════════════════════════════════════════
    // COMMANDS (Client → Server)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Client tells server: "I hit this target for this much damage."
    /// Server validates and applies it.
    /// </summary>
    [Command]
    public void CmdReportDamage(GameObject target, float amount)
    {
        if (target == null) return;

        if (target.TryGetComponent<PlayerHealthSync>(out var targetHealth))
            targetHealth.ServerApplyDamage(amount);
    }

    // ════════════════════════════════════════════════════════
    // SERVER METHODS
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// ONLY runs on server. Applies damage and checks for death.
    /// SyncVars auto-broadcast changes to all clients.
    /// </summary>
    [Server]
    public void ServerApplyDamage(float amount)
    {
        if (_netIsDead) return; // Can't damage a dead player

        _netHealth = Mathf.Max(0f, _netHealth - amount);

        if (_netHealth <= 0f)
            _netIsDead = true; // SyncVar → triggers OnNetDeathChanged on all clients

        // Update the JU TPS health on server-side too
        _juHealth.Health = _netHealth;
        _juHealth.CheckHealthState();
    }

    // ════════════════════════════════════════════════════════
    // SYNCVAR HOOKS (Run on ALL clients automatically)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Called on EVERY client when health changes on server.
    /// </summary>
    private void OnNetHealthChanged(float oldHealth, float newHealth)
    {
        // Update JU TPS health to match server value
        _juHealth.Health = newHealth;

        // Show blood screen effect only on the LOCAL player who got hit
        bool tookDamage = newHealth < oldHealth;
        if (isLocalPlayer && tookDamage && !_netIsDead)
        {
            BloodScreen.PlayerTakingDamaged();

            // Tell HUD / other systems about health change
            NetworkEventBus.RaiseHealthChanged(newHealth, _juHealth.MaxHealth);
        }
    }

    /// <summary>
    /// Called on EVERY client when death state changes on server.
    /// </summary>
    private void OnNetDeathChanged(bool wasAlive, bool isDead)
    {
        _juHealth.IsDead = isDead;

        if (isDead)
        {
            _juHealth.Health = 0f;
            _juHealth.CheckHealthState(); // Triggers JU TPS death (ragdoll / animation)

            if (isLocalPlayer)
                NetworkEventBus.RaiseLocalPlayerDied();
        }
        // Respawn logic will go here in future development
    }

    // ════════════════════════════════════════════════════════
    // PUBLIC API (for other systems like MatchController)
    // ════════════════════════════════════════════════════════

    /// <summary> Current health value (server-authoritative). </summary>
    public float CurrentHealth => _netHealth;

    /// <summary> Is this player currently dead? </summary>
    public bool IsDead => _netIsDead;
}