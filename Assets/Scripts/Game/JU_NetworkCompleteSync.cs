using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.JUInputSystem;
using JUTPS.ItemSystem;
using System.Reflection; // Reflection zaroori hai
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
    // ItemSwitchManager hata diya, hum reflection use karenge

    [Header("Sync Data")]
    // --- MOVEMENT STATES ---
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

    // --- WEAPON & DEATH ---
    [SyncVar(hook = nameof(OnWeaponChanged))] int syncWeaponID = -1;
    [SyncVar(hook = nameof(OnDeathChanged))] bool syncIsDead;

    // --- REFLECTION FIELDS (Access Private/Hidden Variables) ---
    private FieldInfo horizontalField;
    private FieldInfo verticalField;
    private FieldInfo itemsField; // Weapon List access karne ke liye
    private MonoBehaviour itemSwitchManagerScript; // Generic reference

    void Awake()
    {
        juController = GetComponent<JUCharacterController>();
        _animator = GetComponent<Animator>();
        juHealth = GetComponent<JUHealth>();
        itemSwitchManagerScript = GetComponent<ItemSwitchManager>();

        // 1. Setup Reflection for Movement
        var brainType = typeof(JUTPS.CharacterBrain.JUCharacterBrain);
        horizontalField = brainType.GetField("HorizontalX", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        verticalField = brainType.GetField("VerticalY", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

        // 2. Setup Reflection for Items List
        // Hum "Items" dhoond rahe hain jo ItemSwitchManager mein hota hai
        if (itemSwitchManagerScript != null)
        {
            var managerType = itemSwitchManagerScript.GetType();
            itemsField = managerType.GetField("Items", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // Fallback agar "Items" naam na ho (JU TPS ke purane versions ke liye)
            if (itemsField == null) itemsField = managerType.GetField("HoldableItems", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }
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
            // --- LOCAL: Read Input & Send to Server ---
            float h = (float)horizontalField.GetValue(juController);
            float v = (float)verticalField.GetValue(juController);

            CmdSendMovementState(
                h, v,
                juController.IsRunning,
                juController.IsCrouched,
                juController.IsProne,
                juController.IsAiming,
                juController.FiringMode,
                juController.IsJumping,
                juController.IsGrounded,
                juController.IsItemEquiped,
                juController.GetLookPosition(),
                juHealth.IsDead
            );

            HandleInputActions();
        }
        else
        {
            // --- REMOTE: Apply Data from Server ---
            ApplyRemoteMovement();
        }
    }

    void HandleInputActions()
    {
        if (juHealth.IsDead) return;

        // Punch
        if (JUInput.GetButtonDown(JUInput.Buttons.ShotButton) && !juController.IsItemEquiped)
            CmdPlayTrigger("Punch");

        // Roll
        if (JUInput.GetButtonDown(JUInput.Buttons.RollButton))
            CmdPlayTrigger("Roll");

        // Reload
        if (JUInput.GetButtonDown(JUInput.Buttons.ReloadButton))
            CmdPlayTrigger("ReloadRightWeapon");

        // Weapon Switch
        if (JUInput.GetButtonDown(JUInput.Buttons.NextWeaponButton) || JUInput.GetButtonDown(JUInput.Buttons.PreviousWeaponButton))
        {
            Invoke(nameof(CheckLocalWeaponID), 0.1f);
        }
    }

    void CheckLocalWeaponID()
    {
        // UI Crash se bachne ke liye direct inventory se ID le rahe hain
        if (juController.Inventory != null)
        {
            CmdChangeWeapon(juController.Inventory.CurrentRightHandItemID);
        }
    }

    void ApplyRemoteMovement()
    {
        horizontalField.SetValue(juController, syncHorizontal);
        verticalField.SetValue(juController, syncVertical);

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

    // =========================================================
    // COMMANDS
    // =========================================================

    [Command]
    void CmdSendMovementState(float h, float v, bool run, bool crouch, bool prone, bool aim, bool fire, bool jump, bool ground, bool equip, Vector3 look, bool isDead)
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

    [Command]
    void CmdPlayTrigger(string triggerName) { RpcPlayTrigger(triggerName); }

    [Command]
    void CmdChangeWeapon(int newItemIndex) { syncWeaponID = newItemIndex; }

    // =========================================================
    // RPCS & HOOKS
    // =========================================================

    [ClientRpc]
    void RpcPlayTrigger(string triggerName)
    {
        if (isLocalPlayer) return;
        if (_animator != null) _animator.SetTrigger(triggerName);
    }

    // 🛑 CRITICAL FIX: Safe Weapon Switch 🛑
    void OnWeaponChanged(int oldID, int newID)
    {
        if (isLocalPlayer) return;

        // Agar list abhi initialize nahi hui (Deserialize error), thoda wait karo
        if (itemsField == null || itemSwitchManagerScript == null)
        {
            StartCoroutine(RetryWeaponSwitch(newID));
            return;
        }

        // Manual Switch Try karo
        if (!TryManualSwitch(newID))
        {
            // Agar abhi fail hua, to shayad "Start" function run nahi hua, retry karo
            StartCoroutine(RetryWeaponSwitch(newID));
        }
    }

    IEnumerator RetryWeaponSwitch(int id)
    {
        yield return new WaitForSeconds(0.2f); // Thoda wait taake scripts load ho jayen
        TryManualSwitch(id);
    }

    bool TryManualSwitch(int id)
    {
        try
        {
            // Reflection ke zariye "Items" list nikalo
            var itemsList = itemsField.GetValue(itemSwitchManagerScript) as IList;

            if (itemsList == null || itemsList.Count == 0) return false;

            // Loop chala kar sahi weapon ON aur baaki OFF karo
            for (int i = 0; i < itemsList.Count; i++)
            {
                // List ke items 'JUHoldableItem' hain, unka .gameObject access karo
                object itemObj = itemsList[i];
                if (itemObj == null) continue;

                // Reflection se 'gameObject' property dhoondo (Generic approach)
                PropertyInfo goProp = itemObj.GetType().GetProperty("gameObject");
                if (goProp != null)
                {
                    GameObject go = goProp.GetValue(itemObj) as GameObject;
                    if (go != null)
                    {
                        go.SetActive(i == id); // Sirf match hone wala ON, baaki OFF
                    }
                }
            }
            return true; // Success
        }
        catch (System.Exception e)
        {
            // Agar koi error aye to ignore karo, crash mat hone do
            Debug.LogWarning("Safe Weapon Switch Error (Ignored): " + e.Message);
            return false;
        }
    }

    void OnDeathChanged(bool oldState, bool newState)
    {
        if (_animator) _animator.SetBool("Die", newState);
    }
}