// ============================================================
// PlayerMovementSync.cs
// PURPOSE: Sync player MOVEMENT and COMBAT ANIMATIONS across network.
//          LOCAL PLAYER  → reads input → sends to server
//          REMOTE PLAYER → receives data from server → applies to character
//
// REPLACES: The movement + animation part of JU_NetworkCompleteSync.cs
// SINGLE RESPONSIBILITY: Movement and animation sync ONLY.
//
// WHY REFLECTION?
//   JU TPS stores HorizontalX and VerticalY as private/protected fields.
//   We CANNOT access them normally from outside the class.
//   Reflection lets us read/write private variables by their name as a string.
//   It's not ideal, but it's the only way without modifying JU TPS source code.
// ============================================================

using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.JUInputSystem;
using System.Reflection;
using System.Collections;

[RequireComponent(typeof(JUCharacterController))]
[RequireComponent(typeof(NetworkAnimator))]
public class PlayerMovementSync : NetworkBehaviour
{
    // ── Component References (auto-found in Awake) ───────────
    private JUCharacterController _juController;
    private Animator _animator;
    private JUHealth _juHealth;

    // ── Reflection Fields ────────────────────────────────────
    // These let us access JU TPS private variables
    private FieldInfo _horizontalField;
    private FieldInfo _verticalField;

    // ── SyncVars: Movement States ────────────────────────────
    // SyncVar = Mirror automatically sends this value to all clients
    // whenever it changes on the server
    [SyncVar] private float _syncH;          // Horizontal input (-1 to 1)
    [SyncVar] private float _syncV;          // Vertical input (-1 to 1)
    [SyncVar] private bool _syncRunning;
    [SyncVar] private bool _syncCrouched;
    [SyncVar] private bool _syncProne;
    [SyncVar] private bool _syncAiming;
    [SyncVar] private bool _syncFiring;
    [SyncVar] private bool _syncJumping;
    [SyncVar] private bool _syncGrounded;
    [SyncVar] private bool _syncItemEquipped;
    [SyncVar] private Vector3 _syncLookPosition;

    // ════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        _juController = GetComponent<JUCharacterController>();
        _animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();
        _juHealth = GetComponent<JUHealth>();

