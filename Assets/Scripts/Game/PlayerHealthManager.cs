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
                    if (difference >= 24f)
                    {
                        CmdTakeEnvironmentDamage(difference);
                    }
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
                var targetPlayer = target.GetComponent<LSPlayer>();

                if (_playerData != null && targetPlayer != null)
                {
                    // Tayyab's check is preserved here
                    if (_playerData.teamID == targetPlayer.teamID && _playerData.teamID != -1)
                    {
                        return;
                    }
                }
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
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;

        if (netIsDead) return;

        netHealth -= amount;
        netHealth = Mathf.Clamp(netHealth, 0, _juHealth.MaxHealth);

        if (netHealth <= 0)
        {
            gameObject.tag = "Untagged";
            netIsDead = true;
            if (_playerData != null) _playerData.isAlive = false;

            // Tayyab ki PowerUp logic intact hai
            SpawnRandomPowerUp();

            // --- HUMARA ADD KIYA GAYA CODE: Win Check Trigger ---
            if (LSMatchManager.Instance != null) LSMatchManager.Instance.CheckWinCondition();

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