using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.JUInputSystem;
using JUTPS.ItemSystem;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(JUCharacterController))]
[RequireComponent(typeof(NetworkAnimator))]
public class JU_NetworkCompleteSync : NetworkBehaviour
{
    [Header("References")]
    public JUCharacterController juController;
    public Animator _animator;
    public JUHealth juHealth;

    // --- SYNCVARS ---
    [SyncVar] float syncHorizontal;
    [SyncVar] float syncVertical;
    [SyncVar] bool syncIsRunning;
    [SyncVar] bool syncIsCrouched;
    [SyncVar] bool syncIsProne;
    [SyncVar] bool syncIsAiming;
    [SyncVar] bool syncFiringMode;
    [SyncVar] bool syncIsJumping;
    [SyncVar] bool syncIsGrounded;
    [SyncVar] bool syncItemEquiped;
    [SyncVar] Vector3 syncLookPosition;

    [SyncVar(hook = nameof(OnWeaponChanged))] int syncWeaponID = -1;
    [SyncVar(hook = nameof(OnDeathChanged))] bool syncIsDead;

    private ItemSwitchManager itemSwitchManager;

    void Awake()
    {
        juController = GetComponent<JUCharacterController>();
        _animator = GetComponent<Animator>();
        juHealth = GetComponent<JUHealth>();
        itemSwitchManager = GetComponent<ItemSwitchManager>();
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            juController.UseDefaultControllerInput = false;
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            SendMovementToServer();
            HandleInputActions();
        }
        else
        {
            ApplyRemoteMovement();
        }
    }

    void SendMovementToServer()
    {
        float h = juController.HorizontalX;
        float v = juController.VerticalY;

        CmdSendMovementState(h, v,
            juController.IsRunning, juController.IsCrouched, juController.IsProne,
            juController.IsAiming, juController.FiringMode, juController.IsJumping,
            juController.IsGrounded, juController.IsItemEquiped,
            juController.GetLookPosition(), juHealth.IsDead);
    }

    void HandleInputActions()
    {
        if (juHealth.IsDead) return;

        if (JUInput.GetButtonDown(JUInput.Buttons.ShotButton) && !juController.IsItemEquiped)
            CmdPlayTrigger("Punch");

        if (JUInput.GetButtonDown(JUInput.Buttons.RollButton))
            CmdPlayTrigger("Roll");

        if (JUInput.GetButtonDown(JUInput.Buttons.ReloadButton))
            CmdPlayTrigger("ReloadRightWeapon");

        if (JUInput.GetButtonDown(JUInput.Buttons.NextWeaponButton) ||
            JUInput.GetButtonDown(JUInput.Buttons.PreviousWeaponButton))
            Invoke(nameof(SendWeaponID), 0.1f);
    }

    void SendWeaponID()
    {
        if (juController.Inventory != null)
            CmdChangeWeapon(juController.Inventory.CurrentRightHandItemID);
    }

    void ApplyRemoteMovement()
    {
        juController.HorizontalX = syncHorizontal;
        juController.VerticalY = syncVertical;
        juController.IsRunning = syncIsRunning;
        juController.IsCrouched = syncIsCrouched;
        juController.IsProne = syncIsProne;
        juController.IsAiming = syncIsAiming;
        juController.FiringMode = syncFiringMode;
        juController.IsJumping = syncIsJumping;
        juController.IsGrounded = syncIsGrounded;
        juController.IsItemEquiped = syncItemEquiped;
        juController.LookAtPosition = syncLookPosition;

        if (_animator != null) _animator.SetBool("Die", syncIsDead);
    }

    // --- COMMANDS ---

    [Command]
    void CmdSendMovementState(float h, float v, bool run, bool crouch, bool prone,
        bool aim, bool fire, bool jump, bool ground, bool equip, Vector3 look, bool isDead)
    {
        syncHorizontal = h;
        syncVertical = v;
        syncIsRunning = run;
        syncIsCrouched = crouch;
        syncIsProne = prone;
        syncIsAiming = aim;
        syncFiringMode = fire;
        syncIsJumping = jump;
        syncIsGrounded = ground;
        syncItemEquiped = equip;
        syncLookPosition = look;
        syncIsDead = isDead;
    }

    [Command] void CmdPlayTrigger(string name) => RpcPlayTrigger(name);
    [Command] void CmdChangeWeapon(int id) => syncWeaponID = id;

    // --- RPCS ---

    [ClientRpc]
    void RpcPlayTrigger(string name)
    {
        if (isLocalPlayer) return;
        if (_animator != null) _animator.SetTrigger(name);
    }

    // --- SYNCVAR HOOKS ---

    void OnWeaponChanged(int oldID, int newID)
    {
        if (isLocalPlayer) return;
        if (itemSwitchManager != null)
            itemSwitchManager.SwitchToItem(newID);
    }

    void OnDeathChanged(bool oldState, bool newState)
    {
        if (_animator != null) _animator.SetBool("Die", newState);
    }
}