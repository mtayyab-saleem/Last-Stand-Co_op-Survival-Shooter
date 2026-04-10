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

    private const string ANIM_DIE = "Die";
    private const string TRIG_PUNCH = "Punch";
    private const string TRIG_ROLL = "Roll";
    private const string TRIG_RELOAD_RIGHT = "ReloadRightWeapon";

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

        // check if the state has changed enough for network update
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

    private CharacterSyncState lastSentState;
    private Vector3 lastSentLookPos;

    void Awake()
    {
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

        // send Command if state changed, or if look position moved significantly
        bool stateChanged = currentState.HasChanged(lastSentState);
        bool lookChanged = Vector3.Distance(currentLookPos, lastSentLookPos) > 0.1f;

        if (stateChanged || lookChanged)
        {
            CmdUpdateMovementState(currentState, currentLookPos);

            // Update trackers
            lastSentState = currentState;
            lastSentLookPos = currentLookPos;
        }

        // handle Actions (Punch, Reload, Roll, Weapon Switch)
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

        // Weapon Switch Check
        if (juController.Inventory != null)
        {
            int currentLocalWeapon = juController.Inventory.CurrentRightHandItemID;
            if (currentLocalWeapon != netWeaponID)
            {
                CmdChangeWeapon(currentLocalWeapon);
            }
        }
    }

    private void ProcessRemotePlayer()
    {
        // JUTPS built-in method to apply movement inputs 
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


    [ClientRpc]
    private void RpcPlayTrigger(string triggerName)
    {
        if (isLocalPlayer) return;
        if (_animator != null) _animator.SetTrigger(triggerName);
    }

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
                rightHandItems[i].gameObject.SetActive(i == newID);
            }
        }
    }
}