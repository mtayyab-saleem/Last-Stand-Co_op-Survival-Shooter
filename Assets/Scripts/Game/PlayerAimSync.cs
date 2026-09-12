using JUTPS;
using JUTPS.ItemSystem;
using Mirror;
using UnityEngine;

/// <summary>
/// Replicates a player's aim and hand pose to everyone else.
///
/// JUTPS puts all of this in JUCharacterController: it rotates the weapon pivot from
/// the local camera in RefreshItemAimRotationPivot, moves the hand IK targets onto the
/// weapon grips in WieldingIKWeightController, and applies the result in OnAnimatorIK.
/// PlayerNetworkSetup disables that controller on remote players, so none of it ran for
/// them - every remote gun pointed straight ahead and both hands stayed in the raw
/// animation pose instead of on the weapon.
///
/// This component redoes only those three things for remote players, through JUTPS's own
/// public helpers, so the pose matches what the owner sees. The pivot and the IK targets
/// are both handled here in Update, because the grip transforms are children of the
/// pivot - splitting them across Update and LateUpdate would leave the hands a frame
/// behind the gun. That is also exactly where JUTPS does it locally.
/// </summary>
[DisallowMultipleComponent]
public class PlayerAimSync : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Resolved automatically when left empty.")]
    [SerializeField] private JUCharacterController character;

    [Header("Sending")]
    [Tooltip("Degrees of change before a new aim value is published.")]
    [SerializeField] private float sendThreshold = 1.25f;

    [Header("Receiving")]
    [Tooltip("How quickly a remote gun catches up to the synced aim. Higher is snappier.")]
    [SerializeField] private float smoothing = 18f;

    [Tooltip("How fast a remote player's hand IK fades in and out. JUTPS uses 15 locally.")]
    [SerializeField] private float ikTransitionSpeed = 15f;

    /// <summary>JUTPS holds the arms down for this long after a weapon switch.</summary>
    private const float WeaponSwitchDuration = 0.5f;

    // Written by the owner; this component must be set to ClientToServer, which is
    // the same authority model NetworkTransformReliable already uses here.
    [SyncVar] private float aimPitch;
    [SyncVar] private float aimYaw;
    [SyncVar] private bool aiming;

    private float lastSentPitch;
    private float lastSentYaw;
    private bool lastSentAiming;
    private bool hasSentOnce;

    private float remoteSwitchTimer;

    // The controller's LookWeightIK is protected, so remote players keep their own.
    private float lookWeightIK;

    /// <summary>Where this player is pointing, usable by UI or server checks.</summary>
    public Quaternion AimRotation => Quaternion.Euler(aimPitch, aimYaw, 0f);
    public bool IsAiming => aiming;

    private void Awake()
    {
        if (character == null)
            character = GetComponent<JUCharacterController>();
    }

    // Remote work happens in Update so it lands before the animator's IK pass, which is
    // what calls OnAnimatorIK below. HumanoidSpine is therefore one frame stale here -
    // as it is in JUTPS itself, which also reads it from Update for the local player.
    private void Update()
    {
        if (isOwned || character == null)
            return;

        ApplyRemoteAim();
        DriveRemoteWieldingIK();
    }

    // The owner publishes after its camera has settled for the frame.
    private void LateUpdate()
    {
        if (isOwned && character != null)
            PublishOwnerAim();
    }

    private void PublishOwnerAim()
    {
        Vector3 look = character.GetLookDirectionEulerAngles();
        bool nowAiming = character.IsAiming || character.FiringMode;

        bool moved =
            !hasSentOnce ||
            nowAiming != lastSentAiming ||
            Mathf.Abs(Mathf.DeltaAngle(look.x, lastSentPitch)) >= sendThreshold ||
            Mathf.Abs(Mathf.DeltaAngle(look.y, lastSentYaw)) >= sendThreshold;

        if (!moved)
            return;

        aimPitch = look.x;
        aimYaw = look.y;
        aiming = nowAiming;

        lastSentPitch = look.x;
        lastSentYaw = look.y;
        lastSentAiming = nowAiming;
        hasSentOnce = true;
    }

    private void ApplyRemoteAim()
    {
        GameObject pivotObject = character.PivotItemRotation;

        if (pivotObject == null)
            return;

        Transform pivot = pivotObject.transform;

        // JUCharacterController is disabled on remote players, so the pivot has to be
        // kept on the spine here or the weapon drifts away from the body.
        if (character.HumanoidSpine != null)
            pivot.position = character.HumanoidSpine.position;

        // Not aiming: only the body yaw matters, matching JUTPS's own non-aiming path
        // which lerps the pivot back towards the character's own rotation.
        Quaternion target = aiming
            ? Quaternion.Euler(aimPitch, aimYaw, 0f)
            : Quaternion.Euler(0f, transform.eulerAngles.y, 0f);

        // Exponential smoothing, so it is frame-rate independent and hides the gaps
        // between network updates without lagging noticeably behind.
        float t = 1f - Mathf.Exp(-Mathf.Max(0.01f, smoothing) * Time.deltaTime);
        pivot.rotation = Quaternion.Slerp(pivot.rotation, target, t);
    }

    /// <summary>
    /// Remote stand-in for JUCharacterController.WieldingIKWeightController, which is
    /// private. Moves the hand IK targets onto the held weapon and blends the weights.
    /// </summary>
    private void DriveRemoteWieldingIK()
    {
        // The controller's own Update clears this after half a second. It is disabled
        // here, so without this a single weapon switch would pin the hands at zero
        // weight for the rest of the match.
        if (character.IsWeaponSwitching)
        {
            remoteSwitchTimer += Time.deltaTime;

            if (remoteSwitchTimer >= WeaponSwitchDuration)
            {
                character.IsWeaponSwitching = false;
                remoteSwitchTimer = 0f;
            }
        }
        else
        {
            remoteSwitchTimer = 0f;
        }

        // FiringModeIK exists to hold the arms down until the wield animation settles;
        // the timer above covers that here, so it just tracks the synced fire mode.
        character.FiringModeIK = character.FiringMode && !character.IsWeaponSwitching;

        // Puts the IK targets on the weapon grip transforms. Without this the targets
        // sit where they were spawned and the hands never reach the gun at all.
        if (character.IsItemEquiped)
        {
            character.SmoothLeftHandPosition(25f);
            character.SmoothRightHandPosition(25f);

            // Aiming has to be exact - smoothing would visibly separate hand from grip.
            if (character.IsAiming)
                character.DoHandPositioningNoSmoothing();
        }

        JUHoldableItem right = character.HoldableItemInUseRightHand;
        JUHoldableItem left = character.HoldableItemInUseLeftHand;

        float blend = ikTransitionSpeed * Time.deltaTime;

        character.LeftHandWeightIK = Mathf.Lerp(character.LeftHandWeightIK, TargetHandWeight(left, right), blend);
        character.RightHandWeightIK = Mathf.Lerp(character.RightHandWeightIK, TargetHandWeight(right, left), blend);

        lookWeightIK = character.FiringMode && !character.IsReloading
            ? Mathf.Lerp(lookWeightIK, 1f, 5f * Time.deltaTime)
            : Mathf.Lerp(lookWeightIK, 0f, 6f * Time.deltaTime);
    }

    /// <summary>
    /// What one hand's IK weight should settle on, given what each hand holds. Same
    /// cases JUTPS walks through, written once for either hand.
    /// </summary>
    private float TargetHandWeight(JUHoldableItem own, JUHoldableItem other)
    {
        if (character.IsWeaponSwitching || character.IsRolling || character.IsReloading)
            return 0f;

        // Holding something in this hand: it sits on its own grip while in fire mode.
        if (own != null)
        {
            if (own.HoldPose != JUHoldableItem.ItemHoldingPose.Free)
                return character.FiringModeIK ? 1f : 0f;

            // A free-pose item leaves the hand available to support the other weapon.
            return other != null && other.OppositeHandPosition != null ? 1f : 0f;
        }

        // Empty hand: it supports the other weapon when that weapon offers a grip.
        if (other != null && other.OppositeHandPosition != null)
            return (!character.FiringModeIK && character.FiringMode) ? 0f : 1f;

        return 0f;
    }

    // Unity calls this on every component of the Animator's GameObject, so it runs here
    // even though JUCharacterController - which has its own copy - is disabled.
    private void OnAnimatorIK(int layerIndex)
    {
        if (isOwned || character == null)
            return;

        if (!character.InverseKinematics || character.IsRolling)
            return;

        // The same three calls JUCharacterController.OnAnimatorIK makes for its owner.
        character.LeftHandToRespectiveIKPosition(character.LeftHandWeightIK, 0f);
        character.RightHandToRespectiveIKPosition(character.RightHandWeightIK, character.RightHandWeightIK / 1.2f);

        Vector3 lookPosition = character.GetLookPosition();
        Vector3 toLook = lookPosition - transform.position;

        if (toLook.sqrMagnitude < 0.0001f)
            return;

        float bodyWeight = character.IsProne ? 0.1f : 0.3f;
        float intensity = Vector3.Dot(transform.forward, toLook.normalized);

        character.LookAtIK(lookPosition, intensity * lookWeightIK, bodyWeight, 0.6f);
    }
}
