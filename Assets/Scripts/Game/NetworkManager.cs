using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.FX;

[RequireComponent(typeof(JUHealth))]
public class NetworkManager : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private JUHealth _juHealth;

    // --- STATIC CACHE FOR OPTIMIZATION ---
    // Replaces expensive GetComponent calls inside the Update loop (Rule 6)
    public static NetworkManager LocalInstance { get; private set; }

    // --- CONSTANTS ---
    private const float MAX_ALLOWED_DAMAGE = 500f; // Security threshold to prevent insta-kill hacks

    // --- NETWORKED STATE ---
    [SyncVar(hook = nameof(OnServerHealthChanged))]
    public float netHealth = 100f;

    [SyncVar(hook = nameof(OnDeathStateChanged))]
    public bool netIsDead = false;

    void Awake()
    {
        if (_juHealth == null) _juHealth = GetComponent<JUHealth>();
    }

    public override void OnStartLocalPlayer()
    {
        // Cache the local player instance when they spawn
        LocalInstance = this;
    }

    public override void OnStartServer()
    {
        netHealth = _juHealth.Health;
        netIsDead = _juHealth.IsDead;
    }

    void Update()
    {
        // Only process the watchdog logic on clients
        if (!isClient) return;

        // Watchdog: Did the local JUHealth change without server permission?
        if (Mathf.Abs(_juHealth.Health - netHealth) > 0.01f)
        {
            float difference = netHealth - _juHealth.Health;

            // Instantly revert to the authoritative server state to prevent desyncs
            _juHealth.Health = netHealth;
            _juHealth.IsDead = netIsDead;

            // If health dropped (took damage locally)
            if (difference > 0)
            {
                if (!isLocalPlayer)
                {
                    // SHOOTER AUTHORITY: I am observing a remote player take damage on my screen (my bullet hit them).
                    // Route the damage request through MY local player connection.
                    if (LocalInstance != null)
                    {
                        LocalInstance.CmdDealDamage(this.gameObject, difference);
                    }
                }
                else
                {
                    // VICTIM AUTHORITY: I took environment damage locally on my own screen (e.g., Fall Damage, Spikes).
                    CmdTakeEnvironmentDamage(difference);
                }
            }
        }
    }

    // =========================================================
    // COMMANDS (Client -> Server)
    // =========================================================

    [Command]
    public void CmdDealDamage(GameObject target, float amount)
    {
        // Security Check: Prevent spoofed massive damage hacks
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        if (target != null)
        {
            var targetScript = target.GetComponent<NetworkManager>();
            if (targetScript != null)
            {
                targetScript.ServerApplyDamage(amount);
            }
        }
    }

    [Command]
    public void CmdTakeEnvironmentDamage(float amount)
    {
        // Security Check: Prevent spoofed massive fall damage
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        ServerApplyDamage(amount);
    }

    // =========================================================
    // SERVER LOGIC
    // =========================================================

    [Server]
    public void ServerApplyDamage(float amount)
    {
        if (netIsDead) return;

        netHealth -= amount;

        // Clamp health to prevent negative values
        netHealth = Mathf.Clamp(netHealth, 0, _juHealth.MaxHealth);

        if (netHealth <= 0)
        {
            netIsDead = true;
        }

        // Apply on the server so physics/events process correctly
        _juHealth.Health = netHealth;

        if (netIsDead)
        {
            _juHealth.CheckHealthState();
        }
    }

    // =========================================================
    // SYNCVAR HOOKS (Server -> Client Visuals)
    // =========================================================

    private void OnServerHealthChanged(float oldHealth, float newHealth)
    {
        _juHealth.Health = newHealth;

        if (isLocalPlayer && newHealth < oldHealth && !netIsDead)
        {
            BloodScreen.PlayerTakingDamaged();
        }
    }

    private void OnDeathStateChanged(bool oldState, bool newState)
    {
        _juHealth.IsDead = newState;

        if (newState)
        {
            _juHealth.Health = 0;
            // Trigger native JUTPS ragdoll and death events safely via hook
            _juHealth.CheckHealthState();
        }
    }
}