using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.JUInputSystem;

[RequireComponent(typeof(JUCharacterController))]
[RequireComponent(typeof(NetworkAnimator))]
public class NetworkCompleteSync : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private JUCharacterController juController;
    [SerializeField] private Animator _animator;
    [SerializeField] private JUHealth juHealth;

    // Avoid hardcoding strings throughout the code. Centralizing them here prevents typos.
    private const string ANIM_DIE = "Die";
    private const string TRIG_PUNCH = "Punch";
    private const string TRIG_ROLL = "Roll";
    private const string TRIG_RELOAD_RIGHT = "ReloadRightWeapon";

    // --- NETWORK DATA STRUCTURES ---
    // Packing state into a struct is highly optimized for Mirror. 
    // It sends all these booleans and floats in a single clean packet.
    public struct CharacterSyncState
    {
        public float horizontal;
        public float vertical;
        public bool isRunning;
        public bool isCrouched;
        public bool isProne;
        public bool isAiming;
        public bool isFiringMode;
        public bool isJumping;
        public bool isGrounded;
        public bool isItemEquipped;
        public bool isDead;

        // Helper method to check if the state has changed enough to warrant a network update
        public bool HasChanged(CharacterSyncState other)
        {
            return horizontal != other.horizontal ||
                   vertical != other.vertical ||
                   isRunning != other.isRunning ||
                   isCrouched != other.isCrouched ||
                   isProne != other.isProne ||
                   isAiming != other.isAiming ||
                   isFiringMode != other.isFiringMode ||
                   isJumping != other.isJumping ||
                   isGrounded != other.isGrounded ||
                   isItemEquipped != other.isItemEquipped ||
                   isDead != other.isDead;
        }
    }

    [Header("Network Synchronized State")]
    [SyncVar] private CharacterSyncState netState;
    [SyncVar] private Vector3 netLookPosition;

    [SyncVar(hook = nameof(OnWeaponChanged))]
    private int netWeaponID = -1;

    // --- LOCAL STATE TRACKERS ---
    // Used to prevent network spam. We only send updates when these change.
    private CharacterSyncState lastSentState;
    private Vector3 lastSentLookPos;

    void Awake()
    {
        // Safety check: ensure components are assigned, or grab them automatically
        if (juController == null) juController = GetComponent<JUCharacterController>();
        if (_animator == null) _animator = GetComponent<Animator>();
        if (juHealth == null) juHealth = GetComponent<JUHealth>();
    }

    void Start()
    {
        if (!isLocalPlayer)
        {
            // Disable local input processing for remote characters
            juController.UseDefaultControllerInput = false;
            if (TryGetComponent(out Rigidbody rb)) rb.isKinematic = true;
        }
    }

    void Update()
    {
        if (isLocalPlayer)
        {
            ProcessLocalPlayer();
        }
        else
        {
            ProcessRemotePlayer();
        }
    }

    // =========================================================
    // LOCAL PLAYER LOGIC (Authoritative Input)
    // =========================================================

    private void ProcessLocalPlayer()
    {
        // 1. Gather current state directly from Input and JUTPS public properties (NO REFLECTION)
        CharacterSyncState currentState = new CharacterSyncState
        {
            horizontal = JUInput.GetAxis(JUInput.Axis.MoveHorizontal),
            vertical = JUInput.GetAxis(JUInput.Axis.MoveVertical),
            isRunning = juController.IsRunning,
            isCrouched = juController.IsCrouched,
            isProne = juController.IsProne,
            isAiming = juController.IsAiming,
            isFiringMode = juController.FiringMode,
            isJumping = juController.IsJumping,
            isGrounded = juController.IsGrounded,
            isItemEquipped = juController.IsItemEquiped,
            isDead = juHealth.IsDead
        };

        Vector3 currentLookPos = juController.GetLookPosition();

        // 2. Prevent Network Spam: Only send Command if state changed, or if look position moved significantly
        bool stateChanged = currentState.HasChanged(lastSentState);
        bool lookChanged = Vector3.Distance(currentLookPos, lastSentLookPos) > 0.1f;

        if (stateChanged || lookChanged)
        {
            CmdUpdateMovementState(currentState, currentLookPos);

            // Update trackers
            lastSentState = currentState;
            lastSentLookPos = currentLookPos;
        }

        // 3. Handle Actions (Punch, Reload, Roll, Weapon Switch)
        HandleLocalInputActions();
    }

    private void HandleLocalInputActions()
    {
        if (juHealth.IsDead) return;

        // Triggers
        if (JUInput.GetButtonDown(JUInput.Buttons.ShotButton) && !juController.IsItemEquiped)
            CmdPlayTrigger(TRIG_PUNCH);

        if (JUInput.GetButtonDown(JUInput.Buttons.RollButton))
            CmdPlayTrigger(TRIG_ROLL);

        if (JUInput.GetButtonDown(JUInput.Buttons.ReloadButton))
            CmdPlayTrigger(TRIG_RELOAD_RIGHT);

        // Safe Weapon Switch Check
        if (juController.Inventory != null)
        {
            int currentLocalWeapon = juController.Inventory.CurrentRightHandItemID;
            if (currentLocalWeapon != netWeaponID)
            {
                CmdChangeWeapon(currentLocalWeapon);
            }
        }
    }

    // =========================================================
    // REMOTE PLAYER LOGIC (Apply Synced Data)
    // =========================================================

    private void ProcessRemotePlayer()
    {
        // Utilize JUTPS built-in public method to apply movement inputs securely
        juController.SetMoveInput(netState.horizontal, netState.vertical);

        juController.IsRunning = netState.isRunning;
        juController.IsCrouched = netState.isCrouched;
        juController.IsProne = netState.isProne;
        juController.IsAiming = netState.isAiming;
        juController.FiringMode = netState.isFiringMode;
        juController.IsJumping = netState.isJumping;
        juController.IsGrounded = netState.isGrounded;
        juController.IsItemEquiped = netState.isItemEquipped;

        juController.LookAtPosition = netLookPosition;

        if (_animator != null && _animator.GetBool(ANIM_DIE) != netState.isDead)
        {
            _animator.SetBool(ANIM_DIE, netState.isDead);
        }
    }

    // =========================================================
    // COMMANDS (Client -> Server)
    // =========================================================

    [Command]
    private void CmdUpdateMovementState(CharacterSyncState newState, Vector3 newLookPosition)
    {
        netState = newState;
        netLookPosition = newLookPosition;
    }

    [Command]
    private void CmdPlayTrigger(string triggerName)
    {
        RpcPlayTrigger(triggerName);
    }

    [Command]
    private void CmdChangeWeapon(int newWeaponID)
    {
        netWeaponID = newWeaponID;
    }

    // =========================================================
    // RPCS & HOOKS (Server -> Client)
    // =========================================================

    [ClientRpc]
    private void RpcPlayTrigger(string triggerName)
    {
        if (isLocalPlayer) return;
        if (_animator != null) _animator.SetTrigger(triggerName);
    }

    /// <summary>
    /// Safely toggles weapon meshes on remote clients without using Reflection.
    /// Utilizes the public HoldableItensRightHand array provided by JUTPS JUInventory.
    /// </summary>
    private void OnWeaponChanged(int oldID, int newID)
    {
        if (isLocalPlayer) return;

        if (juController.Inventory == null || juController.Inventory.HoldableItensRightHand == null)
        {
            Debug.LogWarning("NetworkCompleteSync: Inventory not initialized yet on remote client.");
            return;
        }

        var rightHandItems = juController.Inventory.HoldableItensRightHand;

        for (int i = 0; i < rightHandItems.Length; i++)
        {
            if (rightHandItems[i] != null && rightHandItems[i].gameObject != null)
            {
                // Only activate the object if its index matches the requested weapon ID
                rightHandItems[i].gameObject.SetActive(i == newID);
            }
        }
    }
}