        SetupReflection();
    }

    private void Start()
    {
        // Remote players should NOT use default keyboard/mouse input
        // Network will control their movement instead
        if (!isLocalPlayer)
        {
            _juController.UseDefaultControllerInput = false;
        }
    }

    private void Update()
    {
        if (isLocalPlayer)
            ReadAndSendInputToServer();
        else
            ApplyReceivedDataToRemote();
    }

    // ════════════════════════════════════════════════════════
    // REFLECTION SETUP
    // ════════════════════════════════════════════════════════

    private void SetupReflection()
    {
        // JUCharacterBrain is the parent class of JUCharacterController
        // It holds the horizontal/vertical input values privately
        var brainType = typeof(JUTPS.CharacterBrain.JUCharacterBrain);

        // BindingFlags tells Reflection WHERE to look:
        // Instance = on an object instance (not static)
        // NonPublic = private and protected fields
        // Public = public fields (just in case)
        var flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        _horizontalField = brainType.GetField("HorizontalX", flags);
        _verticalField = brainType.GetField("VerticalY", flags);

        // Warn us if JU TPS changed these field names in a future update
        if (_horizontalField == null)
            Debug.LogError("[PlayerMovementSync] 'HorizontalX' field not found in JUCharacterBrain. " +
                           "Check if JU TPS was updated and the field was renamed.");
        if (_verticalField == null)
            Debug.LogError("[PlayerMovementSync] 'VerticalY' field not found in JUCharacterBrain. " +
                           "Check if JU TPS was updated and the field was renamed.");
    }

    // ════════════════════════════════════════════════════════
    // LOCAL PLAYER — Read Input and Send to Server
    // ════════════════════════════════════════════════════════

    private void ReadAndSendInputToServer()
    {
        // Safety: can't read if reflection failed or player is missing
        if (_horizontalField == null || _verticalField == null) return;
        if (_juController == null) return;

        // Read current input values using reflection
        float h = (float)_horizontalField.GetValue(_juController);
        float v = (float)_verticalField.GetValue(_juController);

        // Send ALL movement states to the server in one Command call
        // [Command] = "run this on the SERVER, called from the CLIENT"
        CmdSendMovement(
            h, v,
            _juController.IsRunning,
            _juController.IsCrouched,
            _juController.IsProne,
            _juController.IsAiming,
            _juController.FiringMode,
            _juController.IsJumping,
            _juController.IsGrounded,
            _juController.IsItemEquiped,
            _juController.GetLookPosition()
        );

        // Also send one-time action inputs (punch, roll, reload)
        HandleCombatInputs();
    }

    private void HandleCombatInputs()
    {
        // Dead players can't do actions
        if (_juHealth != null && _juHealth.IsDead) return;

        // Punch (only when no weapon is equipped)
        if (JUInput.GetButtonDown(JUInput.Buttons.ShotButton) && !_juController.IsItemEquiped)
            CmdTriggerAnimation("Punch");

        // Roll
        if (JUInput.GetButtonDown(JUInput.Buttons.RollButton))
            CmdTriggerAnimation("Roll");

        // Reload
        if (JUInput.GetButtonDown(JUInput.Buttons.ReloadButton))
            CmdTriggerAnimation("ReloadRightWeapon");
    }

    // ════════════════════════════════════════════════════════
    // REMOTE PLAYER — Apply Received Data
    // ════════════════════════════════════════════════════════

    private void ApplyReceivedDataToRemote()
    {
        if (_horizontalField == null || _verticalField == null) return;
        if (_juController == null) return;

        // Write the synced values into the JU TPS controller
        _horizontalField.SetValue(_juController, _syncH);
        _verticalField.SetValue(_juController, _syncV);

        _juController.IsRunning = _syncRunning;
        _juController.IsCrouched = _syncCrouched;
        _juController.IsProne = _syncProne;
        _juController.IsAiming = _syncAiming;
        _juController.FiringMode = _syncFiring;
        _juController.IsJumping = _syncJumping;
        _juController.IsGrounded = _syncGrounded;
        _juController.IsItemEquiped = _syncItemEquipped;
        _juController.LookAtPosition = _syncLookPosition;

        // Sync death animation (PlayerHealthSync updates _juHealth.IsDead on all clients)
        if (_animator != null && _juHealth != null)
            _animator.SetBool("Die", _juHealth.IsDead);
    }

    // ════════════════════════════════════════════════════════
    // COMMANDS (Client → Server)
    // ════════════════════════════════════════════════════════

    [Command]
    private void CmdSendMovement(
        float h, float v,
        bool running, bool crouched, bool prone,
        bool aiming, bool firing,
        bool jumping, bool grounded,
        bool itemEquipped, Vector3 lookPos)
    {
        // Server stores these values — SyncVar auto-sends to all clients
        _syncH = h;
        _syncV = v;
        _syncRunning = running;
        _syncCrouched = crouched;
        _syncProne = prone;
        _syncAiming = aiming;
        _syncFiring = firing;
        _syncJumping = jumping;
        _syncGrounded = grounded;
        _syncItemEquipped = itemEquipped;
        _syncLookPosition = lookPos;
    }

    [Command]
    private void CmdTriggerAnimation(string triggerName)
    {
        // Server tells ALL clients to play this animation trigger
        RpcPlayAnimationTrigger(triggerName);
    }

    // ════════════════════════════════════════════════════════
    // CLIENT RPCs (Server → All Clients)
    // ════════════════════════════════════════════════════════

    [ClientRpc]
    private void RpcPlayAnimationTrigger(string triggerName)
    {
        // Local player's animation plays naturally from their own input
        // We only need to play it on REMOTE player representations
        if (isLocalPlayer) return;

        if (_animator != null)
            _animator.SetTrigger(triggerName);
    }
}