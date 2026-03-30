// ============================================================
// PlayerNetworkSetup.cs
// PURPOSE: Initialize the player when they spawn on the network.
//          LOCAL PLAYER  → enable camera, mobile controls, register
//          REMOTE PLAYER → disable local-only scripts, make kinematic
//
// REPLACES: JU_NetSetup.cs
// SINGLE RESPONSIBILITY: Setup only. No syncing here.
// ============================================================

using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.CameraSystems;
using JUTPS.JUInputSystem;
using JUTPS.CrossPlataform;

/// <summary>
/// Runs once when the player spawns.
/// Decides: "Am I the local player or someone else's player?"
/// Then sets up accordingly.
/// </summary>
[DisallowMultipleComponent]
public class PlayerNetworkSetup : NetworkBehaviour
{
    // ── Inspector Fields ─────────────────────────────────────
    [Header("Scripts that only run on the LOCAL player")]
    [Tooltip("Drag scripts here that should be DISABLED on remote players")]
    [SerializeField] private MonoBehaviour[] _localOnlyScripts;

    [Header("Objects that only show on the LOCAL player")]
    [Tooltip("Drag GameObjects here that should be HIDDEN for remote players")]
    [SerializeField] private GameObject[] _localOnlyObjects;

    [Header("Mobile Settings")]
    [Tooltip("Force enable mobile controls? (Use for testing on PC)")]
    [SerializeField] private bool _forceMobileMode = false;

    // ── Private References ───────────────────────────────────
    private JUCharacterController _characterController;
    private Animator _playerAnimator;

    // ════════════════════════════════════════════════════════
    // MIRROR CALLBACKS
    // These are called automatically by Mirror when the
    // player object spawns on the network.
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Called ONLY on the device that OWNS this player.
    /// Example: Called on Player 1's phone for Player 1's character.
    /// </summary>
    public override void OnStartLocalPlayer()
    {
        gameObject.tag = "Player";

        CacheComponents();
        RegisterWithGameManager();
        SetupCamera();

        if (_forceMobileMode)
            EnableMobileControls();

        // Tell all other systems "local player is ready!"
        NetworkEventBus.RaiseLocalPlayerSpawned(gameObject);

        Debug.Log($"[PlayerNetworkSetup] Local player initialized: {gameObject.name}");
    }

    /// <summary>
    /// Called on EVERY device (including remote devices) when this player spawns.
    /// We use it to set up REMOTE players.
    /// </summary>
    public override void OnStartClient()
    {
        // If this IS our local player, OnStartLocalPlayer already handled it
        if (isLocalPlayer) return;

        // This is someone else's player — disable local-only components
        gameObject.tag = "Untagged";
        DisableLocalOnlyComponents();

        // Make physics kinematic — network will control position, not physics
        if (TryGetComponent<Rigidbody>(out var rb))
            rb.isKinematic = true;

        // Keep animator ON — NetworkAnimator needs it to sync animations
        if (_playerAnimator != null)
            _playerAnimator.enabled = true;

        Debug.Log($"[PlayerNetworkSetup] Remote player setup complete: {gameObject.name}");
    }

    // ════════════════════════════════════════════════════════
    // CLEANUP
    // ════════════════════════════════════════════════════════

    private void OnDestroy()
    {
        if (!isLocalPlayer) return;

        // Unregister from game manager when we leave
        if (JUGameManager.PlayerController == _characterController)
            JUGameManager.PlayerController = null;

        // Re-enable keyboard input if we had blocked it for mobile
        JUInput.Instance()?.DisableBlockStandardInputs();

        // Tell other systems the player is gone
        NetworkEventBus.RaiseLocalPlayerDespawned();
    }

    // ════════════════════════════════════════════════════════
    // PRIVATE HELPER METHODS
    // ════════════════════════════════════════════════════════

    private void CacheComponents()
    {
        _characterController = GetComponent<JUCharacterController>();

        // Try to find the Animator — first on this object, then in children
        _playerAnimator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
    }

    private void RegisterWithGameManager()
    {
        if (_characterController == null)
        {
            Debug.LogError("[PlayerNetworkSetup] JUCharacterController not found! " +
                           "Make sure it's on the player prefab.");
            return;
        }

        // JUGameManager needs to know which character is "our" player
        JUGameManager.PlayerController = _characterController;
        Debug.Log("[PlayerNetworkSetup] Player registered with JUGameManager.");
    }

    private void SetupCamera()
    {
        // Find the TPS camera (it's usually disabled until a player spawns)
        var cam = FindFirstObjectByType<TPSCameraController>(FindObjectsInactive.Include);
        if (cam == null)
        {
            Debug.LogWarning("[PlayerNetworkSetup] TPSCameraController not found in scene.");
            return;
        }

        cam.TargetToFollow = transform;
        cam.characterTarget = _characterController;
        cam.gameObject.SetActive(true);
        cam.enabled = true;
        Debug.Log("[PlayerNetworkSetup] Camera assigned to local player.");
    }

    private void EnableMobileControls()
    {
        var mobileRig = FindFirstObjectByType<MobileRig>(FindObjectsInactive.Include);
        if (mobileRig == null)
        {
            Debug.LogWarning("[PlayerNetworkSetup] MobileRig not found in scene.");
            return;
        }

        mobileRig.gameObject.SetActive(true);
        mobileRig.enabled = true;

        // Block keyboard/mouse input so mobile joystick takes over
        JUInput.Instance()?.EnableBlockStandardInputs();
        Debug.Log("[PlayerNetworkSetup] Mobile controls enabled.");
    }

    private void DisableLocalOnlyComponents()
    {
        foreach (var script in _localOnlyScripts)
        {
            if (script != null)
                script.enabled = false;
        }

        foreach (var obj in _localOnlyObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }
}