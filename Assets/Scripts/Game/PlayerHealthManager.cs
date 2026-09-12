using JUTPS;
using JUTPS.FX;
using Mirror;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;

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

    [Header("Friendly Fire Debug")]
    [Tooltip("Console logs for damage and team testing. Off by default: this fires on every health change on every client.")]
    [SerializeField] private bool enableDamageDebug = false;

    [Header("Power-Ups")]
    public GameObject[] powerUpPrefabs;

    private static readonly Dictionary<GameObject, (PlayerHealthManager health, LSPlayer player)> ServerPlayerCache = new Dictionary<GameObject, (PlayerHealthManager, LSPlayer)>();

    void Awake()
    {
        if (_juHealth == null) _juHealth = GetComponent<JUHealth>();
        _playerData = GetComponent<LSPlayer>();
    }

    public override void OnStartLocalPlayer()
    {
        LocalInstance = this;
    }

public override void OnStopLocalPlayer()
    {
        // Bullet.cs and Damager.cs route damage through this static reference.
        // Leaving it pointing at a destroyed object across a scene change is asking
        // for trouble, so drop it as soon as the local player goes away.
        if (LocalInstance == this)
            LocalInstance = null;
    }


    public override void OnStartServer()
    {
        netHealth = _juHealth.Health;
        netIsDead = _juHealth.IsDead;
        ServerPlayerCache[gameObject] = (this, _playerData);

        if (enableDamageDebug && _playerData != null)
        {
            Debug.Log(
                $"[DAMAGE DEBUG] Server Player Ready | " +
                $"Player = {_playerData.playerName} | NetId = {netId} | TeamID = {_playerData.teamID}"
            );
        }
    }

public override void OnStopServer()
    {
        ServerPlayerCache.Remove(gameObject);

        // The cache is static, so an editor host restart would otherwise leak
        // entries pointing at destroyed objects.
        if (!NetworkServer.active)
        {
            ServerPlayerCache.Clear();
        }

        base.OnStopServer();
    }

[Command]
    public void CmdShootTarget(GameObject target, float weaponDamage)
    {
        // Validate the damage value first.
        if (weaponDamage <= 0 || weaponDamage > MAX_ALLOWED_DAMAGE)
        {
            if (enableDamageDebug)
            {
                Debug.LogWarning($"[DAMAGE DEBUG] Invalid weapon damage = {weaponDamage}");
            }

            return;
        }

        if (target == null)
        {
            if (enableDamageDebug)
            {
                Debug.LogWarning("[DAMAGE DEBUG] CmdShootTarget called with NULL target.");
            }

            return;
        }

        PlayerHealthManager targetScript = null;
        LSPlayer targetPlayer = null;

        // Try the server cache first.
        if (ServerPlayerCache.TryGetValue(target, out var cache))
        {
            targetScript = cache.health;
            targetPlayer = cache.player;
        }
        else
        {
            targetScript = target.GetComponent<PlayerHealthManager>();

            if (targetScript != null)
            {
                targetPlayer = target.GetComponent<LSPlayer>();
            }
        }

        if (enableDamageDebug)
        {
            string attackerName = _playerData != null ? _playerData.playerName : "NULL";
            int attackerTeam = _playerData != null ? _playerData.teamID : -999;

            string targetName = targetPlayer != null ? targetPlayer.playerName : "NULL";
            int targetTeam = targetPlayer != null ? targetPlayer.teamID : -999;

            Debug.Log(
                $"[DAMAGE DEBUG] CmdShootTarget CALLED | " +
                $"Attacker = {attackerName} Team = {attackerTeam} | " +
                $"Target = {targetName} Team = {targetTeam} | " +
                $"Damage = {weaponDamage}"
            );
        }

        if (targetScript == null)
        {
            if (enableDamageDebug)
            {
                Debug.LogWarning(
                    $"[DAMAGE DEBUG] Target has no PlayerHealthManager | Target Object = {target.name}"
                );
            }

            return;
        }

        // Server-authoritative friendly fire check. Bullets, melee damagers and this
        // command all share one rule, so teammates can never hurt each other.
        if (FriendlyFireUtility.IsSameTeam(gameObject, target))
        {
            if (enableDamageDebug)
            {
                Debug.Log(
                    $"[DAMAGE DEBUG] FRIENDLY FIRE BLOCKED | " +
                    $"Attacker Team = {(_playerData != null ? _playerData.teamID : 0)} | " +
                    $"Target Team = {(targetPlayer != null ? targetPlayer.teamID : 0)}"
                );
            }

            // Damage is cancelled completely: no health change, no death, no power-up drop.
            return;
        }

        if (enableDamageDebug)
        {
            Debug.Log(
                $"[DAMAGE DEBUG] ENEMY DAMAGE ALLOWED | " +
                $"Target = {target.name} | Damage = {weaponDamage}"
            );
        }

        // Apply valid enemy damage on the server.
        targetScript.ServerApplyDamage(weaponDamage);
    }

    [Command]
    public void CmdTakeEnvironmentDamage(float amount)
    {
        if (amount <= 0 || amount > MAX_ALLOWED_DAMAGE) return;

        if (enableDamageDebug)
        {
            Debug.Log(
                $"[DAMAGE DEBUG] Environment Damage | Player = {gameObject.name} | " +
                $"TeamID = {(_playerData != null ? _playerData.teamID : -1)} | Damage = {amount}"
            );
        }

        ServerApplyDamage(amount);
    }

    [Server]
    public void ServerApplyDamage(float amount)
    {
        // 1. NO DAMAGE IN LOBBY SCENE
        if (SceneManager.GetActiveScene().name == "LobbyScene") return;

        if (netIsDead) return;

        if (enableDamageDebug)
        {
            Debug.Log(
                $"[DAMAGE DEBUG] ServerApplyDamage | Player = {gameObject.name} | " +
                $"TeamID = {(_playerData != null ? _playerData.teamID : -1)} | " +
                $"Old Health = {netHealth} | Damage = {amount}"
            );
        }

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
        if (enableDamageDebug)
        {
            Debug.Log(
                $"[DAMAGE DEBUG] Health Sync Changed | Player = {gameObject.name} | " +
                $"TeamID = {(_playerData != null ? _playerData.teamID : -1)} | " +
                $"Old = {oldHealth} | New = {newHealth}"
            );
        }

        _juHealth.Health = newHealth;

        if (isLocalPlayer && newHealth < oldHealth && !netIsDead)
        {
            if (enableDamageDebug)
            {
                Debug.Log("[DAMAGE DEBUG] Blood Screen Triggered.");
            }

            BloodScreen.PlayerTakingDamaged();
        }
    }

private void OnDeathStateChanged(bool oldState, bool newState)
    {
        _juHealth.IsDead = newState;

        if (newState)
        {
            // Match the server: a corpse must stop being a valid damage target on
            // clients too, otherwise bullets keep registering hits and hit markers.
            gameObject.tag = "Untagged";

            _juHealth.Health = 0;
            _juHealth.CheckHealthState();
        }
    }
}