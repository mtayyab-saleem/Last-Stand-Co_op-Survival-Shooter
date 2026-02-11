using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using JUTPS.ItemSystem;
using JUTPS.ExtendedInverseKinematics;
using JUTPS.VehicleSystem;
using JUTPS.InventorySystem;
using JUTPS.ActionScripts;
using JUTPS.PhysicsScripts;
using JUTPS.WeaponSystem;
using JUTPS.CameraSystems;

//using JU_INPUT_SYSTEM;

namespace JUTPS.CharacterBrain
{

    public class JUCharacterBrain : MonoBehaviour
    {
        //ESSENTIALS
        [HideInInspector] public Vector3 UpDirection = Vector3.zero;
        [HideInInspector] public Quaternion UpOrientation;
        [HideInInspector] private Quaternion ForwardOrientation = Quaternion.identity;
        [HideInInspector] public Animator anim;
        [HideInInspector] public Rigidbody rb;
        [HideInInspector] public Collider coll;
        protected Camera MyCamera;
        [HideInInspector] public JUCameraController MyPivotCamera;
        [HideInInspector] private Quaternion lastDirectionTransformRotation;

        //ADDITIONALS
        [HideInInspector] protected AdvancedRagdollController Ragdoller;
        [HideInInspector] protected JUFootPlacement FootPlacerIK;
        [HideInInspector] public JUHealth CharacterHealth;
        [HideInInspector] public DriveVehicles DriveVehicleAbility;
        public JUInventory Inventory;
        [HideInInspector] public Damager LeftHandDamager, RightHandDamager, LeftFootDamager, RightFootDamager;

        public enum MovementMode { Free, AwaysInFireMode, JuTpsClassic }

        //MOVEMENT VARIABLES
        [HideInInspector] public float VelocityMultiplier;
        protected float VerticalY;
        protected float HorizontalX;
        [HideInInspector] public Transform DirectionTransform;

        //ROTATION VARIABLES
        protected float BodyRotation;
        protected float IdleTurn;
        protected Vector3 EulerRotation;
        protected Quaternion DesiredCameraRotation;
        [HideInInspector] public Vector3 DesiredDirection;
        //JUMP INERT
        protected float LastX, LastY, LastVelMult;

        //STEP CORRECTION VARIABLES
        [HideInInspector] public RaycastHit Step_Hit, FootStepHit;
        protected bool AdjustHeight;
        private bool GoToStepPosition;
        private Vector3 StartStepUpCharacterPosition, StepPosition;
        private float GoingToStepTime;

        //MOVEMENT
        [Header("Movement Settings")]
        public MovementMode LocomotionMode;
        public bool SetRigidbodyVelocity = true;
        public float FireModeMaxTime = 1;
        public float Speed = 3;

        public float WalkSpeed = 0.5f;
        public float CrouchSpeed = 0.4f;
        public float RunSpeed = 1f;

        public float SprintingSpeedMax = 3f;
        public float SprintingAcceleration = 2;
        public float SprintingDeceleration = 0.6f;

        public float RotationSpeed = 2;
        public float JumpForce = 3f;
        public float StoppingSpeed = 2;
        public float AirInfluenceControll = 0.5f;
        public float MaxWalkableAngle = 45;
        public bool CurvedMovement = true;
        public bool LerpRotation = false;
        public bool BodyInclination = true;
        public bool MovementAffectsWeaponAccuracy;
        public float OnMovePrecision = 4;
        public Vector3 LookAtPosition;

        //RUN IMPULSE / SPRINT
        public bool SprintingSkill = true;
        protected bool CanSprint = true;
        protected bool ReachedMaxSprintSpeed = false;
        protected float CurrentSprintSpeedIntensity;
        public bool SprintOnRunButton = false;
        public bool UnlimitedSprintDuration = false;

        //GROUND ANGLE DESACELERATION
        public bool GroundAngleDesaceleration = true;
        public float GroundAngleDesacelerationMultiplier = 1.5f;
        protected float SlidingVelocity;
        public float GroundAngle;
        public Vector3 GroundNormal;
        public Vector3 GroundPoint;
        //ROOT MOTION
        public bool RootMotion = false;
        public float RootMotionSpeed = 1;
        public bool RootMotionRotation = false;
        public Vector3 RootMotionDeltaPosition;


        [Header("Default Event Options")]
        public bool RagdollWhenDie;
        //EVENTS
        public JUCharacterEvents Events;

        [System.Serializable]
        public struct JUCharacterEvents
        {
            public UnityEvent OnDeath, OnRessurect, OnRun, OnSprinting, OnRoll, OnJump, OnCrouch, OnGetUp, OnStartMoving, OnIdle, OnEnterFireMode, OnExitFireMode, OnPunch;
            private bool calledRun, calledOnSprinting, calledStartMoving, calledStopMoving, calledOnFireMode, calledOnExitFireMode;
            public void UpdateRuntimeEventsCallbacks(JUCharacterBrain juchar)
            {
                //Run Event
                if (juchar.IsRunning && calledRun == false) { OnRun.Invoke(); } else if (calledRun == true) { calledRun = false; }
                //Sprint Event
                if (juchar.IsSprinting && calledOnSprinting == false) { OnSprinting.Invoke(); } else if (calledOnSprinting == true) { calledOnSprinting = false; }
                //Start Move Event
                if (juchar.IsMoving && calledStartMoving == false) { OnStartMoving.Invoke(); } else if (calledStartMoving == true) { calledStartMoving = false; }
                //Stop Move/Idle Event
                if (juchar.IsMoving == false && calledStopMoving == false) { OnIdle.Invoke(); } else if (calledStopMoving == true) { calledStopMoving = false; }
                //Fire Mode Event
                if (juchar.FiringMode && calledOnFireMode == false) { OnEnterFireMode.Invoke(); } else if (calledOnFireMode == true) { calledOnFireMode = false; }
                //Exit Fire Mode Event
                if (juchar.FiringMode == false && calledOnExitFireMode == false) { OnExitFireMode.Invoke(); } else if (calledOnExitFireMode == true) { calledOnExitFireMode = false; }
            }
            public void SetEventListeners(JUCharacterBrain juchar)
            {
                OnDeath.AddListener(juchar.KillCharacter);
                OnRessurect.AddListener(juchar.RessurectCharacter);
                OnRoll.AddListener(juchar._Roll);
                OnJump.AddListener(juchar._Jump);
                OnCrouch.AddListener(juchar._Crouch);
                OnGetUp.AddListener(juchar._GetUp);
                OnPunch.AddListener(juchar._DoPunch);
            }
            public void RemoveEventListeners(JUCharacterBrain juchar)
            {
                OnDeath.RemoveListener(juchar.KillCharacter);
                OnRessurect.RemoveListener(juchar.RessurectCharacter);
                OnRoll.RemoveListener(juchar._Roll);
                OnJump.RemoveListener(juchar._Jump);
                OnCrouch.RemoveListener(juchar._Crouch);
                OnGetUp.RemoveListener(juchar._GetUp);
                OnPunch.RemoveListener(juchar._DoPunch);

            }
        }

        [Header("Ground Check Settings")]
        public LayerMask WhatIsGround;
        public float GroundCheckRadius = 0.1f;
        public float GroundCheckHeighOfsset = 0.1f;
        public float GroundCheckSize = 0.5f;

        [Header("Wall Check Settings")]
        public LayerMask WhatIsWall;
        public float WallRayHeight = 1f;
        public float WallRayDistance = 0.6f;

        [Header("Step Settings")]
        public bool EnableStepCorrection = true;
        public float UpStepSpeed = 5;
        public LayerMask StepCorrectionMask;
        public float FootstepHeight = 0.4f;
        public float ForwardStepOffset = 0.6f;
        public float StepHeight = 0.02f;
        public bool EnableUngroundedStepUp = true;
        public float UngroundedStepUpSpeed = 4;
        public float UngroundedStepUpRayDistance = 0.1f;
        public float StoppingTimeOnStepPosition = 0.5f;

        [Header("Animator Parameters")]
        public JUAnimatorParameters AnimatorParameters;

        [Header("Item Management Settings")]
        //public LayerMask CrosshairHitMask;
        public GameObject PivotItemRotation;
        public WeaponAimRotationCenter WeaponHoldingPositions;

        [HideInInspector] public JUHoldableItem HoldableItemInUseRightHand, HoldableItemInUseLeftHand;
        [HideInInspector] public Weapon WeaponInUseRightHand, WeaponInUseLeftHand;
        [HideInInspector] public MeleeWeapon MeleeWeaponInUseRightHand, MeleeWeaponInUseLeftHand;

        protected int CurrentItemIDRightHand = -1, CurrentItemIDLeftHand = -1; // [-1] = Hand
        [Header("Fire Mode Settings")]
        public PressAimMode AimMode;
        public float FireModeWalkSpeed = 0.5f, FireModeRunSpeed = 1.3f, FireModeCrouchSpeed = 0.5f;
        public enum PressAimMode { HoldToAim, OnePressToAim }


        //Animator Layers Weight
        [HideInInspector] protected float IsArmedWeight;
        [HideInInspector] protected float LegsLayerWeight;
        [HideInInspector] protected float BothArmsLayerWeight;
        [HideInInspector] protected float RightArmLayerWeight, LeftArmLayerWeight;
        [HideInInspector] protected float WeaponSwitchLayerWeight;
        [HideInInspector] protected float WeaponSwitchingCurrentTime;

        //Hand IK Targets
        [HideInInspector] public Transform IKPositionRightHand;
        [HideInInspector] public Transform IKPositionLeftHand;
        [HideInInspector] private Transform RightHandIKPositionTarget;
        [HideInInspector] private Transform LeftHandIKPositionTarget;
        //Bones
        [HideInInspector] public Transform HumanoidSpine;
        [HideInInspector] public Transform RightFootBone, LeftFootBone;

        protected float LookWeightIK = 0;
        protected float ArmsWeightIK = 0;
        public float LeftHandWeightIK = 0;
        public float RightHandWeightIK = 0;

        //FIRE MODE Timer
        [HideInInspector] public float CurrentTimeToDisableFireMode;

        public Vehicle VehicleInArea;
        public Collider[] CharacterHitBoxes;


        [Header("States")]
        public bool IsDead;
        public bool DisableAllMove;
        public bool CanMove = true, CanRotate = true;
        public bool IsMoving;
        public bool IsRunning;
        public bool IsSprinting;
        public bool IsCrouched;
        public bool IsProne;
        public bool CanJump;
        public bool IsJumping;
        public bool IsGrounded = true;
        public bool IsSliding;
        public bool IsMeleeAttacking;
        public bool IsPunching;
        public bool IsItemEquiped;
        public bool IsDualWielding;
        public bool IsAiming = false;
        public bool FiringMode = false;
        public bool FiringModeIK = true;
        public bool ToPickupItem;
        public bool IsRolling;
        public bool IsRagdolled;
        public bool IsDriving;
        public bool UsedItem;
        public bool IsReloading;
        public bool WallAHead;
        public bool IsWeaponSwitching;
        public bool InverseKinematics = true;
        public bool IsArtificialIntelligence = false;
        #region Unity Standard Functions

