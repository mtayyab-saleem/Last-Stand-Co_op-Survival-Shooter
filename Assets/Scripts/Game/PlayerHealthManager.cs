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

    private LSPlayer _playerData;
    [Header("Power-Ups")]
    public GameObject[] powerUpPrefabs;

    void Awake()
    {
        if (_juHealth == null) _juHealth = GetComponent<JUHealth>();
        _playerData = GetComponent<LSPlayer>(); 
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

    [Command]
    public void CmdShootTarget(GameObject target, float weaponDamage)
    {
        // Server-side validation
        if (weaponDamage <= 0 || weaponDamage > MAX_ALLOWED_DAMAGE) return;

        if (target != null)
        {
            var targetScript = target.GetComponent<PlayerHealthManager>();
            if (targetScript != null)
            {
                var targetPlayer = target.GetComponent<LSPlayer>();

                // Friendly fire validation
                if (_playerData != null && targetPlayer != null)
                {
                    if (_playerData.teamID == targetPlayer.teamID && _playerData.teamID != -1)
                    {
                        return;
                    }
                }
                
                // Process shot and apply damage directly
                targetScript.ServerApplyDamage(weaponDamage);
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
        // 1. NO DAMAGE IN LOBBY SCENE
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;

        if (netIsDead) return;

        netHealth -= amount;

        // Clamp health to prevent negative values
        netHealth = Mathf.Clamp(netHealth, 0, _juHealth.MaxHealth);

        if (netHealth <= 0)
        {
            gameObject.tag = "Untagged";
            netIsDead = true;
            if (_playerData != null) _playerData.isAlive = false; // Mark dead for team logic
            SpawnRandomPowerUp();
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
    private void SpawnRandomPowerUp()
    {
        if (powerUpPrefabs == null || powerUpPrefabs.Length == 0) return;

        int randomIndex = Random.Range(0, powerUpPrefabs.Length);
        GameObject selectedPrefab = powerUpPrefabs[randomIndex];

        if (selectedPrefab != null)
        {
            Vector3 spawnPosition = transform.position + (Vector3.up * 0.5f);
            GameObject spawnedPowerUp = Instantiate(selectedPrefab, spawnPosition, Quaternion.identity);
            NetworkServer.Spawn(spawnedPowerUp);
        }
    }
    [Server]
    private void HostDeathSequence()
    {
        Debug.Log("[Server] Host died. Enabling Free Roam.");
        RpcDisableCharacter();
    }

    [Server]
    private void ClientDeathSequence()
    {
        Debug.Log("[Server] Client died. Kicking to Main Menu.");
        RpcDisableCharacter();
        TargetDisconnect(connectionToClient); 
        NetworkServer.Destroy(gameObject);
    }

    [TargetRpc]
    private void TargetDisconnect(NetworkConnection target)
    {
        Debug.Log("[Client] died completely. Showing loading screen and disconnecting.");

        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
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