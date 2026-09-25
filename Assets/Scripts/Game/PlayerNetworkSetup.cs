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

        // JUTPS's BodyLeanInert switches the animator to physics-rate updates in its
        // Awake, so arms, gun and body only got a new pose on physics steps - which do
        // not line up with rendered frames. Root motion is off on this character, so
        // nothing needs physics-rate animation; animate every frame.
        if (playerAnimator != null)
            playerAnimator.updateMode = AnimatorUpdateMode.Normal;

        // AI PLAYER, ON THE SERVER: the server drives it, so unlike a remote player it
        // keeps its character controller and real physics. Everywhere else a bot is an
        // ordinary remote player and takes the branch below.
        if (TryGetComponent(out LSPlayer lsPlayer) && lsPlayer.IsServerBot)
        {
            SetupServerBot();
            return;
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

            StripRemotePlayerCost();

            if (playerAnimator != null)
            {
                playerAnimator.enabled = true;
            }
        }
        // LOCAL PLAYER
        else
        {
            gameObject.tag = "Player";
            SetupLocalPlayer();
        }
    }

    /// <summary>
    /// Input, inventory pickups and footsteps are all player-only; the character
    /// controller stays on because LSBotBrain moves the bot through it.
    /// </summary>
    void SetupServerBot()
    {
        foreach (var s in scriptsToDisable)
        {
            if (s == null || s is JUCharacterController)
                continue;

            // ItemSwitchManager queues "equip the start item" with Invoke in its Start,
            // and Invoke still fires on a disabled component: 0.2 s after spawning it
            // switched the bot back to empty hands, leaving it stuck in fire mode with
            // no gun. Anything these player-only scripts had lined up is cancelled.
            s.CancelInvoke();
            s.enabled = false;
        }

        gameObject.tag = "Untagged";
        StripRemotePlayerCost();

        if (TryGetComponent(out JUCharacterController character))
            DisableStepUp(character);

        // LSBotBrain decides the culling mode: bones must keep updating off-screen only
        // while the bot is fighting.
        if (playerAnimator != null)
            playerAnimator.enabled = true;
    }

    /// <summary>
    /// Remote players only need to look right, not to light anything.
    /// The character prefab carries a Spot Light that is only meaningful for the player
    /// holding the controls - with 8 players in a match that was 8 realtime lights for
    /// no visual gain. Renderers, animator and colliders are deliberately left alone.
    ///
    /// Particle systems are deliberately left alone too: every one on this prefab is a
    /// bullet shell emitter that never plays unless a shot fires it, so disabling them
    /// saved nothing and only stopped remote players from ejecting shells.
    /// </summary>
    void StripRemotePlayerCost()
    {
        foreach (Light light in GetComponentsInChildren<Light>(true))
            light.enabled = false;

        // Skinning every frame regardless of visibility is pure waste. Using the
        // precomputed bounds lets Unity cull these meshes when off-camera.
        foreach (SkinnedMeshRenderer skin in GetComponentsInChildren<SkinnedMeshRenderer>(true))
        {
            skin.updateWhenOffscreen = false;
            skin.skinnedMotionVectors = false;
        }
    }

    /// <summary>
    /// JUTPS's step-up pushes the body up with an impulse every physics step whenever
    /// the ground ahead is a little higher and flat, and counts the character as
    /// grounded for as long as it is doing so - so it never falls back. On terrain it
    /// fired on every bump: the player hopped by itself while sprinting, and bots, which
    /// run all the time, piled the impulses up and floated off the ground. The terrain
    /// has no stairs or kerbs for it to help with; the capsule rides over bumps itself.
    /// </summary>
    static void DisableStepUp(JUCharacterController character)
    {
        character.EnableStepCorrection = false;
        character.EnableUngroundedStepUp = false;
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

        DisableStepUp(character);

        // Nothing steeper than 55 degrees can be climbed; JUTPS slides the player off it.
        // This player only - bots keep JUTPS's own slope handling.
        character.MaxWalkableAngle = 55f;
        if (!TryGetComponent(out PlayerSlopeLimiter _))
            gameObject.AddComponent<PlayerSlopeLimiter>();

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