        private void OnEnable()
        {
            Events.SetEventListeners(this);
        }
        private void OnDisable()
        {
            Events.RemoveEventListeners(this);
        }
        private void OnDestroy()
        {
            if (PivotItemRotation != null) Destroy(PivotItemRotation);
        }
        protected virtual void Awake()
        {
            //Change states
            CanMove = true;
            CanRotate = true;
            UpDirection = Vector3.up;

            // Get necessary references
            anim = GetComponent<Animator>();
            rb = GetComponent<Rigidbody>();
            coll = GetComponent<Collider>();
            if (WhatIsGround == 0) WhatIsGround = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider", "Vehicle");
            if (WhatIsWall == 0) WhatIsWall = LayerMask.GetMask("Default", "Terrain", "Walls", "VehicleMeshCollider", "Vehicle");

            // Generate Direction Transform
            DirectionTransform = CreateEmptyTransform("Direction Transform", transform.position, transform.rotation, transform, true);
            // Generate Inverse Kinematics Transforms
            LeftHandIKPositionTarget = CreateEmptyTransform("Left Hand Target", transform.position, transform.rotation, transform, false);
            RightHandIKPositionTarget = CreateEmptyTransform("Right Hand Target", transform.position, transform.rotation, transform, false);

            IKPositionLeftHand = CreateEmptyTransform("Left Hand IK Position", transform.position, transform.rotation, transform, true);
            IKPositionRightHand = CreateEmptyTransform("Right Hand IK Position", transform.position, transform.rotation, transform, true);

            //Disable IK and Firing Mode
            FiringMode = false; ArmsWeightIK = 0;

            // Get character hitboxes
            CharacterHitBoxes = GetComponentsInChildren<Collider>();

            // Setup hitbox physic collision ignore
            foreach (Collider hitbox in CharacterHitBoxes)
            {
                if (hitbox != coll) Physics.IgnoreCollision(coll, hitbox);
            }


            // Get Item Aim Rotation Center
            PivotItemRotation = GetComponentInChildren<WeaponAimRotationCenter>().gameObject;
            WeaponHoldingPositions = PivotItemRotation.GetComponentInChildren<WeaponAimRotationCenter>();

            // Unparenting Item Aim Rotation Center
            //PivotItemRotation.transform.SetParent(null);
            // Hide
            //PivotItemRotation.gameObject.hideFlags = HideFlags.HideInHierarchy;

            // Start with no item selected
            CurrentItemIDRightHand = -1;
            WeaponInUseRightHand = null;
            HoldableItemInUseRightHand = null;

            // Get Camera references
            MyPivotCamera = (IsArtificialIntelligence == false) ? FindObjectOfType<JUCameraController>() : null;
            MyCamera = (MyPivotCamera != null && IsArtificialIntelligence == false) ? MyPivotCamera.mCamera : null;

            // Get last character spine bone
            if (HumanoidSpine == null) { HumanoidSpine = anim.GetLastSpineBone(); }

            // Get Foot Bones
            if (LeftFootBone == null || RightFootBone == null)
            {
                LeftFootBone = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
                RightFootBone = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            }

            //Get Damagers
            if (LeftHandDamager == null)
            {
                LeftHandDamager = GetLeftHandDamager();
            }
            if (RightHandDamager == null)
            {
                RightHandDamager = GetRightHandDamager();
            }
            if (LeftFootDamager == null)
            {
                LeftFootDamager = GetLeftFootDamager();
            }
            if (RightFootDamager == null)
            {
                RightFootDamager = GetRightFootDamager();
            }

            //Get JUHealth
            if (TryGetComponent(out JUHealth health)) { CharacterHealth = health; CharacterHealth.OnDeath.AddListener(DisableDamagers); }

            // Get Inventory
            if (TryGetComponent(out JUInventory juInventory)) { Inventory = juInventory; }

            // Get Ragdoller
            if (TryGetComponent(out AdvancedRagdollController ragdollerController)) { Ragdoller = ragdollerController; }

            // Get JU Foot Placer
            if (TryGetComponent(out JUFootPlacement footplacer)) { FootPlacerIK = footplacer; }

            // Get Drive Vehicle Ability
            if (TryGetComponent(out DriveVehicles driver)) { DriveVehicleAbility = driver; }
        }

        #endregion



        #region Utilities Functions
        public Vector3 GetLookPosition()
        {
            if (MyCamera == null)
            {
                return LookAtPosition;
            }
            else
            {
                if (LookAtPosition != Vector3.zero)
                {
                    return LookAtPosition;
                }
                else
                {
                    return MyCamera.transform.position + MyCamera.transform.forward * 100;
                }
            }
        }
        public Vector3 GetLookDirectionEulerAngles()
        {
            if (MyCamera != null && LookAtPosition == Vector3.zero)
                return MyCamera.transform.eulerAngles;

            Vector3 direction = LookAtPosition - PivotItemRotation.transform.position;

            if (direction.magnitude < 0.01f)
                return Quaternion.identity.eulerAngles;
            direction /= direction.magnitude;
            return Quaternion.LookRotation(direction).eulerAngles;
        }
        public Vector3 GetCurrentWeaponLookDirection(bool RightHand = true)
        {
            Vector3 direction = Vector3.up;
            if (RightHand)
            {
                if (WeaponInUseRightHand != null)
                {
                    Vector3 shootDirectionRight = (GetLookPosition() - WeaponInUseRightHand.Shoot_Position.position).normalized;
                    direction = shootDirectionRight;
                }
            }
            else
            {
                if (WeaponInUseLeftHand != null)
                {
                    Vector3 shootDirectionLeft = (GetLookPosition() - WeaponInUseLeftHand.Shoot_Position.position).normalized;
                    direction = shootDirectionLeft;
                }
            }
            return direction;
        }

