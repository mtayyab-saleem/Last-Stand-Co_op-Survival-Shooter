using UnityEngine;
using Mirror;
using JUTPS.CameraSystems;
using JUTPS;
using JUTPS.CrossPlataform;
using JUTPS.JUInputSystem;

public class PlayerNetworkSetup : NetworkBehaviour
{
    public MonoBehaviour[] scriptsToDisable;
    [Header("Network Sync Components")]
    [Tooltip("Player's Animator (Auto-find if not assigned)")]
    public Animator playerAnimator;

    void Start()
    {
        if (playerAnimator == null)
        {
            playerAnimator = GetComponent<Animator>();
        }

        // REMOTE PLAYERS
        if (!isLocalPlayer)
        {
            foreach (var s in scriptsToDisable) s.enabled = false;
            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;      // Physics engine ko calculation se rokna
                rb.useGravity = false;      // Gravity ka bojh hatana
                rb.interpolation = RigidbodyInterpolation.None; // Interpolation CPU khati hai
            }

            // Main Collider ko Trigger kar dein ya band kar dein
            if (TryGetComponent(out CapsuleCollider col))
            {
                col.isTrigger = true; // Takkar ki physics calculation khatam
            }
            //if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
            gameObject.tag = "Untagged";

            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
                Debug.Log($"Remote player animator enabled: {gameObject.name}");
            }
        }
        // LOCAL PLAYER
        else
        {
            gameObject.tag = "Player";
            SetupLocalPlayer();
        }
    }

    void SetupLocalPlayer()
    {
        // GAME MANAGER SETUP
        var character = GetComponent<JUCharacterController>();
        if (character != null)
        {
            JUGameManager.PlayerController = character;
            Debug.Log("Player registered with JUGameManager");
        }
        else
        {
            Debug.LogError("JUCharacterController not found on player!");
            return;
        }

        //CAMERA SETUP
        var cam = CameraManager.MainCam;
        if (cam != null)
        {
            cam.TargetToFollow = this.transform;
            cam.characterTarget = character;
            cam.gameObject.SetActive(true);
            cam.enabled = true;
            Debug.Log("Camera setup complete");
        }


        SetupNetworkAnimator();
    }

    void SetupNetworkAnimator()
    {
        
        if (TryGetComponent(out NetworkAnimator netAnimator))
        {
            Debug.Log($"Animation sync is perfectly active for {gameObject.name}");
        }
        else
        {
            Debug.LogError("NetworkAnimator missing on Player Prefab!");
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();

        if (isLocalPlayer)
        {
            if (!NetworkClient.isConnected)
            {
                Debug.Log("[PlayerNetworkSetup] Connection lost. Returning to Main Menu.");
                if (GameUIManager.Instance != null)
                {
                    GameUIManager.Instance.TriggerDisconnectSequence();
                }
            }
        }
    }

    [Command]
    public void CmdCollectPowerup(GameObject powerup)
    {
        if (powerup == null) return;

        // Health PowerUp Logic
        var healthPowerUp = powerup.GetComponent<JUTPS.PowerUps.HealthPowerUp>();
        if (healthPowerUp != null)
        {
            var healthManager = GetComponent<PlayerHealthManager>();
            if (healthManager != null)
            {
                var juHealth = GetComponent<JUHealth>();
                if (juHealth != null && healthManager.netHealth < juHealth.MaxHealth)
                {
                    healthManager.netHealth += healthPowerUp.HealthToAdd;
                    healthManager.netHealth = Mathf.Clamp(healthManager.netHealth, 0, juHealth.MaxHealth);
                    juHealth.Health = healthManager.netHealth;
                }
            }
            NetworkServer.Destroy(powerup);
            return;
        }

        // Ammo PowerUp Logic
        var ammoBox = powerup.GetComponent<JUTPS.WeaponSystem.AmmoBox>();
        if (ammoBox != null)
        {
            var pl = GetComponent<JUCharacterController>();
            if (pl != null && pl.IsItemEquiped)
            {
                // Apply on server so Host gets it, and sync to client if needed
                if (pl.WeaponInUseRightHand != null)
                {
                    if (pl.WeaponInUseRightHand.ItemName == ammoBox.WeaponName || ammoBox.WeaponName == "AnyWeapon")
                        pl.WeaponInUseRightHand.TotalBullets += pl.WeaponInUseLeftHand == null ? ammoBox.AmmoCount : ammoBox.AmmoCount / 2;
                }
                if (pl.WeaponInUseLeftHand != null)
                {
                    if (pl.WeaponInUseLeftHand.ItemName == ammoBox.WeaponName || ammoBox.WeaponName == "AnyWeapon")
                        pl.WeaponInUseLeftHand.TotalBullets += pl.WeaponInUseRightHand == null ? ammoBox.AmmoCount : ammoBox.AmmoCount / 2;
                }

                TargetApplyAmmo(connectionToClient, ammoBox.AmmoCount, ammoBox.WeaponName);
            }
            NetworkServer.Destroy(powerup);
            return;
        }
    }

    [TargetRpc]
    private void TargetApplyAmmo(NetworkConnection target, int ammoCount, string weaponName)
    {
        if (isServer) return; // Host already applied it on server

        var pl = GetComponent<JUCharacterController>();
        if (pl != null && pl.IsItemEquiped)
        {
            if (pl.WeaponInUseRightHand != null)
            {
                if (pl.WeaponInUseRightHand.ItemName == weaponName || weaponName == "AnyWeapon")
                    pl.WeaponInUseRightHand.TotalBullets += pl.WeaponInUseLeftHand == null ? ammoCount : ammoCount / 2;
            }
            if (pl.WeaponInUseLeftHand != null)
            {
                if (pl.WeaponInUseLeftHand.ItemName == weaponName || weaponName == "AnyWeapon")
                    pl.WeaponInUseLeftHand.TotalBullets += pl.WeaponInUseRightHand == null ? ammoCount : ammoCount / 2;
            }
        }
    }

    void OnDestroy()
    {
        if (isLocalPlayer)
        {
            if (TryGetComponent(out JUCharacterController character))
            {
                if (JUGameManager.PlayerController == character)
                {
                    JUGameManager.PlayerController = null;
                    Debug.Log("Player safely unregistered from JUGameManager");
                }
            }

            if (JUInput.Instance() != null && JUInput.Instance().IsBlockingDefaultInputs)
            {
                JUInput.Instance().DisableBlockStandardInputs();
            }
            var mobileRig = Object.FindFirstObjectByType<MobileRig>(FindObjectsInactive.Include);
            if (mobileRig != null)
            {
                mobileRig.gameObject.SetActive(false);
            }
        }
    }
}