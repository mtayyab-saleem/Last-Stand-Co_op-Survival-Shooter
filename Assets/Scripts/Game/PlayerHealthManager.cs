//using JUTPS;
//using JUTPS.FX;
//using Mirror;
//using UnityEngine;
//using UnityEngine.SceneManagement; 
//using System.Linq; 

//[RequireComponent(typeof(JUHealth))]
//public class PlayerHealthManager : NetworkBehaviour
//{
//    [Header("Core References")]
//    [SerializeField] private JUHealth _juHealth;
//    public static PlayerHealthManager LocalInstance { get; private set; }
//    private const float MAX_ALLOWED_DAMAGE = 500f;

//    [SyncVar(hook = nameof(OnServerHealthChanged))]
//    public float netHealth = 100f;

//    [SyncVar(hook = nameof(OnDeathStateChanged))]
//    public bool netIsDead = false;

//    private LSPlayer _playerData;

//    void Awake()
//    {
//        if (_juHealth == null) _juHealth = GetComponent<JUHealth>();
//        _playerData = GetComponent<LSPlayer>(); 
//    }

//    public override void OnStartLocalPlayer()
//    {
//        LocalInstance = this;
//    }

//    public override void OnStartServer()
//    {
//        netHealth = _juHealth.Health;
//        netIsDead = _juHealth.IsDead;
//    }

//    void Update()
//    {
//        if (!isClient) return;

//        if (Mathf.Abs(_juHealth.Health - netHealth) > 0.01f)
//        {
//            float difference = netHealth - _juHealth.Health;

//            _juHealth.Health = netHealth;
//            _juHealth.IsDead = netIsDead;

//            if (difference > 0)
//            {
//                if (!isLocalPlayer)
//                {
//                    if (LocalInstance != null)
//                    {
//                        LocalInstance.CmdDealDamage(this.gameObject, difference);
//                    }
//                }
//                else
//                {
//                    CmdTakeEnvironmentDamage(difference);
//                }
//            }
//        }
//    }

//    [Command]
//    //public void CmdDealDamage(GameObject target, float amount)
//    //{
//    //    if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

//    //    if (target != null)
//    //    {
//    //        var targetScript = target.GetComponent<PlayerHealthManager>();
//    //        if (targetScript != null)
//    //        {
//    //            targetScript.ServerApplyDamage(amount);
//    //        }
//    //    }
//    //}


//    public void CmdDealDamage(GameObject target, float amount)
//    {
//        // Ignore invalid damage amounts to prevent exploits
//        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

//        if (target != null)
//        {
//            var targetHealth = target.GetComponent<PlayerHealthManager>();

//            // Fetch LSPlayer components to verify team affiliations
//            var targetPlayer = target.GetComponent<LSPlayer>();
//            var shooterPlayer = this.GetComponent<LSPlayer>();

//            if (targetHealth != null && targetPlayer != null && shooterPlayer != null)
//            {
//                // RULE: Block Friendly Fire. If both players belong to the same team, ignore the damage.
//                if (shooterPlayer.teamID == targetPlayer.teamID)
//                {
//                    Debug.Log($"[Friendly Fire Blocked] '{shooterPlayer.playerName}' hit teammate '{targetPlayer.playerName}'. No damage applied.");
//                    return;
//                }

//                // If the target is an enemy (different team ID), apply the damage normally
//                targetHealth.ServerApplyDamage(amount);
//            }
//        }
//    }

//    [Command]
//    public void CmdTakeEnvironmentDamage(float amount)
//    {
//        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

//        ServerApplyDamage(amount);
//    }

//    [Server]
//    public void ServerApplyDamage(float amount)
//    {
//        // 1. NO DAMAGE IN LOBBY SCENE
//        if (SceneManager.GetActiveScene().name == "LobbyScene") return;

//        if (netIsDead) return;

//        netHealth -= amount;

//        // Clamp health to prevent negative values
//        netHealth = Mathf.Clamp(netHealth, 0, _juHealth.MaxHealth);

//        if (netHealth <= 0)
//        {
//            netIsDead = true;
//            if (_playerData != null) _playerData.isAlive = false; // Mark dead for team logic

//            if (isLocalPlayer)
//            {
//                Invoke(nameof(HostDeathSequence), 3f);
//            }
//            else
//            {
//                Invoke(nameof(ClientDeathSequence), 3f);
//            }
//        }

//        // Apply on the server 
//        _juHealth.Health = netHealth;

//        if (netIsDead)
//        {
//            _juHealth.CheckHealthState();
//        }
//    }

//    [Server]
//    private void HostDeathSequence()
//    {
//        Debug.Log("[Server] Host died. Enabling Free Roam.");
//        RpcDisableCharacter();
//    }

//    [Server]
//    private void ClientDeathSequence()
//    {
//        Debug.Log("[Server] Client died. Kicking to Main Menu.");
//        RpcDisableCharacter();
//        TargetDisconnect(connectionToClient); 
//        NetworkServer.Destroy(gameObject);
//    }

