using JUTPS;
using JUTPS.FX;
using JUTPS.PhysicsScripts;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(JUHealth))]
public class GameNetworkManager : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private JUHealth _juHealth;
    public static GameNetworkManager LocalInstance { get; private set; }

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
            var targetScript = target.GetComponent<GameNetworkManager>();
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
            Invoke(nameof(ServerCleanupPlayer), 2f);
        }

        // Apply on the server so physics/events process correctly
        _juHealth.Health = netHealth;

        if (netIsDead)
        {
            _juHealth.CheckHealthState();
        }
    }

    [Server]
    private void ServerCleanupPlayer()
    {
        RpcDisableCharacter();
        if (isServer && isLocalPlayer)
        {
            Debug.Log("🛡 Host character hidden, but server is still running.");
        }
        else
        {
            NetworkConnectionToClient conn = connectionToClient;
            if (conn != null)
            {
                Debug.Log($"🔌 Disconnecting and destroying player: {conn.connectionId}");

                conn.Disconnect();
            }
            NetworkServer.Destroy(gameObject);
        }

    }
    // =========================================================
    // SYNCVAR HOOKS (Server -> Client Visuals)
    // =========================================================
    [ClientRpc]
    private void RpcDisableCharacter()
    {
        Debug.Log("🧹 Cleaning up player physics and visuals...");

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        if (TryGetComponent(out CapsuleCollider mainCollider))
        {
            mainCollider.enabled = false;
            Debug.Log("🚫 Main Collider Disabled");
        }

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true; // Physics band
            rb.detectCollisions = false; // Takrana band
        }

        if (TryGetComponent(out JUTPS.PhysicsScripts.AdvancedRagdollController ragdoll))
        {
            ragdoll.enabled = false;

            if (ragdoll.RagdollBones != null)
            {
                foreach (var boneRb in ragdoll.RagdollBones)
                {
                    boneRb.isKinematic = true;
                    boneRb.detectCollisions = false;
                }
            }
        }

        // 5. Damage script (Health) ko bhi disable kardo taake phantom damage na ho
        if (TryGetComponent(out JUHealth health))
        {
            health.enabled = false;
        }
    }
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