using JUTPS;
using JUTPS.FX;
using JUTPS.PhysicsScripts;
using Mirror;
using UnityEngine;

[RequireComponent(typeof(JUHealth))]
public class PlayerHealthManager : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private JUHealth _juHealth;
    public static PlayerHealthManager LocalInstance { get; private set; }
    private const float MAX_ALLOWED_DAMAGE = 500f;
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
        LocalInstance = this;
    }
    public override void OnStartServer()
    {
        netHealth = _juHealth.Health;
        netIsDead = _juHealth.IsDead;
    }

    void Update()
    {
        if (!isClient) return;

        if (Mathf.Abs(_juHealth.Health - netHealth) > 0.01f)
        {
            float difference = netHealth - _juHealth.Health;

            _juHealth.Health = netHealth;
            _juHealth.IsDead = netIsDead;

            if (difference > 0)
            {
                if (!isLocalPlayer)
                {
                    if (LocalInstance != null)
                    {
                        LocalInstance.CmdDealDamage(this.gameObject, difference);
                    }
                }
                else
                {
                    CmdTakeEnvironmentDamage(difference);
                }
            }
        }
    }

    [Command]
    public void CmdDealDamage(GameObject target, float amount)
    {
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        if (target != null)
        {
            var targetScript = target.GetComponent<PlayerHealthManager>();
            if (targetScript != null)
            {
                targetScript.ServerApplyDamage(amount);
            }
        }
    }

    [Command]
    public void CmdTakeEnvironmentDamage(float amount)
    {
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        ServerApplyDamage(amount);
    }

 
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
            if (isLocalPlayer)
            {
                Invoke(nameof(HostDeathSequence), 3f);
            }
            else
            {
                Invoke(nameof(ClientDeathSequence), 3f);
            }
        }

        // Apply on the server 
        _juHealth.Health = netHealth;

        if (netIsDead)
        {
            _juHealth.CheckHealthState();
        }
    }

    [Server]
    private void HostDeathSequence()
    {
        Debug.Log("[Server] Host died. Keeping server alive and hiding the body.");
        RpcDisableCharacter();
    }

    [Server]
    private void ClientDeathSequence()
    {
        Debug.Log("[Server] Client died. Instructing client to disconnect and return to menu.");

        TargetDisconnect(connectionToClient);

        NetworkServer.Destroy(gameObject);
    }
    [Server]

    [TargetRpc]
    private void TargetDisconnect(NetworkConnection target)
    {
        Debug.Log("[Client] died. Showing loading screen and disconnecting.");

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowLoadingPanel();
        }

        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StopClient();
        }
    }

    [ClientRpc]
    private void RpcDisableCharacter()
    {
        Debug.Log("Cleaning up player physics and visuals...");

        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }

        if (TryGetComponent(out CapsuleCollider mainCollider))
        {
            mainCollider.enabled = false;
            Debug.Log("Main Collider Disabled");
        }

        if (TryGetComponent(out Rigidbody rb))
        {
            rb.isKinematic = true; 
            rb.detectCollisions = false; 
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
            _juHealth.CheckHealthState();

        }
    }
}