using UnityEngine;
using Mirror;
using JUTPS;
using JUTPS.JUInputSystem;
using JUTPS.ItemSystem; // Required for JUHoldableItem

[RequireComponent(typeof(JUCharacterController))]
[RequireComponent(typeof(NetworkAnimator))]
public class NetworkCompleteSync : NetworkBehaviour
{
    [Header("Core References")]
    [SerializeField] private JUCharacterController juController;
    [SerializeField] private Animator characterAnimator;
    [SerializeField] private JUHealth juHealth;

    // String literals converted to constants to avoid allocation and typos
    private const string ANIM_DIE = "Die";
    private const string TRIG_PUNCH = "Punch";
    private const string TRIG_ROLL = "Roll";
    private const string TRIG_RELOAD_RIGHT = "ReloadRightWeapon";

    /// <summary>
    /// Struct to efficiently synchronize character state across the network.
    /// </summary>
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

        // Evaluates if the state has changed enough to warrant a network update
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
    [SyncVar] private float netHorizontal;
    [SyncVar] private float netVertical;
    [SyncVar] private bool netIsRunning;
    [SyncVar] private bool netIsCrouched;
    [SyncVar] private bool netIsProne;
    [SyncVar] private bool netIsAiming;
    [SyncVar] private bool netIsFiringMode;
    [SyncVar] private bool netIsJumping;
    [SyncVar] private bool netIsGrounded;
    [SyncVar] private bool netIsItemEquipped;
    [SyncVar] private bool netIsDead;
    [SyncVar] private Vector3 netLookPosition;

    // Dual-wield weapon synchronization hooks
    [SyncVar(hook = nameof(OnRightWeaponChanged))] private int netRightWeaponID = -1;
    [SyncVar(hook = nameof(OnLeftWeaponChanged))] private int netLeftWeaponID = -1;

    private CharacterSyncState lastSentState;
    private Vector3 lastSentLookPos;

    private float lastStateSyncTime;
    private const float SYNC_INTERVAL = 0.05f; // 20 updates per second

    private void Awake()
    {
        if (juController == null) juController = GetComponent<JUCharacterController>();
        if (characterAnimator == null) characterAnimator = GetComponent<Animator>();
        if (juHealth == null) juHealth = GetComponent<JUHealth>();
    }

    private void Start()
    {
        if (!isLocalPlayer)
        {
            // Disable local input processing for remote avatars to prevent interference
            juController.UseDefaultControllerInput = false;

            if (TryGetComponent(out Rigidbody rb))
            {
                rb.isKinematic = true;
            }
        }
    }

    private void Update()
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

    private void ProcessLocalPlayer()
    {
        // Gather current state directly from Input and JUTPS public properties
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

        // Send Command only if the core state changed or look position moved significantly (optimization)
        bool stateChanged = currentState.HasChanged(lastSentState);
        bool lookChanged = Vector3.Distance(currentLookPos, lastSentLookPos) > 0.1f;

        if ((stateChanged || lookChanged) && Time.time - lastStateSyncTime >= SYNC_INTERVAL)
        {
            CmdUpdateMovementState(currentState, currentLookPos);

            lastSentState = currentState;
            lastSentLookPos = currentLookPos;
            lastStateSyncTime = Time.time;
        }

        HandleLocalInputActions();
    }

    private void HandleLocalInputActions()
    {
        if (juHealth.IsDead) return;

        // Synchronize one-off animation triggers
        if (JUInput.GetButtonDown(JUInput.Buttons.ShotButton) && !juController.IsItemEquiped)
            CmdPlayTrigger(TRIG_PUNCH);

        if (JUInput.GetButtonDown(JUInput.Buttons.RollButton))
            CmdPlayTrigger(TRIG_ROLL);

        if (JUInput.GetButtonDown(JUInput.Buttons.ReloadButton))
            CmdPlayTrigger(TRIG_RELOAD_RIGHT);

        // Synchronize active weapons for both hands
        if (juController.Inventory != null)
        {
            int currentLocalRightWeapon = juController.Inventory.CurrentRightHandItemID;
            int currentLocalLeftWeapon = juController.Inventory.CurrentLeftHandItemID;

            if (currentLocalRightWeapon != netRightWeaponID || currentLocalLeftWeapon != netLeftWeaponID)
            {
                CmdChangeWeapons(currentLocalRightWeapon, currentLocalLeftWeapon);
            }
        }
    }

    private void ProcessRemotePlayer()
    {
        // Apply synchronized state to the remote avatar using JUTPS built-in methods
        juController.SetMoveInput(netHorizontal, netVertical);

        juController.IsRunning = netIsRunning;
        juController.IsCrouched = netIsCrouched;
        juController.IsProne = netIsProne;
        juController.IsAiming = netIsAiming;
        juController.FiringMode = netIsFiringMode;
        juController.IsJumping = netIsJumping;
        juController.IsGrounded = netIsGrounded;
        juController.IsItemEquiped = netIsItemEquipped;
        juController.LookAtPosition = netLookPosition;

        if (characterAnimator != null && characterAnimator.GetBool(ANIM_DIE) != netIsDead)
        {
            characterAnimator.SetBool(ANIM_DIE, netIsDead);
        }
    }

    [Command(channel = Channels.Unreliable)]
    private void CmdUpdateMovementState(CharacterSyncState newState, Vector3 newLookPosition)
    {
        netHorizontal = newState.horizontal;
        netVertical = newState.vertical;
        netIsRunning = newState.isRunning;
        netIsCrouched = newState.isCrouched;
        netIsProne = newState.isProne;
        netIsAiming = newState.isAiming;
        netIsFiringMode = newState.isFiringMode;
        netIsJumping = newState.isJumping;
        netIsGrounded = newState.isGrounded;
        netIsItemEquipped = newState.isItemEquipped;
        netIsDead = newState.isDead;

        netLookPosition = newLookPosition;
    }

    [Command]
    private void CmdPlayTrigger(string triggerName)
    {
        RpcPlayTrigger(triggerName);
    }

    [Command]
    private void CmdChangeWeapons(int newRightWeaponID, int newLeftWeaponID)
    {
        netRightWeaponID = newRightWeaponID;
        netLeftWeaponID = newLeftWeaponID;
    }

    [ClientRpc]
    private void RpcPlayTrigger(string triggerName)
    {
        if (isLocalPlayer) return;
        if (characterAnimator != null) characterAnimator.SetTrigger(triggerName);
    }

    #region Weapon Synchronization Hooks

    private void OnRightWeaponChanged(int oldID, int newID)
    {
        if (isLocalPlayer || juController.Inventory == null) return;
        UpdateWeaponVisibility(juController.Inventory.HoldableItensRightHand, newID);
    }

    private void OnLeftWeaponChanged(int oldID, int newID)
    {
        if (isLocalPlayer || juController.Inventory == null) return;
        UpdateWeaponVisibility(juController.Inventory.HoldableItensLeftHand, newID);
    }

    /// <summary>
    /// Safely enables the active weapon and disables all others in the provided item array.
    /// </summary>
    private void UpdateWeaponVisibility(JUHoldableItem[] items, int activeWeaponID)
    {
        if (items == null) return;

        for (int i = 0; i < items.Length; i++)
        {
            if (items[i] != null && items[i].gameObject != null)
            {
                items[i].gameObject.SetActive(i == activeWeaponID);
            }
        }
    }

    #endregion
}