//    [TargetRpc]
//    private void TargetDisconnect(NetworkConnection target)
//    {
//        Debug.Log("[Client] died completely. Showing loading screen and disconnecting.");

//        if (GameUIManager.Instance != null)
//        {
//            GameUIManager.Instance.TriggerDisconnectSequence();
//        }
//    }

//    [ClientRpc]
//    private void RpcDisableCharacter()
//    {
//        Debug.Log("Cleaning up player physics and visuals...");

//        foreach (Transform child in transform)
//        {
//            child.gameObject.SetActive(false);
//        }

//        if (TryGetComponent(out CapsuleCollider mainCollider))
//        {
//            mainCollider.enabled = false;
//            Debug.Log("Main Collider Disabled");
//        }

//        if (TryGetComponent(out Rigidbody rb))
//        {
//            rb.isKinematic = true;
//            rb.detectCollisions = false;
//        }

//        if (TryGetComponent(out JUTPS.PhysicsScripts.AdvancedRagdollController ragdoll))
//        {
//            ragdoll.enabled = false;

//            if (ragdoll.RagdollBones != null)
//            {
//                foreach (var boneRb in ragdoll.RagdollBones)
//                {
//                    boneRb.isKinematic = true;
//                    boneRb.detectCollisions = false;
//                }
//            }
//        }

//        if (TryGetComponent(out JUHealth health))
//        {
//            health.enabled = false;
//        }
//    }
//    private void OnServerHealthChanged(float oldHealth, float newHealth)
//    {
//        _juHealth.Health = newHealth;

//        if (isLocalPlayer && newHealth < oldHealth && !netIsDead)
//        {
//            BloodScreen.PlayerTakingDamaged();
//        }
//    }

//    private void OnDeathStateChanged(bool oldState, bool newState)
//    {
//        _juHealth.IsDead = newState;

//        if (newState)
//        {
//            _juHealth.Health = 0;
//            _juHealth.CheckHealthState();

//        }
//    }
//}


using JUTPS;
using JUTPS.FX;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;

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

    // OPTIMIZATION: Cached public reference.
    public LSPlayer playerData { get; private set; }

    void Awake()
    {
        if (_juHealth == null) _juHealth = GetComponent<JUHealth>();
        playerData = GetComponent<LSPlayer>();
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
                        LocalInstance.CmdDealDamage(this, difference);
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
    public void CmdDealDamage(PlayerHealthManager targetHealth, float amount)
    {
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        if (targetHealth != null)
        {
            if (this.playerData != null && targetHealth.playerData != null)
            {
                // --- MAIN FIX: Solo mode mein friendly fire hamesha bypass hoga ---
                bool isSoloMode = false;
                if (LSMatchManager.Instance != null && LSMatchManager.Instance.currentMode == LSMatchManager.GameMode.Solo)
                {
                    isSoloMode = true; // Solo mein koi team nahi hoti, sab enemies hain!
                }

                // Agar Solo nahi hai, Team ID assign ho chuki hai (!= -1), aur dono ki team same hai, toh block karein
                if (!isSoloMode && this.playerData.teamID != -1 && this.playerData.teamID == targetHealth.playerData.teamID)
                {
                    Debug.Log($"[Friendly Fire Blocked] Shooter '{this.playerData.playerName}' hit teammate '{targetHealth.playerData.playerName}'. Zero damage applied.");
                    return;
                }
            }

            // Target is an enemy, apply damage
            targetHealth.ServerApplyDamage(amount);
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
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;
        if (netIsDead) return;

        netHealth -= amount;
        netHealth = Mathf.Clamp(netHealth, 0, _juHealth.MaxHealth);

        if (netHealth <= 0)
        {
            netIsDead = true;
            if (playerData != null) playerData.isAlive = false;

            if (isLocalPlayer)
            {
                Invoke(nameof(HostDeathSequence), 3f);
            }
            else
            {
                Invoke(nameof(ClientDeathSequence), 3f);
            }
        }

        _juHealth.Health = netHealth;

        if (netIsDead)
        {
            _juHealth.CheckHealthState();
        }
    }

    [Server]
    private void HostDeathSequence()
    {
        Debug.Log("[Server] Host died. Enabling Ghost Free Roam Mode.");
        RpcDisableCharacter();
    }

    [Server]
    private void ClientDeathSequence()
    {
        Debug.Log("[Server] Client died. Initiating disconnect to Main Menu.");
        RpcDisableCharacter();
        TargetDisconnect(connectionToClient);
        NetworkServer.Destroy(gameObject);
    }

    [TargetRpc]
    private void TargetDisconnect(NetworkConnection target)
    {
        Debug.Log("[Client] Target completely dead. Returning to Lobby/Main Menu.");
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
        }
    }

    [ClientRpc]
    private void RpcDisableCharacter()
    {
        Debug.Log("[Client] Cleaning up player physics and visual models...");

        foreach (Transform child in transform) child.gameObject.SetActive(false);

        if (TryGetComponent(out CapsuleCollider mainCollider)) mainCollider.enabled = false;

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

        if (TryGetComponent(out JUHealth health)) health.enabled = false;
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