        public void SetForwardOrientation(Quaternion forwardRotation)
        {
            ForwardOrientation = forwardRotation;
        }
        public Quaternion GetForwardOrientation()
        {
            Quaternion orientation = (MyCamera == null || ForwardOrientation != Quaternion.identity) ? ForwardOrientation : MyCamera.transform.rotation;

            if (IsArtificialIntelligence == true && MyPivotCamera != null)
            {
                MyPivotCamera = null;
                MyCamera = null;
                orientation = ForwardOrientation;
            }

            return orientation;
        }
        public virtual void ResetDefaultLayersWeight(float Speed = 0, bool LegLayerException = false, bool RightArmLayerException = false, bool LeftArmLayerException = false, bool BothArmsLayerException = false, bool WeaponSwitchLayerException = false)
        {
            if (Speed == 0)
            {
                if (!LegLayerException) LegsLayerWeight = 0;
                if (!RightArmLayerException) RightArmLayerWeight = 0;
                if (!LeftArmLayerException) LeftArmLayerWeight = 0;
                if (!BothArmsLayerException) BothArmsLayerWeight = 0;
                if (!WeaponSwitchLayerException) WeaponSwitchLayerWeight = 0;
            }
            else
            {
                if (!LegLayerException) LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, 0, Speed * Time.deltaTime);
                if (!RightArmLayerException) RightArmLayerWeight = Mathf.Lerp(RightArmLayerWeight, 0, Speed * Time.deltaTime);
                if (!LeftArmLayerException) LeftArmLayerWeight = Mathf.Lerp(LeftArmLayerWeight, 0, Speed * Time.deltaTime);
                if (!BothArmsLayerException) BothArmsLayerWeight = Mathf.Lerp(BothArmsLayerWeight, 0, Speed * Time.deltaTime);
                if (!WeaponSwitchLayerException) WeaponSwitchLayerWeight = Mathf.Lerp(WeaponSwitchLayerWeight, 0, Speed * Time.deltaTime);
            }
        }
        public virtual void SetDefaultAnimatorsLayersWeight(JUAnimatorParameters parameters, float LegsWeight, float RightArmWeight, float LeftArmWeight, float BothArmsWeight, float WeaponSwitchWeight)
        {
            anim.SetLayerWeight(parameters._LegsLayerIndex, LegsWeight);
            anim.SetLayerWeight(parameters._RightArmLayerIndex, RightArmWeight);
            anim.SetLayerWeight(parameters._LeftArmLayerIndex, LeftArmWeight);
            anim.SetLayerWeight(parameters._BothArmsLayerIndex, BothArmsWeight);
            anim.SetLayerWeight(parameters._SwitchWeaponLayerIndex, WeaponSwitchWeight);
        }
        public Transform GetLastSpineBone()
        {
            if (anim == null) return null;
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Head).parent.parent;
            return spine;
        }
        private Damager GetRightHandDamager()
        {
            if (RightHandDamager != null) return RightHandDamager;

            Damager rDamage = null;
            Damager[] damagers = GetComponentsInChildren<Damager>();

            foreach (Damager targetdamager in damagers)
            {
                if (targetdamager.transform.parent == anim.GetBoneTransform(HumanBodyBones.RightLowerArm))
                {
                    rDamage = targetdamager;
                }
            }

            return rDamage;
        }
        private Damager GetLeftHandDamager()
        {
            if (LeftHandDamager != null) return LeftHandDamager;

            Damager lDamage = null;
            Damager[] damagers = GetComponentsInChildren<Damager>();

            foreach (Damager targetdamager in damagers)
            {
                if (targetdamager.transform.parent == anim.GetBoneTransform(HumanBodyBones.LeftLowerArm))
                {
                    lDamage = targetdamager;
                }
            }

            return lDamage;
        }
        private Damager GetLeftFootDamager()
        {
            if (LeftFootDamager != null) return LeftFootDamager;

            Damager lDamage = null;
            Damager[] damagers = GetComponentsInChildren<Damager>();

            foreach (Damager targetdamager in damagers)
            {
                if (targetdamager.transform.parent == anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg))
                {
                    lDamage = targetdamager;
                }
            }

            return lDamage;
        }
        private Damager GetRightFootDamager()
        {
            if (RightFootDamager != null) return RightFootDamager;

            Damager lDamage = null;
            Damager[] damagers = GetComponentsInChildren<Damager>();

            foreach (Damager targetdamager in damagers)
            {
                if (targetdamager.transform.parent == anim.GetBoneTransform(HumanBodyBones.RightUpperLeg))
                {
                    lDamage = targetdamager;
                }
            }

            return lDamage;
        }
        public void DisableDamagers()
        {
            if (LeftFootDamager != null) LeftFootDamager.gameObject.SetActive(false);
            if (RightFootDamager != null) RightFootDamager.gameObject.SetActive(false);
            if (LeftHandDamager != null) LeftHandDamager.gameObject.SetActive(false);
            if (RightHandDamager != null) RightHandDamager.gameObject.SetActive(false);
        }

        public void PhysicalIgnore(GameObject GameObjectToIgnore, bool ignore)
        {
            // Ignore ALL colliders of object
            Collider[] obj_colliders = GameObjectToIgnore.GetComponentsInChildren<Collider>(true);

            //No colliders
            if (obj_colliders.Length == 0) { Debug.Log("There is not colliders in " + GameObjectToIgnore.name + " to ignore"); return; }

            //Simple Ignore
            if (obj_colliders.Length == 1)
            {
                PhysicalIgnore(obj_colliders[0], ignore);
                return;
            }


            foreach (Collider obj_col in obj_colliders)
            {
                foreach (Collider hitbox in CharacterHitBoxes)
                {
                    //Ignore
                    Physics.IgnoreCollision(obj_col, hitbox, ignore);
                }
            }
            if (!ignore)
            {
                // Debug.Log("all " + gameObject.name + " colliders are IGNORING all " + GameObjectToIgnore.name + " colliders ");
            }
            else
            {
                //  Debug.Log("all " + gameObject.name + " colliders are DETECTING COLLISION all " + GameObjectToIgnore.name + " colliders ");
            }
        }
        public void PhysicalIgnore(Collider col, bool ignore)
        {
            foreach (Collider hitbox in CharacterHitBoxes) Physics.IgnoreCollision(col, hitbox, ignore);
        }

        protected Transform CreateEmptyTransform(string name = "New Transform", Vector3 position = default(Vector3), Quaternion rotation = default(Quaternion), Transform parent = null, bool hide = false)
        {
            Transform newTransform = new GameObject(name).transform;

            newTransform.position = position;
            newTransform.rotation = rotation;
            newTransform.parent = parent;

            if (hide)
            {
                newTransform.hideFlags = HideFlags.HideInHierarchy;
                newTransform.gameObject.hideFlags = HideFlags.HideAndDontSave;
            }

            return newTransform;
        }
        #endregion

        #region Locomotion Functions

        public void SetMoveInput(float HorizontalInput, float VerticalInput, float Smooth = -1)
        {
            if (Smooth <= 0)
            {
                HorizontalX = HorizontalInput;
                VerticalY = VerticalInput;
            }
            else
            {
                HorizontalX = Mathf.Lerp(HorizontalX, HorizontalInput, Smooth * Time.deltaTime);
                VerticalY = Mathf.Lerp(VerticalY, VerticalInput, Smooth * Time.deltaTime);
            }
        }
        public virtual void Rotate(float HorizontalX, float VerticalY)
        {
            if (IsDriving == true || CanRotate == false)
                return;

            //Get the camera forward direction
            DesiredCameraRotation = GetForwardOrientation();

            //Rotation Direction
            DesiredDirection = new Vector3(HorizontalX, 0, VerticalY);

            // ---- BODY ROTATION ----
            Vector3 DesiredEulerAngles = transform.localEulerAngles;

            if (IsMoving)
            {
                // >>> Set Desired Direction

                //Look to desired direction
                if ((Mathf.Abs(HorizontalX) > 0.01f || Mathf.Abs(VerticalY) > 0.01f))
                {
                    DirectionTransform.rotation = DesiredCameraRotation * Quaternion.LookRotation(DesiredDirection.normalized);

                    //Prevent negative Vector.Up glitch
                    if (Vector3.Dot(transform.up, Vector3.up) < -0.989f)
                    {
                        //Debug.Log("Up direction dot = " + Vector3.Dot(transform.up, Vector3.up));
                        DirectionTransform.rotation = lastDirectionTransformRotation;
                    }
                    else
                    {

                        lastDirectionTransformRotation = DirectionTransform.rotation;
                    }
                }

                if (LerpRotation)
                {
                    DesiredEulerAngles.y = Mathf.LerpAngle(DesiredEulerAngles.y, DirectionTransform.eulerAngles.y, (IsProne || IsCrouched ? 0.5f : 1) * RotationSpeed * Time.deltaTime);
                }
                else
                {
                    DesiredEulerAngles.y = Mathf.MoveTowardsAngle(DesiredEulerAngles.y, DirectionTransform.eulerAngles.y, (IsProne || IsCrouched ? 0.5f : 1) * 100 * RotationSpeed * Time.deltaTime);
                }
            }
            bool BlockFireModeCondition = ((HoldableItemInUseRightHand != null) ? HoldableItemInUseRightHand.BlockFireMode : false);
            //Firing Mode Rotation
            if (FiringMode && BlockFireModeCondition == false && IsRolling == false) // >>> Firing Mode Rotation
            {
                if (MyCamera != null)
                {
                    LookRotationToAimPosition((LookAtPosition != Vector3.zero) ? LookAtPosition : MyCamera.transform.position + MyCamera.transform.forward * 100, RotationSpeed, UpOrientation * Vector3.up);
                }
                else
                {
                    LookRotationToAimPosition(LookAtPosition, RotationSpeed, UpOrientation * Vector3.up);
                }
            }
            else           // >>> Free Mode Rotation
            {
                if (RootMotionRotation == false || RootMotion == false)
                {
                    if (CurvedMovement == true)
                    {
                        transform.localEulerAngles = DesiredEulerAngles;
                    }
                    else
                    {
                        if (Mathf.Abs(HorizontalX) > 0.01f || Mathf.Abs(VerticalY) > 0.01f)
                        {
                            //Force transform direction up to transform up
                            DirectionTransform.rotation = Quaternion.FromToRotation(DirectionTransform.up, UpDirection) * DirectionTransform.rotation;

                            transform.rotation = Quaternion.Lerp(transform.rotation, DirectionTransform.rotation, ((IsRolling) ? 1.5f * RotationSpeed : RotationSpeed) * Time.deltaTime);
                        }
                    }
                }
            }

            //Adjust Up Rotation
            Quaternion Up_Direction = Quaternion.FromToRotation(transform.up, IsProne ? GroundNormal : UpDirection);
            UpDirection = (GroundNormal == Vector3.zero) ? Vector3.up : UpDirection;
            UpOrientation = Quaternion.Lerp(transform.rotation, Up_Direction * transform.rotation, IsGrounded ? 8 * Time.deltaTime : 2 * Time.deltaTime);
            transform.rotation = UpOrientation;

            //Force transform direction up to transform up
            DirectionTransform.rotation = Quaternion.FromToRotation(DirectionTransform.up, UpDirection) * DirectionTransform.rotation;
            Debug.DrawRay(DirectionTransform.position, DirectionTransform.forward);
        }
        public virtual void DoLookAt(Vector3 targetPosition = default(Vector3), float RotationSpeedMultiplier = 1, bool FreezeUpDirection = true)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(targetPosition - transform.position), RotationSpeedMultiplier * RotationSpeed * Time.deltaTime);
            if (FreezeUpDirection) transform.rotation = Quaternion.FromToRotation(transform.up, UpDirection) * transform.rotation;

        }
        public virtual void MoveForward(float SpeedMultiplier)
        {
            if (SetRigidbodyVelocity)
            {
                var localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
                rb.linearVelocity = transform.forward * SpeedMultiplier * Speed + transform.up * localVelocity.y;
                rb.linearVelocity = rb.linearVelocity;
                //rb.velocity = transform.forward * SpeedMultiplier * Speed + transform.up * rb.velocity.y;
            }
            else
            {
                transform.Translate(Vector3.forward * SpeedMultiplier * Speed * Time.deltaTime, Space.Self);
            }
        }
        public virtual void Move(Vector3 Movement, float SpeedMultiplier)
        {
            if (SetRigidbodyVelocity)
            {
                rb.linearVelocity = Movement * SpeedMultiplier * Speed;
            }
            else
            {
                transform.Translate(Movement * SpeedMultiplier * Speed * Time.deltaTime, Space.World);
            }
        }
        public virtual void Move(Transform DirectionMovement, float SpeedMultiplier)
        {
            if (SetRigidbodyVelocity)
            {
                var localVelocity = DirectionMovement.InverseTransformDirection(rb.linearVelocity);
                rb.linearVelocity = DirectionMovement.forward * SpeedMultiplier * Speed + transform.up * localVelocity.y;
            }
            else
            {
                transform.Translate(DirectionMovement.forward * SpeedMultiplier * Speed * Time.deltaTime, Space.World);
            }
        }

        /// <summary>
        /// This function uses the DirectionTransform variable to apply force.
        /// </summary>
        /// <param name="SpeedMultiplier">Speed Multiplier</param>
        public virtual void MoveDirectional(float SpeedMultiplier)
        {
            if (SetRigidbodyVelocity)
            {
                var localVelocity = transform.InverseTransformDirection(rb.linearVelocity);
                rb.linearVelocity = DirectionTransform.forward * SpeedMultiplier * Speed + transform.up * localVelocity.y;
                //rb.velocity = DirectionTransform.forward * SpeedMultiplier * Speed + transform.up * rb.velocity.y;
            }
            else
            {
                transform.Translate(DirectionTransform.forward * SpeedMultiplier * Speed * Time.deltaTime, Space.World);
            }
        }
        public void InAirMovementControl(bool JumpInert = true)
        {
            if (IsGrounded)
            {
                if (JumpInert)
                {
                    LastX = HorizontalX;
                    LastY = VerticalY;
                    LastVelMult = VelocityMultiplier;
                    CanMove = true;
                }
            }
            else
            {
                transform.Translate(0, -1f * Time.deltaTime, 0);
                //if (SetRigidbodyVelocity)
                //{
                //    if (IsMoving) rb.AddForce(DirectionTransform.forward * AirInfluenceControll * 10, ForceMode.Force);

                //}
                //else
                //{
                if (IsMoving)
                {
                    transform.Translate(DirectionTransform.forward * AirInfluenceControll / 2 * Time.deltaTime, Space.World);
                }
                //}
            }
        }
        protected virtual void LookRotationToAimPosition(Vector3 Position = default(Vector3), float RotationSpeed = 10, Vector3 Up_Direction = default(Vector3))
        {
            if (IsRolling == true) return;

            Vector3 lookAtPosition = Position;
            Vector3 lookingDirection = (lookAtPosition - transform.position).normalized;

            transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.forward, lookingDirection) * transform.rotation, 3 * RotationSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.FromToRotation(transform.up, (Up_Direction != Vector3.zero) ? Up_Direction : Vector3.up) * transform.rotation;
        }
        protected virtual void FireModeTimer(bool ShotInput, bool AimInput)
        {
            //Fire Mode Timer
            if (CurrentTimeToDisableFireMode < 20 && FiringMode == true && IsMeleeAttacking == false && IsReloading == false && ShotInput == false && AimInput == false)
            {
                CurrentTimeToDisableFireMode += Time.deltaTime;
                if (CurrentTimeToDisableFireMode >= FireModeMaxTime)
                {
                    FiringMode = false;
                    FiringModeIK = false;
                    CurrentTimeToDisableFireMode = 0;
                }
                //Aiming Disable FireMode
                if (IsAiming) CurrentTimeToDisableFireMode = 0;
            }
            else
            {
                CurrentTimeToDisableFireMode = 0;
            }
        }
        protected virtual void DoFireModeMovement(bool FiringMode)
        {
            if (IsDriving == true || FiringMode == false || IsRolling) return;

            //Movement
            if (CanMove && IsGrounded)
            {
                MoveDirectional(VelocityMultiplier);
            }

            IsArmedWeight = Mathf.Lerp(IsArmedWeight, 1, 5 * Time.deltaTime);

            //Running Firing Mode State
            if (IsRunning == true && IsSprinting == false && IsCrouched == false && WallAHead == false && IsGrounded == true && IsMoving == true)
            {
                VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, FireModeRunSpeed - GroundAngleDesacelerationValue(), 5 * Time.deltaTime);
            }
            //Walking Firing Mode State
            if (IsRunning == false && IsSprinting == false && IsCrouched == false && WallAHead == false && IsGrounded == true && IsMoving == true)
            {
                VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, FireModeWalkSpeed - GroundAngleDesacelerationValue(), 5 * Time.deltaTime);
            }
            //Crouch Fire Mode State
            if (IsRunning == false && IsSprinting == false && IsCrouched == true && WallAHead == false && IsGrounded == true && IsMoving == true)
            {
                VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, FireModeCrouchSpeed - GroundAngleDesacelerationValue(), 5 * Time.deltaTime);
            }
            if (IsMoving == false)
            {
                VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, 0f, 5 * Time.deltaTime);
            }

            //Disable Run Impulse / Sprinting
            ReachedMaxSprintSpeed = false;
            CanSprint = true;
            CurrentSprintSpeedIntensity = 0;
            IsSprinting = false;
        }
        protected virtual void DoFreeMovement(bool FiringMode)
        {
            if (IsDriving || FiringMode) return;

            IsArmedWeight = Mathf.Lerp(IsArmedWeight, 0, 3 * Time.deltaTime);

            //>>> Set Rigidbody Movement 
            if (IsGrounded && CanMove && !RootMotion)
            {
                if (CurvedMovement == true)
                {
                    MoveForward(VelocityMultiplier);
                }
                else
                {
                    Move(DirectionTransform, VelocityMultiplier);
                }
            }
            // Run Impulse / Sprinting
            Sprinting();
            // Locomotion Speed Controller
            if (IsMoving && IsMeleeAttacking == false && IsPunching == false)
            {
                // >>> Walk State
                if (IsRunning == false && IsSprinting == false && IsCrouched == false && WallAHead == false)
                {
                    VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, WalkSpeed - GroundAngleDesacelerationValue(), 6 * Time.deltaTime);
                    IsSprinting = false;
                }
                // Crouch State
                if (IsCrouched == true && IsRunning == false && WallAHead == false)
                {
                    VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, CrouchSpeed - GroundAngleDesacelerationValue(), 6 * Time.deltaTime);
                    IsSprinting = false;
                }

                // >>> Fast Run/Sprint State
                if (IsRunning == true && IsSprinting == true && IsCrouched == false && WallAHead == false)
                {
                    VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, RunSpeed - GroundAngleDesacelerationValue(), 6 * Time.deltaTime);
                }

                // >>> Slow Run State
                if (IsRunning == true && IsSprinting == false && IsCrouched == false && WallAHead == false)
                {
                    VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, RunSpeed - GroundAngleDesacelerationValue(), 6 * Time.deltaTime);
                    IsSprinting = false;
                }

            }
            else
            {
                //Idle State
                if (IsGrounded)
                {
                    VelocityMultiplier = Mathf.MoveTowards(VelocityMultiplier, 0f, (StoppingSpeed + Mathf.Lerp(0, 0.5f, VelocityMultiplier)) * Time.deltaTime);
                }
                IsRunning = false;
                IsSprinting = false;
                ReachedMaxSprintSpeed = false;
                CurrentSprintSpeedIntensity = Mathf.MoveTowards(CurrentSprintSpeedIntensity, 0, 3 * Time.deltaTime);
                CanSprint = true;
            }

        }
        protected Vector3 WordSpaceToBlendTreeSpace(Vector3 LookAtPosition, Transform DirectionTransform)
        {
            Vector3 inputaxis = new Vector3();
            inputaxis = DirectionTransform.forward;
            //inputaxis.x = DirectionTransform.forward.x;
            //inputaxis.z = DirectionTransform.forward.z;
            //inputaxis.y = 0;

            float forwardBackwardsMagnitude = 0;
            float rightLeftMagnitude = 0;

            if (inputaxis.magnitude > 0)
            {
                //Forward Input Value
                Vector3 normalizedLookingAt = LookAtPosition - transform.position;
                normalizedLookingAt.Normalize();

                forwardBackwardsMagnitude = Mathf.Clamp(Vector3.Dot(inputaxis, normalizedLookingAt), -1, 1);

                //Righ Input Value
                Vector3 perpendicularLookingAt = new Vector3(normalizedLookingAt.z, 0, -normalizedLookingAt.x);
                rightLeftMagnitude = Mathf.Clamp(Vector3.Dot(inputaxis, transform.right), -1, 1);

                return new Vector3(rightLeftMagnitude, 0, forwardBackwardsMagnitude).normalized;
            }
            else
            {
                return inputaxis;
            }
        }
        protected virtual void CalculateBodyRotation(ref float bodyRotation)
        {
            if (IsMoving && BodyInclination && CanMove && !WallAHead)
            {
                if (IsGrounded)
                {
                    bodyRotation = Mathf.LerpAngle(bodyRotation, DesiredRotationAngle() / 180, 2.5f * Time.deltaTime);

                    if (Mathf.Abs(DesiredRotationAngle()) < 10)
                    {
                        bodyRotation = Mathf.LerpAngle(bodyRotation, 0, 2 * Time.deltaTime);
                        // transform.rotation = Quaternion.RotateTowards(transform.rotation, DirectionTransform.rotation, RotationSpeed * Time.deltaTime);
                    }
                }
                else
                {
                    bodyRotation = Mathf.Lerp(bodyRotation, 0f, 8 * Time.deltaTime);
                }
            }
            else
            {
                bodyRotation = Mathf.Lerp(bodyRotation, 0f, 8 * Time.deltaTime);
            }
        }

        [HideInInspector] private Vector3 oldEulerAngles;
        public void CalculateRotationIntensity(ref float RotationIntensity, float Multiplier = 2)
        {
            float diff = Multiplier * Vector3.SignedAngle(transform.forward, Quaternion.Euler(oldEulerAngles) * Vector3.forward, transform.up);

            RotationIntensity = Mathf.LerpAngle(RotationIntensity, diff, 5 * Time.deltaTime);

            //if(!IsArtificialIntelligence)Debug.Log(diff);
            oldEulerAngles = transform.eulerAngles;
        }

        protected float DesiredRotationAngle()
        {
            return Vector3.SignedAngle(transform.forward, DirectionTransform.forward, transform.up);
        }

        protected virtual void Sprinting()
        {
            if (SprintingSkill)
            {
                if (FiringMode)
                {
                    CurrentSprintSpeedIntensity = 0;
                    ReachedMaxSprintSpeed = true;
                }

                if (IsRunning && IsSprinting && IsPunching == false)
                {
                    if (CurrentSprintSpeedIntensity >= SprintingSpeedMax && UnlimitedSprintDuration == false)
                    {
                        ReachedMaxSprintSpeed = true;
                    }

                    //Speed Up
                    if (VelocityMultiplier <= SprintingSpeedMax && ReachedMaxSprintSpeed == false)
                    {
                        CurrentSprintSpeedIntensity = Mathf.Lerp(CurrentSprintSpeedIntensity, SprintingSpeedMax + 0.3f, SprintingAcceleration * Time.deltaTime);
                        VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, CurrentSprintSpeedIntensity - GroundAngleDesacelerationValue(), 10 * Time.deltaTime);
                    }


                    //Speed Down
                    if (ReachedMaxSprintSpeed == true)
                    {
                        CurrentSprintSpeedIntensity -= SprintingDeceleration * Time.deltaTime;
                        VelocityMultiplier = CurrentSprintSpeedIntensity;

                        if (VelocityMultiplier < RunSpeed)
                        {
                            CanSprint = false;
                            IsSprinting = false;
                            ReachedMaxSprintSpeed = false;
                            CurrentSprintSpeedIntensity = RunSpeed;
                        }
                    }
                }

                //Run Impulse
                if (IsRunning && CanSprint == true && IsSprinting == false && SprintOnRunButton == false)
                {
                    IsSprinting = true;
                }
                CurrentSprintSpeedIntensity = Mathf.Clamp(CurrentSprintSpeedIntensity, RunSpeed, SprintingSpeedMax);
            }
        }


        protected virtual void GroundCheck()
        {
            //Ground Check
            if (IsDriving == false)
            {
                Collider[] groundcheck = Physics.OverlapBox(transform.position + transform.up * GroundCheckHeighOfsset, new Vector3(GroundCheckRadius, GroundCheckSize, GroundCheckRadius), transform.rotation, WhatIsGround);
                if (groundcheck.Length != 0 && IsJumping == false)
                {
                    IsGrounded = true;
                }
                else if (IsGrounded == true)
                {
                    //Simulate Inert
                    if (!SetRigidbodyVelocity)
                    {
                        rb.AddForce(DirectionTransform.forward * LastVelMult * rb.mass * Speed, ForceMode.Impulse);
                    }

                    if (AdjustHeight == false) IsGrounded = false;
                }
            }

            //Ground Angle Check
            RaycastHit hit;
            if (Physics.Raycast(transform.position + transform.up * 0.5f, -transform.up, out hit, 2, WhatIsGround))
            {
                GroundAngle = Vector3.Angle(Vector3.up, hit.normal);
                GroundNormal = hit.normal;
                GroundPoint = hit.point;
            }
            else
            {
                GroundNormal = Vector3.zero;
                GroundAngle = 0;
                GroundPoint = Vector3.zero;
            }
        }
        protected Vector3 GetGroundPoint()
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position + transform.up * 0.5f, -transform.up, out hit, 1000, WhatIsGround))
            {
                GroundPoint = hit.point;
            }
            else
            {
                GroundPoint = Vector3.zero;
            }
            return GroundPoint;
        }
        protected virtual void WallAHeadCheck()
        {
            //Wall in front
            RaycastHit HitFront;
            if (Physics.Raycast(transform.position + transform.up * WallRayHeight, DirectionTransform.forward, out HitFront, WallRayDistance, WhatIsWall))
            {
                WallAHead = true;
                Debug.DrawLine(HitFront.point, transform.position + transform.up * WallRayHeight);
            }
            else
            {
                WallAHead = false;
            }
            if (WallAHead == true)
            {
                VelocityMultiplier = Mathf.Lerp(VelocityMultiplier, 0, 10 * Time.deltaTime);
                CurrentSprintSpeedIntensity = 0;
            }
        }
        protected virtual void SlopeSlide()
        {
            if (GroundAngle > MaxWalkableAngle && IsGrounded)
            {
                if (IsSliding == false)
                {
                    IsSliding = true;
                }
                else if (MaxWalkableAngle > 0)
                {
                    SlidingVelocity += Physics.gravity.y * Time.deltaTime;
                    transform.Translate(-GroundNormal * SlidingVelocity * Time.deltaTime, Space.World);
                    transform.Translate(Vector3.up * SlidingVelocity * Time.deltaTime, Space.World);

                }
            }
            else
            {
                SlidingVelocity = 0;
                IsSliding = false;
            }
        }

        protected float StepAngle()
        {
            float angle = 0;
            if (Step_Hit.point != Vector3.zero)
            {
                angle = Vector3.Angle(transform.up, Step_Hit.normal);
            }
            return angle;
        }
        public float GroundAngleDesacelerationValue()
        {
            if (GroundAngleDesaceleration == true && IsProne == false)
            {
                float value;
                float ClampGroundAngle = Mathf.Clamp(GroundAngle, 0, 60);

                if (IsRunning)
                {
                    value = ClampGroundAngle / 200;
                }
                else
                {
                    value = ClampGroundAngle / 700;
                }

                return value * GroundAngleDesacelerationMultiplier;
            }
            else { return 0f; }
        }

        protected virtual void StepCorrectionCalculation()
        {
            if (IsDriving || !CanMove) return;

            if (IsMoving && EnableUngroundedStepUp && IsGrounded == false && LeftFootBone != null && RightFootBone != null && UngroundedStepUpSpeed > 0 && WallAHead == false)
            {
                // Step detection
                if (Physics.SphereCast(transform.position + transform.up * FootstepHeight + transform.forward * ForwardStepOffset, ((CapsuleCollider)coll).radius / 2, -transform.up, out FootStepHit, UngroundedStepUpRayDistance, WhatIsGround) && GoToStepPosition == false) { if (FootStepHit.point.y > GroundPoint.y + StepHeight && FootStepHit.point.y > transform.position.y + StepHeight && StepAngle() < 10) { StepPosition = FootStepHit.point; GoToStepPosition = true; StartStepUpCharacterPosition = transform.position; } }
            }

            if (IsMoving && EnableStepCorrection && IsGrounded == true && WallAHead == false)
            {
                //Step height Correction
                if (Physics.Raycast(transform.position + transform.up * FootstepHeight + DirectionTransform.forward * ForwardStepOffset, -Vector3.up, out Step_Hit, FootstepHeight - StepHeight, WhatIsGround) && AdjustHeight == false)
                {
                    if (Step_Hit.point.y > transform.position.y && StepAngle() < 10)
                    {
                        AdjustHeight = true;
                    }
                }
                //else
                //{
                //Step_Hit.point = transform.position;
                //AdjustHeight = false;
                //}
            }
            //else
            //{
            //AdjustHeight = false;
            //Step_Hit.point = transform.position;
            //}

            if (!AdjustHeight) Step_Hit.point = transform.position;
        }

        protected virtual void StepCorrectionMovement()
        {
            if (GoToStepPosition && EnableUngroundedStepUp)
            {
                GoingToStepTime = Mathf.MoveTowards(GoingToStepTime, (1f+StoppingTimeOnStepPosition), UngroundedStepUpSpeed * Time.deltaTime);
                transform.position = Vector3.Slerp(StartStepUpCharacterPosition, StepPosition, GoingToStepTime);
                if (IsJumping == false)
                {
                    anim.SetBool(AnimatorParameters.Grounded, true);
                    anim.SetFloat(AnimatorParameters.LandingIntensity, 1.5f);
                }
                if (GoingToStepTime >= (1f + StoppingTimeOnStepPosition))
                {
                    GoToStepPosition = false;
                    GoingToStepTime = 0;
                    StartStepUpCharacterPosition = transform.position;
                }
                if (GoingToStepTime < (1f + StoppingTimeOnStepPosition))
                {
                    VelocityMultiplier = 0;
                }

                return;
            }

            if (AdjustHeight && !IsDriving)
            {
                Vector3 playerStepPosition = transform.position; playerStepPosition.y = Step_Hit.point.y;

                //transform.position = Vector3.Lerp(transform.position, playerStepPosition, 8 * UpStepSpeed * Time.deltaTime);
                transform.position = Vector3.MoveTowards(transform.position, playerStepPosition, UpStepSpeed * Time.deltaTime);
                //transform.position += transform.up * (UpStepSpeed / 2 + (UpStepSpeed / 2 * VelocityMultiplier)) * Time.fixedDeltaTime;
                rb.AddForce(transform.up * rb.mass / 8 * UpStepSpeed, ForceMode.Impulse);
                anim.SetBool(AnimatorParameters.Grounded, true);
                //rb.AddForce(transform.up * rb.mass * UpStepSpeed / 10, ForceMode.Impulse);

                if (transform.position.y > playerStepPosition.y + 0.05F)
                {
                    Step_Hit.point = transform.position;
                    AdjustHeight = false;
                }
            }
        }

        protected virtual void ApplyRootMotionOnLocomotion()
        {
            if (RootMotion && IsGrounded == true && IsJumping == false && !FiringMode && !IsDriving)
            {
                if (Ragdoller != null) { if (Ragdoller.State != AdvancedRagdollController.RagdollState.Animated) return; }

                anim.updateMode = AnimatorUpdateMode.Fixed;
                RootMotionDeltaPosition = anim.deltaPosition * Time.fixedDeltaTime;
                RootMotionDeltaPosition.y = 0;
                ///_______________________________________________________________________________________________________________________________________________________
                // >> NOTE:                                                                                                                                              |
                /// When decreasing the Time.timeScale, the Animator does not return the delta position correctly, preventing the character from moving in slow motion   |
                /// If Time Scale is different from 1, instead of rootmotion, normal motion without Root Motion base will be used so that it keeps moving in slow motion.|
                ///_______________________________________________________________________________________________________________________________________________________|

                if (Time.timeScale == 1)
                {
                    rb.linearVelocity = RootMotionDeltaPosition * 5000 * RootMotionSpeed + Vector3.up * rb.linearVelocity.y;
                }
                else
                {
                    if (CurvedMovement)
                    {
                        rb.linearVelocity = transform.forward * VelocityMultiplier * Speed + Vector3.up * rb.linearVelocity.y;
                    }
                    else
                    {
                        rb.linearVelocity = DirectionTransform.forward * VelocityMultiplier * Speed + Vector3.up * rb.linearVelocity.y;
                    }
                }
                if (RootMotionRotation)
                {
                    transform.Rotate(0, anim.deltaRotation.y * 160, 0);
                }
            }
        }

        #endregion

        #region Character Actions Functions
        protected virtual void UseRightHandItem(bool ShotInput, bool ShotDownInput)
        {
            if (HoldableItemInUseRightHand == null) return;
            if ((HoldableItemInUseRightHand is Weapon) == true || (HoldableItemInUseRightHand is MeleeWeapon) == true) { return; }
            //Debug.Log("Is Righ Hand Holdable Item selected");
            if (!IsItemEquiped || IsRolling) return;

            //Disable Aiming
            IsAiming = false;
            bool canUseItem = false;

            if (HoldableItemInUseRightHand is ThrowableItem && ShotDownInput)
            {
                anim.SetTrigger((HoldableItemInUseRightHand as ThrowableItem).AnimationTriggerParameterName);
                return;
            }

            //Continuous Item Using
            if (HoldableItemInUseRightHand.ContinuousUseItem)
            {
                if (ShotInput)
                {
                    if (IsRolling == false && IsDriving == false && HoldableItemInUseRightHand.CanUseItem)
                    {
                        UseEquipedItem();
                    }
                    else
                    {
                        HoldableItemInUseRightHand.StopUseItem();
                    }
                }
                else
                {
                    HoldableItemInUseRightHand.StopUseItem();
                }
            }
            else
            {
                //Sequencial Item Using
                if (ShotInput && HoldableItemInUseRightHand.IsUsingItem == false)
                {
                    if (IsRolling == false && IsDriving == false && canUseItem)
                    {
                        UseEquipedItem();
                    }
                    else
                    {
                        HoldableItemInUseRightHand.StopUseItem();
                    }
                }
                else
                {
                    HoldableItemInUseRightHand.StopUseItem();
                }

                //Reenable Use item
                HoldableItemInUseRightHand.StopUseItem();
            }
            if (HoldableItemInUseRightHand.ContinuousUseItem == false)
            {
                canUseItem = !ShotInput;
            }
        }
        protected virtual void UseLeftHandItem(bool ShotInput, bool ShotDownInput)
        {
            if (HoldableItemInUseLeftHand == null) return;
            if ((HoldableItemInUseLeftHand is Weapon) == true || (HoldableItemInUseLeftHand is MeleeWeapon) == true) { return; }
            Debug.Log("Is Left Hand Holdable Item selected");

            if (!FiringMode || !IsItemEquiped || IsRolling) return;

            //Disable Aiming
            IsAiming = false;
            bool canUseItem = false;

            if (HoldableItemInUseLeftHand is ThrowableItem && ShotDownInput)
            {
                anim.SetTrigger((HoldableItemInUseLeftHand as ThrowableItem).AnimationTriggerParameterName);
                return;
            }

            //Continuous Item Using
            if (HoldableItemInUseLeftHand.ContinuousUseItem)
            {
                if (ShotInput)
                {
                    if (IsRolling == false && IsDriving == false && ArmsWeightIK > 0.7f && HoldableItemInUseLeftHand.CanUseItem)
                    {
                        UseEquipedItem();
                    }
                    else
                    {
                        HoldableItemInUseLeftHand.StopUseItem();
                    }
                }
                else
                {
                    HoldableItemInUseLeftHand.StopUseItem();
                }
            }
            else
            {
                //Sequencial Item Using
                if (ShotInput && HoldableItemInUseLeftHand.IsUsingItem == false)
                {
                    if (IsRolling == false && IsDriving == false && ArmsWeightIK > 0.7f && canUseItem)
                    {
                        UseEquipedItem();
                    }
                    else
                    {
                        HoldableItemInUseLeftHand.StopUseItem();
                    }
                }
                else
                {
                    HoldableItemInUseLeftHand.StopUseItem();
                }

                //Reenable Use item
                HoldableItemInUseLeftHand.StopUseItem();
            }
            if (HoldableItemInUseLeftHand.ContinuousUseItem == false)
            {
                canUseItem = !ShotInput;
            }
        }

        public virtual void UseMeleeWeapons(bool AttackInputDown)
        {
            if (HoldableItemInUseRightHand == null && HoldableItemInUseLeftHand == null) { IsMeleeAttacking = false; return; }

            if (HoldableItemInUseRightHand != null) { if ((HoldableItemInUseRightHand is MeleeWeapon) == false) return; }
            if (HoldableItemInUseLeftHand != null) { if ((HoldableItemInUseLeftHand is MeleeWeapon) == false) return; }


            IsMeleeAttacking = (MeleeWeaponInUseLeftHand != null) ? MeleeWeaponInUseLeftHand.IsUsingItem : false;
            IsMeleeAttacking = (MeleeWeaponInUseRightHand != null) ? MeleeWeaponInUseRightHand.IsUsingItem : false;


            if (AttackInputDown)
            {
                if (MeleeWeaponInUseLeftHand != null && MeleeWeaponInUseRightHand == null)
                {
                    anim.SetTrigger(MeleeWeaponInUseLeftHand.AttackAnimatorParameterName);
                    IsMeleeAttacking = true;
                }
                if (MeleeWeaponInUseRightHand != null)
                {
                    anim.SetTrigger(MeleeWeaponInUseRightHand.AttackAnimatorParameterName);
                    IsMeleeAttacking = true;
                }
            }
        }
        public virtual void UseWeaponRightHand(bool ShotInput, bool ShotInputDown, bool AimInput, bool AimInputDown)
        {
            if ((HoldableItemInUseRightHand is Weapon) == false) { WeaponInUseRightHand = null; return; }

            if (!FiringMode || IsRolling || IsDead || IsDriving || IsReloading)
            {
                IsAiming = false;
                return;
            }

            if (MovementAffectsWeaponAccuracy) WeaponInUseRightHand.ShotErrorProbability += (VelocityMultiplier * WeaponInUseRightHand.Precision) * Time.fixedDeltaTime / (8 * OnMovePrecision);

            bool canUseItem = false;

            // Weapon Using Control
            if (WeaponInUseRightHand.ContinuousUseItem)
            {
                canUseItem = HoldableItemInUseRightHand.CanUseItem;
            }
            else
            {
                canUseItem = ShotInputDown;
            }


            //Aiming

            if (JUGameManager.IsMobileControls)
            {
                if (AimInputDown) IsAiming = !IsAiming;
            }
            else
            {
                if (AimMode == PressAimMode.OnePressToAim && AimInputDown) IsAiming = !IsAiming;

                if (ArmsWeightIK > 0.4f)
                {
                    if (AimMode == PressAimMode.HoldToAim) IsAiming = AimInput;
                }
                else
                {
                    IsAiming = false;
                }
            }
            if (HoldableItemInUseLeftHand != null && HoldableItemInUseRightHand != null) IsAiming = false;


            // >>> Full Auto Shooting (CONTINUOUS Item Use ONLY)
            if (WeaponInUseRightHand.FireMode != Weapon.WeaponFireMode.SemiAuto)
            {
                if (ShotInput && ArmsWeightIK > 0.4f && canUseItem)
                {
                    UseEquipedItem(RightHand: true);
                }
                else
                {
                    WeaponInUseRightHand.StopUseItemDelayed(0.09f);
                }
            }
            else
            {
                // >>> Semi Auto Shooting

                //Shot in normal fire rate
                if (ShotInput && ArmsWeightIK > 0.4f && canUseItem)
                {
                    UseEquipedItem(RightHand: true);
                }
                else
                {
                    WeaponInUseRightHand.StopUseItemDelayed(0.09f);
                }

                //Force shooting out of firerate
                if (ShotInputDown && IsRolling == false && IsDriving == false && ArmsWeightIK > 0.4f && WeaponInUseRightHand.BulletsAmounts > 0 && WeaponInUseRightHand.IsUsingItem == true && WeaponInUseRightHand.CurrentFireRateToShoot > 0.09f)
                {
                    WeaponInUseRightHand.Shot();
                }
            }
        }
        public virtual void UseWeaponLeftHand(bool ShotInput, bool ShotInputDown, bool AimInput, bool AimInputDown)
        {
            if ((HoldableItemInUseLeftHand is Weapon) == false) { WeaponInUseLeftHand = null; return; }

            if (!FiringMode || IsRolling || IsDead || IsDriving || IsReloading)
            {
                IsAiming = false;
                return;
            }

            if (MovementAffectsWeaponAccuracy) WeaponInUseLeftHand.ShotErrorProbability += (VelocityMultiplier * WeaponInUseLeftHand.Precision) * Time.fixedDeltaTime / (8 * OnMovePrecision);

            bool canUseItem = false;

            // Weapon Using Control
            if (WeaponInUseLeftHand.ContinuousUseItem)
            {
                canUseItem = HoldableItemInUseLeftHand.CanUseItem;
            }
            else
            {
                canUseItem = ShotInputDown;
            }

            //Aiming
            if (JUGameManager.IsMobileControls)
            {
                if (AimInput) IsAiming = !IsAiming;
            }
            else
            {
                if (AimMode == PressAimMode.OnePressToAim && AimInputDown) IsAiming = !IsAiming;

                if (ArmsWeightIK > 0.4f)
                {
                    if (AimMode == PressAimMode.HoldToAim) IsAiming = AimInput;
                }
                else
                {
                    IsAiming = false;
                }
            }

            if (HoldableItemInUseLeftHand != null && HoldableItemInUseLeftHand != null) IsAiming = false;

            // >>> Full Auto Shooting (CONTINUOUS Item Use ONLY)
            if (WeaponInUseLeftHand.FireMode != Weapon.WeaponFireMode.SemiAuto)
            {
                if (ShotInput && ArmsWeightIK > 0.4f && canUseItem)
                {
                    UseEquipedItem(RightHand: false);
                }
                else
                {
                    WeaponInUseLeftHand.StopUseItemDelayed(0.09f);
                }
            }
            else
            {
                // >>> Semi Auto Shooting

                //Shot in normal fire rate
                if (ShotInput && ArmsWeightIK > 0.4f && canUseItem)
                {
                    UseEquipedItem(RightHand: false);
                }
                else
                {
                    WeaponInUseLeftHand.StopUseItemDelayed(0.09f);
                }

                //Force shooting out of firerate
                if (ShotInputDown && IsRolling == false && IsDriving == false && ArmsWeightIK > 0.4f && WeaponInUseLeftHand.BulletsAmounts > 0 && WeaponInUseLeftHand.IsUsingItem == true && WeaponInUseLeftHand.CurrentFireRateToShoot > 0.09f)
                {
                    WeaponInUseLeftHand.Shot();
                }
            }

        }


        public virtual void _ThrowCurrentThrowableItem()
        {
            if (HoldableItemInUseRightHand != null)
            {
                if (HoldableItemInUseRightHand is ThrowableItem)
                {
                    if (LookAtPosition == Vector3.zero && MyCamera != null && FiringMode == true)
                    {
                        ThrowableItem item = (HoldableItemInUseRightHand as ThrowableItem);
                        item.DirectionToThrow = transform.InverseTransformDirection(MyCamera.transform.forward);
                        item.ThrowThis(item.ThrowForce, item.ThrowUpForce, item.PositionToThrow, transform.InverseTransformDirection(MyCamera.transform.forward), item.RotationForce);
                    }
                    else
                    {
                        HoldableItemInUseRightHand.UseItem();
                    }
                }
            }
            if (HoldableItemInUseLeftHand != null)
            {
                if (HoldableItemInUseLeftHand is ThrowableItem)
                {
                    if (LookAtPosition == Vector3.zero && MyCamera != null && FiringMode == true)
                    {
                        ThrowableItem item = (HoldableItemInUseLeftHand as ThrowableItem);
                        item.DirectionToThrow = transform.InverseTransformDirection(MyCamera.transform.forward);
                        item.ThrowThis(item.ThrowForce, item.ThrowUpForce, item.PositionToThrow, transform.InverseTransformDirection(MyCamera.transform.forward), item.RotationForce);
                    }
                    else
                    {
                        HoldableItemInUseLeftHand.UseItem();
                    }
                }
            }
        }
        public virtual void _ReloadEquipedWeapons(bool ReloadInput)
        {
            if (WeaponInUseRightHand != null)
            {
                //Reload
                if (ReloadInput && WeaponInUseRightHand.BulletsAmounts < WeaponInUseRightHand.BulletsPerMagazine && WeaponInUseRightHand.TotalBullets > 0)
                {
                    _ReloadWeaponRightHandWeapon();
                }
            }
            if (WeaponInUseLeftHand != null)
            {
                if (ReloadInput && WeaponInUseLeftHand.BulletsAmounts < WeaponInUseLeftHand.BulletsPerMagazine && WeaponInUseLeftHand.TotalBullets > 0)
                {
                    _ReloadWeaponLeftHandWeapon();
                }
            }
        }
        public virtual void _ReloadWeaponRightHandWeapon()
        {
            if (WeaponInUseRightHand == null) return;
            if (WeaponInUseRightHand.BulletsAmounts < WeaponInUseRightHand.BulletsPerMagazine && WeaponInUseRightHand.TotalBullets > 0)
            {
                anim.SetTrigger(AnimatorParameters.ReloadRightWeapon);
                IsReloading = true;
            }
        }
        public virtual void _ReloadWeaponLeftHandWeapon()
        {
            if (WeaponInUseLeftHand == null) return;
            if (WeaponInUseLeftHand.BulletsAmounts < WeaponInUseLeftHand.BulletsPerMagazine && WeaponInUseLeftHand.TotalBullets > 0)
            {
                anim.SetTrigger(AnimatorParameters.ReloadLeftWeapon);
                IsReloading = true;
            }
        }

        public virtual void _AutoReload(bool ShotInput = true)
        {
            if (WeaponInUseLeftHand != null && WeaponInUseRightHand != null)
            {
                if (ShotInput && WeaponInUseRightHand.BulletsAmounts <= 0 && WeaponInUseRightHand.TotalBullets > 0 && WeaponInUseLeftHand.BulletsAmounts <= 0 && WeaponInUseLeftHand.TotalBullets > 0)
                {
                    _ReloadWeaponRightHandWeapon();
                    _ReloadWeaponLeftHandWeapon();
                }
            }
            else
            {


                if (WeaponInUseRightHand != null)
                {
                    if (ShotInput && WeaponInUseRightHand.BulletsAmounts == 0 && WeaponInUseRightHand.TotalBullets > 0)
                    {
                        _ReloadWeaponRightHandWeapon();
                    }
                }
                if (WeaponInUseLeftHand != null)
                {
                    //if (WeaponInUseLeftHand.BulletsAmounts == 0 && WeaponInUseLeftHand.TotalBullets > 0 && IsReloading == true && IsInvoking("_ReloadWeaponLeftHandWeapon") == false)
                    //{
                    //    Invoke("_ReloadWeaponLeftHandWeapon", 0.5f);
                    // }
                    if (ShotInput && WeaponInUseLeftHand.BulletsAmounts == 0 && WeaponInUseLeftHand.TotalBullets > 0)
                    {
                        _ReloadWeaponLeftHandWeapon();
                    }
                }
            }
        }
        public virtual void _Move(float HorizontalInput, float VerticalInput, bool Running)
        {
            HorizontalX = HorizontalInput;
            VerticalY = VerticalInput;
            IsRunning = Running;
        }

        public virtual void _Jump()
        {
            if (IsGrounded == false || IsJumping == true || IsRolling == true || IsDriving == true || CanJump == false || IsProne || IsRagdolled)
            {
                _GetUp();

                return;
            }
            if (anim.GetCurrentAnimatorStateInfo(0).IsName("Prone Free Locomotion BlendTree") ||
                anim.GetCurrentAnimatorStateInfo(0).IsName("CrouchToProne") ||
                anim.GetCurrentAnimatorStateInfo(0).IsName("Prone FireMode BlendTree") ||
                anim.GetCurrentAnimatorStateInfo(0).IsName("Prone To Crouch")) return;

            //Change States
            IsGrounded = false;
            IsJumping = true;
            CanJump = false;
            IsCrouched = false;

            //Add Force
            rb.AddForce(transform.up * 200 * JumpForce, ForceMode.Impulse);
            if (SetRigidbodyVelocity == false)
            {
                rb.AddForce(DirectionTransform.forward * LastVelMult * rb.mass * Speed, ForceMode.Impulse);
                VelocityMultiplier = 0;
            }

            //Disable IsJumping state in 0.3s
            Invoke(nameof(_disablejump), 0.3f);
        }
        public virtual void _NewJumpDelay(float Delay = 0.3f, bool JumpDecreaseSpeed = false)
        {
            //New Jump Delay
            if (CanJump == false && IsJumping == false && IsGrounded == true && IsInvoking(nameof(_enableCanJump)) == false)
            {
                if (JumpDecreaseSpeed) VelocityMultiplier = VelocityMultiplier / 4;
                Invoke(nameof(_enableCanJump), Delay);
            }
        }
        public virtual void _Crouch()
        {
            if (IsGrounded == false || IsDriving == true) return;

            if (IsProne) IsProne = false;
            IsCrouched = true;
        }
        public virtual void _Prone()
        {
            if (IsGrounded == false || IsDriving == true) return;
            IsCrouched = true;
            IsProne = true;
        }
        public virtual void _GetUp()
        {
            if (IsProne)
            {
                IsCrouched = true;
                IsProne = false;
            }
            else
            {
                IsCrouched = false;
                IsProne = false;
            }
        }
        public virtual void _Roll()
        {
            if (IsGrounded == false || IsRolling == true || IsProne || anim.GetBool(AnimatorParameters.Roll)) return;
            anim.SetTrigger(AnimatorParameters.Roll);
            IsRolling = true;
            Invoke(nameof(stopRolling), 1f);
        }
        public virtual void _DoPunch()
        {
            if (AnimatorParameters.Punch != "")
            {
                anim.SetTrigger(AnimatorParameters.Punch);
                IsPunching = true;
            }
        }

        public virtual void DefaultUseOfAllItems(bool ShotInput, bool ShotInputDown = false, bool ReloadInput = false, bool AimInput = false, bool AimInputDown = false, bool MeleeAttackInput = false)
        {
            if (HoldableItemInUseLeftHand != null || HoldableItemInUseRightHand != null)
            {
                UseLeftHandItem(ShotInput, ShotInputDown);
                UseRightHandItem(ShotInput, ShotInputDown);

                if (WeaponInUseRightHand != null)
                {
                    if (WeaponInUseRightHand.AimMode == Weapon.WeaponAimMode.None) { IsAiming = false; AimInput = false; AimInputDown = false; }
                }

                if (RightHandWeightIK > 0.5f) UseWeaponLeftHand(ShotInput, ShotInputDown, AimInput, AimInputDown);
                if (RightHandWeightIK > 0.5f) UseWeaponRightHand(ShotInput, ShotInputDown, AimInput, AimInputDown);

                UseMeleeWeapons(MeleeAttackInput);

                _ReloadEquipedWeapons(ReloadInput);
                _AutoReload();
            }
            else
            {
                if (MeleeAttackInput) _DoPunch();
            }
        }
        public virtual void _AimScope()
        {
            IsAiming = !IsAiming;
        }
        #endregion


        #region Default Animation Events
        public void reloadRightHandWeapon()
        {
            if (WeaponInUseRightHand != null) WeaponInUseRightHand.Reload();

            anim.ResetTrigger(AnimatorParameters.ReloadRightWeapon);
            IsReloading = false;
        }
        public void reloadLeftHandWeapon()
        {
            if (WeaponInUseLeftHand != null) WeaponInUseLeftHand.Reload();

            anim.ResetTrigger(AnimatorParameters.ReloadLeftWeapon);
            IsReloading = false;
        }

        public void emitBulletShell()
        {
            if (WeaponInUseRightHand != null)
            {
                if (WeaponInUseRightHand.BulletCasingPrefab != null)
                {
                    WeaponInUseRightHand.EmitBulletShell();
                }
            }
            if (WeaponInUseLeftHand != null)
            {
                if (WeaponInUseLeftHand.BulletCasingPrefab != null)
                {
                    WeaponInUseLeftHand.EmitBulletShell();
                }
            }
        }

        public void disableMove()
        {
            CanMove = false;
        }
        public void enableMove()
        {
            CanMove = true;
            DisableAllMove = false;
            CanMove = true;
        }
        public void disableRotation()
        {
            CanRotate = false;
        }
        public void enableRotation()
        {
            CanRotate = true;
        }
        public void disableFireModeIK()
        {
            FiringModeIK = false;
        }
        public void enableFireModeIK()
        {
            FiringModeIK = true;
        }
        public void stopRolling()
        {
            CanMove = true;
            IsRolling = false;
            enableFireModeIK();
        }
        public void startRolling()
        {
            IsRolling = true;
            CanMove = false;
            disableFireModeIK();
        }

        #endregion



        #region Invoke(Timed) Functions
        private void _disablejump()
        {
            IsJumping = false;
        }
        private void _disableroll()
        {
            IsRolling = false;
        }
        private void _enableCanJump()
        {
            CanJump = true;
        }
        #endregion


        #region State Functions
        public void DrivingCheck()
        {
            if (DriveVehicleAbility == null) { IsDriving = false; return; }

            IsDriving = DriveVehicleAbility.IsDriving;
            VehicleInArea = DriveVehicleAbility.CurrentVehicle;
        }
        protected void HealthCheck()
        {
            if (CharacterHealth == null) return;

            if (CharacterHealth.Health <= 0 && IsDead == false)
            {
                KillCharacter();
            }

            if (IsDead == false) return;

            CanMove = false;
            IsRunning = false;
            IsCrouched = false;
            IsJumping = false;
            IsGrounded = false;
            IsItemEquiped = false;
            IsRolling = false;
            IsDriving = false;
            UsedItem = false;
            WallAHead = false;
            FiringModeIK = false;
            ResetDefaultLayersWeight();

            coll.isTrigger = true;
            coll.enabled = true;
            rb.isKinematic = true;
            rb.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;

            gameObject.layer = 2;
            transform.position = GetGroundPoint();
        }
        protected void PickUpCheck()
        {
            if (Inventory == null)
            {
                ToPickupItem = false;
                return;
            }
            else
            {
                //if (Inventory.EnablePickup == false) { ToPickupItem = false; return; }
                if (Inventory.ItemToPickUp != null)
                {
                    ToPickupItem = true;
                }
                else
                {
                    if (ToPickupItem == true && Inventory.ItemToPickUp == null && IsInvoking(nameof(DisableToPickUpItemBoolean)) == false)
                    {
                        Invoke(nameof(DisableToPickUpItemBoolean), 0.3f);
                    }
                }
                ToPickupItem = Inventory.ItemToPickUp == null ? false : true;
            }
        }
        private void DisableToPickUpItemBoolean() => ToPickupItem = false;

        public virtual void TakeDamage(float Damage, Vector3 hitPosition = default(Vector3))
        {
            if (CharacterHealth == null)
            {
                CharacterHealth = GetComponent<JUHealth>();
                if (CharacterHealth == null) return;
            }
            CharacterHealth.DoDamage(Damage, hitPosition);
        }
        public virtual void KillCharacter()
        {
            if (CharacterHealth == null)
            {
                Debug.LogWarning("Unable to kill the character as there is no JU Health component attached to it.");
                return;
            }
            //Reset default animator layers
            ResetDefaultLayersWeight();

            //Do ragdoll when Die
            if (RagdollWhenDie == true && Ragdoller != null)
            {
                Ragdoller.State = AdvancedRagdollController.RagdollState.Ragdolled;
                Ragdoller.TimeToGetUp = 900;
            }

            CharacterHealth.Health = 0;
            IsDead = true;
        }
        public virtual void RessurectCharacter()
        {
            if (IsDead == false) return;

            //Reset Camera
            if (FindObjectOfType<TPSCameraController>() != null) { FindObjectOfType<TPSCameraController>().mCamera.transform.localEulerAngles = Vector3.zero; }


            //Get up
            if (Ragdoller != null)
            {
                anim.GetBoneTransform(HumanBodyBones.Hips).SetParent(Ragdoller.HipsParent);
                Ragdoller.State = AdvancedRagdollController.RagdollState.BlendToAnim;
                Ragdoller.TimeToGetUp = 2;
                Ragdoller.BlendAmount = 0;
                Ragdoller.SetActiveRagdoll(false);
            }

            //Enable Movement
            DisableAllMove = false;
            CanMove = true;

            //Reset Health
            if (CharacterHealth != null)
            {
                CharacterHealth.Health = CharacterHealth.MaxHealth;
                CharacterHealth.IsDead = false;
                CharacterHealth.CheckHealthState();
            }
            IsDead = false;

            //Reset Collider
            coll.isTrigger = false;

            //Reset Rigidbody
            rb.useGravity = true;
            rb.isKinematic = false;
            rb.linearVelocity = transform.up * rb.linearVelocity.y;

            //Enable Tps Script
            this.enabled = true;

            //Reset Animator
            anim.enabled = true;
            anim.Play("WalkingBlend", 0);
            anim.SetLayerWeight(1, 0);
            anim.SetLayerWeight(2, 0);
            anim.SetLayerWeight(3, 0);
            anim.SetLayerWeight(4, 0);
            anim.SetLayerWeight(5, 0);


            Debug.Log("Player has respawned");
        }
        #endregion

        #region Item Management
        private bool UsedRightItem;
        public void UseEquipedItem(bool RightHand = true)
        {
            if (HoldableItemInUseRightHand != null && HoldableItemInUseLeftHand == null && RightHand) HoldableItemInUseRightHand.UseItem();

            if (HoldableItemInUseLeftHand != null)
            {
                if (WeaponInUseRightHand == null)
                {
                    if (RightHand == false) HoldableItemInUseLeftHand.UseItem();
                    if (HoldableItemInUseRightHand != null && RightHand == true) HoldableItemInUseRightHand.UseItem();
                }
                else
                {
                    if (WeaponInUseLeftHand != null)
                    {
                        //Debug.Log("2 w");

                        if (WeaponInUseRightHand.CurrentFireRateToShoot >= WeaponInUseRightHand.Fire_Rate && WeaponInUseLeftHand.CurrentFireRateToShoot >= WeaponInUseLeftHand.Fire_Rate)
                        {
                            //WeaponInUseLeftHand.CurrentFireRateToShoot = WeaponInUseLeftHand.Fire_Rate / 2;
                        }
                        //WeaponInUseLeftHand.CurrentFireRateToShoot = WeaponInUseRightHand.CurrentFireRateToShoot;
                        if (RightHand == true && UsedRightItem == false)
                        {
                            HoldableItemInUseRightHand.UseItem();
                            WeaponInUseLeftHand.CurrentFireRateToShoot = 0;
                            UsedRightItem = true;
                        }
                        if (RightHand == false && UsedRightItem == true)
                        {
                            HoldableItemInUseLeftHand.UseItem();
                            UsedRightItem = false;
                        }
                    }
                    else
                    {
                        //Debug.Log("1 w");
                        HoldableItemInUseLeftHand.UseItem();
                        HoldableItemInUseRightHand.UseItem();
                    }
                }
            }

            if (WeaponInUseLeftHand != null)
            {
                if ((WeaponInUseLeftHand.FireMode == Weapon.WeaponFireMode.BoltAction ||
                     WeaponInUseLeftHand.FireMode == Weapon.WeaponFireMode.Shotgun))
                {
                    Invoke("PullWeaponBolt", 0.3f);
                }
            }

            if (WeaponInUseRightHand != null)
            {
                if ((WeaponInUseRightHand.FireMode == Weapon.WeaponFireMode.BoltAction ||
                     WeaponInUseRightHand.FireMode == Weapon.WeaponFireMode.Shotgun))
                {
                    Invoke("PullWeaponBolt", 0.4f);
                }
            }
        }


        /// <summary>
        ///[0] No Wielding Item     [1] Right Hand Wielding     [2] Left Hand Wielding     [3] Dual Wielding
        /// </summary>
        /// <returns></returns>
        public int GetWieldingID()
        {
            int id = -1;

            if (HoldableItemInUseRightHand == null && HoldableItemInUseLeftHand == null) { id = 0; }

            if (HoldableItemInUseRightHand != null && HoldableItemInUseLeftHand == null) { id = 1; }

            if (HoldableItemInUseRightHand == null && HoldableItemInUseLeftHand != null) { id = 2; }

            if (HoldableItemInUseRightHand != null && HoldableItemInUseLeftHand != null) { id = 3; }

            return id;
        }
        public void SwitchToNextItem(bool RightHand = true)
        {
            SwitchItens(SwitchDirection.Forward, RightHand);
        }
        public void SwitchToPreviousItem(bool RightHand = true)
        {
            SwitchItens(SwitchDirection.Backward, RightHand);
        }
        private JUHoldableItem oldDualItem;
        public void SwitchToItem(int id = -1, bool RightHand = true)
        {
            if (Inventory == null) return;
            //Disable Aiming State and Shot State
            IsAiming = false; UsedItem = false;
            if (JUPauseGame.IsPaused || IsReloading || IsDead || IsDriving || IsRagdolled) return;

            //if you have an item forcing double wielding before switching items do the left hand item switch
            if (oldDualItem != null)
            {
                Inventory.SwitchToItem(-1, false);
                oldDualItem.gameObject.SetActive(false);
                oldDualItem = null;
            }
            //Switch
            Inventory.SwitchToItem(id, RightHand);

            //Get IDs
            CurrentItemIDRightHand = Inventory.CurrentRightHandItemID;
            CurrentItemIDLeftHand = Inventory.CurrentLeftHandItemID;

            //Get Holdable Itens
            HoldableItemInUseLeftHand = Inventory.HoldableItemInUseInLeftHand;
            HoldableItemInUseRightHand = Inventory.HoldableItemInUseInRightHand;

            //Get Weapon
            WeaponInUseLeftHand = Inventory.WeaponInUseInLeftHand;
            WeaponInUseRightHand = Inventory.WeaponInUseInRightHand;

            //Get Melee Weapon
            MeleeWeaponInUseRightHand = Inventory.MeleeWeaponInUseInRightHand;
            MeleeWeaponInUseLeftHand = Inventory.MeleeWeaponInUseInLeftHand;

            IsItemEquiped = Inventory.IsItemSelected;
            IsDualWielding = Inventory.IsDualWielding;

            //Force Dual Wielding
            if (RightHand == true)
            {
                if (HoldableItemInUseRightHand != null)
                {
                    if (HoldableItemInUseRightHand.ForceDualWielding && HoldableItemInUseRightHand.DualItemToWielding != null)
                    {
                        SwitchToItem(HoldableItemInUseRightHand.DualItemToWielding.ItemSwitchID, false);
                        oldDualItem = HoldableItemInUseRightHand.DualItemToWielding;
                    }
                }
                else
                {
                    SwitchToItem(-1, false);
                    oldDualItem = null;
                }
            }
            else
            {
                if (HoldableItemInUseLeftHand != null)
                {
                    if (HoldableItemInUseLeftHand.ForceDualWielding && HoldableItemInUseLeftHand.DualItemToWielding != null)
                    {
                        SwitchToItem(HoldableItemInUseLeftHand.DualItemToWielding.ItemSwitchID, true);
                        oldDualItem = HoldableItemInUseLeftHand.DualItemToWielding;
                    }
                }
                else
                {
                    oldDualItem = null;
                }
            }

            if (HoldableItemInUseRightHand != null || HoldableItemInUseLeftHand != null)
            {
                IsWeaponSwitching = true;
                WeaponSwitchingCurrentTime = 0;
                PlayWeaponSwitchAnimation();

                //IK
                ArmsWeightIK = 0;
                if (CurrentItemIDRightHand != -1) BothArmsLayerWeight = 0;
            }

        }
        public enum SwitchDirection { Forward, Backward }
        public virtual void SwitchItens(SwitchDirection Direction, bool RightHand = true)
        {
            //Disable Aiming State and Shot State
            IsAiming = false; UsedItem = false;

            if (JUPauseGame.IsPaused || IsReloading || IsReloading || IsDead || IsDriving || IsRagdolled || DisableAllMove) { return; }



            switch (Direction)
            {
                case SwitchDirection.Forward:
                    if (RightHand) CurrentItemIDRightHand = Inventory.GetNextUnlockedItemID(CurrentItemIDRightHand); else CurrentItemIDLeftHand = Inventory.GetNextUnlockedItemID(CurrentItemIDLeftHand, transform, false);
                    break;
                case SwitchDirection.Backward:
                    if (RightHand) CurrentItemIDRightHand = Inventory.GetPreviousUnlockedItemID(CurrentItemIDRightHand); else CurrentItemIDLeftHand = Inventory.GetPreviousUnlockedItemID(CurrentItemIDLeftHand, transform, false);
                    break;
            }


            SwitchToItem(RightHand ? CurrentItemIDRightHand : CurrentItemIDLeftHand, RightHand);
        }

        protected virtual void PlayWeaponSwitchAnimation()
        {
            if (HoldableItemInUseRightHand != null)
            {
                if (HoldableItemInUseRightHand.PushItemFrom == JUHoldableItem.ItemSwitchPosition.Back)
                {
                    anim.Play("Weapon Switch Back", 5, 0);
                }
                else
                {
                    anim.Play("Weapon Switch Hips", 5, 0);
                }
            }
        }
        public virtual void PullWeaponBolt()
        {
            if (WeaponInUseRightHand == null) return;

            if ((WeaponInUseRightHand.FireMode == Weapon.WeaponFireMode.BoltAction) && WeaponInUseRightHand.IsUsingItem == true)
            {
                IsAiming = false;

                anim.SetTrigger(AnimatorParameters.PullWeaponSlider);
            }
        }

        #endregion

        #region [ IK ] Inverse Kinematics Utilities Functions
        public void DoHandPositioningNoSmoothing()
        {
            IKPositionLeftHand.position = LeftHandIKPositionTarget.position;
            IKPositionRightHand.position = RightHandIKPositionTarget.position;

            IKPositionLeftHand.rotation = LeftHandIKPositionTarget.rotation;
            IKPositionRightHand.rotation = RightHandIKPositionTarget.rotation;
        }
        public void SmoothRightHandPosition(float Speed = 8)
        {
            //Debug.Log("Left hand = " + HoldableItemInUseLeftHand);
            //Debug.Log("Right Hand = " + HoldableItemInUseRightHand);

            // If I DON'T have an item in my left hand
            if (HoldableItemInUseLeftHand == null)
            {
                //Set Right Hand Parent
                IKPositionRightHand.parent = transform;

                if (HoldableItemInUseRightHand != null)
                {
                    //Get target transformations
                    Quaternion rightHandRotation = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseRightHand.ItemWieldPositionID].rotation;
                    Vector3 rightHandPosition = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseRightHand.ItemWieldPositionID].position;

                    //Set Right Hand IK Target Position
                    SetRightHandIKPosition(rightHandPosition, rightHandRotation);

                    //Smooth Right Hand Transformation
                    IKPositionRightHand.position = IsAiming ? rightHandPosition : Vector3.Lerp(IKPositionRightHand.position, RightHandIKPositionTarget.position, Speed * Time.deltaTime);
                    IKPositionRightHand.rotation = IsAiming ? rightHandRotation : Quaternion.Lerp(IKPositionRightHand.rotation, RightHandIKPositionTarget.rotation, Speed * Time.deltaTime);
                }
            }
            else
            {
                // If i HAVE a item in the left hand
                if (HoldableItemInUseLeftHand.OppositeHandPosition != null && HoldableItemInUseRightHand == null)
                {
                    //Set Right Hand Parent
                    IKPositionRightHand.parent = HoldableItemInUseLeftHand.OppositeHandPosition.transform;
                    if (IKPositionRightHand.position != HoldableItemInUseLeftHand.OppositeHandPosition.transform.position ||
                        RightHandIKPositionTarget.position != HoldableItemInUseLeftHand.OppositeHandPosition.transform.position)
                    {
                        IKPositionRightHand.position = HoldableItemInUseLeftHand.OppositeHandPosition.transform.position;
                        IKPositionRightHand.rotation = HoldableItemInUseLeftHand.OppositeHandPosition.rotation;
                        RightHandIKPositionTarget.position = HoldableItemInUseLeftHand.OppositeHandPosition.transform.position;
                        RightHandIKPositionTarget.rotation = HoldableItemInUseLeftHand.OppositeHandPosition.rotation;
                    }
                }
                else
                {
                    //Set Right Hand Parent
                    IKPositionRightHand.parent = transform;

                    if (HoldableItemInUseRightHand != null)
                    {
                        //Get target transformations
                        Quaternion rightHandRotation = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseRightHand.ItemWieldPositionID].rotation;
                        Vector3 rightHandPosition = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseRightHand.ItemWieldPositionID].position;

                        //Set Right Hand IK Target Position
                        SetRightHandIKPosition(rightHandPosition, rightHandRotation);

                        //Smooth Right Hand Transformation
                        IKPositionRightHand.position = Vector3.Lerp(IKPositionRightHand.position, RightHandIKPositionTarget.position, Speed * Time.deltaTime);
                        IKPositionRightHand.rotation = Quaternion.Lerp(IKPositionRightHand.rotation, RightHandIKPositionTarget.rotation, Speed * Time.deltaTime);
                    }
                }
            }
            if (IKPositionRightHand.parent != null) RightHandIKPositionTarget.parent = IKPositionRightHand.parent;
        }
        public void SmoothLeftHandPosition(float Speed = 8)
        {
            //Se eu NÃO tenho um item na mão direita
            if (HoldableItemInUseRightHand == null)
            {
                //Set Left Hand Parent
                IKPositionLeftHand.parent = transform;

                if (HoldableItemInUseLeftHand != null)
                {
                    //Get target transformations
                    Quaternion leftHandRotation = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseLeftHand.ItemWieldPositionID].rotation;
                    Vector3 lefttHandPosition = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseLeftHand.ItemWieldPositionID].position;

                    //Set Left Hand IK Target Position
                    SetLeftHandIKPosition(lefttHandPosition, leftHandRotation);

                    //Smooth Left Hand Transformation
                    IKPositionLeftHand.position = Vector3.Lerp(IKPositionLeftHand.position, LeftHandIKPositionTarget.position, Speed * Time.deltaTime);
                    IKPositionLeftHand.rotation = Quaternion.Lerp(IKPositionLeftHand.rotation, LeftHandIKPositionTarget.rotation, Speed * Time.deltaTime);
                }
            }
            else
            {
                //Se eu TENHO um item na mão direita
                if (HoldableItemInUseRightHand.OppositeHandPosition != null && HoldableItemInUseLeftHand == null)
                {
                    //Set Left Hand Parent
                    IKPositionLeftHand.parent = HoldableItemInUseRightHand.OppositeHandPosition.transform;
                    if (IKPositionLeftHand.position != HoldableItemInUseRightHand.OppositeHandPosition.transform.position ||
                        LeftHandIKPositionTarget.position != HoldableItemInUseRightHand.OppositeHandPosition.transform.position)
                    {
                        IKPositionLeftHand.position = HoldableItemInUseRightHand.OppositeHandPosition.transform.position;
                        IKPositionLeftHand.rotation = HoldableItemInUseRightHand.OppositeHandPosition.rotation;
                        LeftHandIKPositionTarget.position = HoldableItemInUseRightHand.OppositeHandPosition.transform.position;
                        LeftHandIKPositionTarget.rotation = HoldableItemInUseRightHand.OppositeHandPosition.rotation;
                    }
                }
                else
                {
                    //Set Left Hand Parent
                    IKPositionLeftHand.parent = transform;

                    if (HoldableItemInUseLeftHand != null)
                    {
                        //Get target transformations
                        Quaternion leftHandRotation = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseLeftHand.ItemWieldPositionID].rotation;
                        Vector3 lefttHandPosition = WeaponHoldingPositions.WeaponPositionTransform[HoldableItemInUseLeftHand.ItemWieldPositionID].position;

                        //Set Left Hand IK Target Position
                        SetLeftHandIKPosition(lefttHandPosition, leftHandRotation);

                        //Smooth Left Hand Transformation
                        IKPositionLeftHand.position = Vector3.Lerp(IKPositionLeftHand.position, LeftHandIKPositionTarget.position, Speed * Time.deltaTime);
                        IKPositionLeftHand.rotation = Quaternion.Lerp(IKPositionLeftHand.rotation, LeftHandIKPositionTarget.rotation, Speed * Time.deltaTime);
                    }
                }
            }
            if (IKPositionLeftHand.parent != null) LeftHandIKPositionTarget.parent = IKPositionLeftHand.parent;
        }

        public void SetRightHandWieldingPositionAndSpace(Transform targetTransform, Transform parent)
        {
            RightHandIKPositionTarget.parent = parent;
            if (targetTransform != null)
            {
                if (RightHandIKPositionTarget.position != targetTransform.position)
                {
                    RightHandIKPositionTarget.position = targetTransform.position;
                    RightHandIKPositionTarget.rotation = targetTransform.rotation;
                }
            }
            IKPositionRightHand.parent = parent;
        }
        public void SetLeftHandWieldingPositionAndSpace(Transform targetTransform, Transform parent)
        {
            LeftHandIKPositionTarget.parent = parent;
            if (targetTransform != null)
            {
                if (LeftHandIKPositionTarget.position != targetTransform.position)
                {
                    LeftHandIKPositionTarget.position = targetTransform.position;
                    LeftHandIKPositionTarget.rotation = targetTransform.rotation;
                }
            }
            IKPositionRightHand.parent = parent;
        }


        public void SetRightHandIKPosition(Vector3 Position, Quaternion Rotation)
        {
            RightHandIKPositionTarget.position = Position;
            RightHandIKPositionTarget.rotation = Rotation;
        }
        public void SetLeftHandIKPosition(Vector3 Position, Quaternion Rotation)
        {
            LeftHandIKPositionTarget.position = Position;
            LeftHandIKPositionTarget.rotation = Rotation;
        }

        public void RightHandToRespectiveIKPosition(float IKWeight, float ElbowAdjustWeight = 0)
        {
            if (IKWeight == 0) return;
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, IKWeight);

            anim.SetIKPosition(AvatarIKGoal.RightHand, IKPositionRightHand.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, IKPositionRightHand.rotation);

            if (ElbowAdjustWeight == 0) return;
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, ElbowAdjustWeight);
            Vector3 hintPos = PivotItemRotation.transform.position + PivotItemRotation.transform.right * 2 + PivotItemRotation.transform.forward * 1 - PivotItemRotation.transform.up * 3f;
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, hintPos);
        }
        public void LeftHandToRespectiveIKPosition(float IKWeight, float ElbowAdjustWeight = 0)
        {
            if (IKWeight == 0) return;
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, IKWeight);

            anim.SetIKPosition(AvatarIKGoal.LeftHand, IKPositionLeftHand.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, IKPositionLeftHand.rotation);

            if (ElbowAdjustWeight == 0) return;
            anim.SetIKHintPositionWeight(AvatarIKHint.LeftElbow, ElbowAdjustWeight);
            Vector3 hintPos = PivotItemRotation.transform.position - PivotItemRotation.transform.right * 2 + PivotItemRotation.transform.forward * 1 - PivotItemRotation.transform.up * 3f;
            anim.SetIKHintPosition(AvatarIKHint.LeftElbow, hintPos);
        }


        public void LookAtIK(Vector3 position, float IKWeight = 1, float BodyIKWeight = 0.5f, float HeadIKWeight = 1)
        {
            anim.NormalLookAt(position, HeadIKWeight, BodyIKWeight, IKWeight);
        }

        [HideInInspector] protected Transform SpineLookATTransform;
        [HideInInspector] protected Quaternion OriginalSpineRotation;
        [HideInInspector] protected Vector3 SmoothedSpineLookAtPosition, TargetSpineLookAtPosition;

        public void SpineLookAt(Vector3 position, float GlobalWeight, float WorldUpWeight = 0.2f, float SpineInclination = 0, float SmoothTime = 5f)
        {
            TargetSpineLookAtPosition = position;
            SmoothedSpineLookAtPosition = Vector3.Lerp(SmoothedSpineLookAtPosition, TargetSpineLookAtPosition, SmoothTime * Time.deltaTime);

            if (SpineLookATTransform == null)
            {
                SpineLookATTransform = new GameObject("SpineIKDirection").transform;
                //SpineLookATTransform.hideFlags = HideFlags.HideInHierarchy;
                SpineLookATTransform.position = anim.GetBoneTransform(HumanBodyBones.Spine).position;
                SpineLookATTransform.rotation = anim.GetBoneTransform(HumanBodyBones.Spine).rotation;
                SpineLookATTransform.SetParent(anim.GetBoneTransform(HumanBodyBones.Spine).parent);
            }
            else
            {
                SpineLookATTransform.LookAt(position, Vector3.Lerp(anim.GetBoneTransform(HumanBodyBones.Spine).up + anim.GetBoneTransform(HumanBodyBones.Spine).right * GlobalWeight * SpineInclination, Vector3.up, WorldUpWeight));
                Quaternion LocalSpineRotation = Quaternion.Lerp(OriginalSpineRotation, SpineLookATTransform.localRotation, GlobalWeight);
                if (SpineInclination == 0) LocalSpineRotation.z = OriginalSpineRotation.z;
                anim.SetBoneLocalRotation(HumanBodyBones.Spine, LocalSpineRotation);
            }
        }
        #endregion
    }


    [System.Serializable]
    public class JUAnimatorParameters
    {
        [Header("Default Layers IDs")]
        public int _BaseLayerIndex = 0;
        public int _LegsLayerIndex = 1;
        public int _RightArmLayerIndex = 2;
        public int _LeftArmLayerIndex = 3;
        public int _BothArmsLayerIndex = 4;
        public int _SwitchWeaponLayerIndex = 5;

        [Header("Default Parameters Names")]
        public string Moving = "Moving";
        public string Running = "Running";
        public string Speed = "Speed";
        public string HorizontalInput = "Horizontal";
        public string VerticalInput = "Vertical";
        public string IdleTurn = "IdleTurn";
        public string MovingTurn = "MovingTurn";
        public string Grounded = "Grounded";
        public string Jumping = "Jumping";
        public string ItemEquiped = "ItemEquiped";
        public string FireMode = "FireMode";
        public string Crouch = "Crouched";
        public string Prone = "Prone";
        public string Driving = "Driving";
        public string Dying = "Die";
        public string Punch = "Punch";
        public string Roll = "Roll";
        public string ReloadRightWeapon = "ReloadRightWeapon";
        public string ReloadLeftWeapon = "ReloadLeftWeapon";
        public string PullWeaponSlider = "PullWeaponSlider";
        public string LandingIntensity = "LandingIntensity";
        public string ItemWieldingRightHandPoseID = "ItemWieldingRightHandPoseID";
        public string ItemWieldingLeftHandPoseID = "ItemWieldingLeftHandPoseID";

        public string ItemsWieldingIdentifier = "ItemsWieldingIdentifier";
    }

}