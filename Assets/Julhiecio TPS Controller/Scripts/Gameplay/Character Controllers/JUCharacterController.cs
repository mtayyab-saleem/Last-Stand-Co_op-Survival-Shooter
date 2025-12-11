using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using JUTPS.ItemSystem;
using JUTPS.PhysicsScripts;
using JUTPS.CharacterBrain;
using JUTPS.ExtendedInverseKinematics;
using JUTPS.JUInputSystem;

namespace JUTPS
{
    [AddComponentMenu("JU TPS/Third Person System/Character Controllers/JU Character Controller")]
    [RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(CapsuleCollider))]
    public class JUCharacterController : JUCharacterBrain
    {
        [Header("Controller Options")]
        public bool UseDefaultControllerInput = true;
        public bool AutoRun = true;
        public bool WalkOnRunButton = true;

        public bool DecreaseSpeedOnJump = false;
        public bool BlockVerticalInput;
        public bool BlockHorizontalInput;
        public bool BlockFireModeOnCursorVisible = false;
        public bool BlockFireModeOnPunching = true;
        public bool EnablePunchAttacks = true;
        public bool EnableRoll = true;
        

        [Header("Physical Damage")]
        public bool PhysicalDamage = true;
        public bool DoRagdollPhysicalDamage = true;
        public float PhysicalDamageStartAt = 25;
        public float PhysicalDamageMultiplier = 0.8f;
        public float RagdollStartAtDamage = 10;
        public string[] PhysicalDamageIgnoreTags = new string[] { "Player", "Enemy", "Bones", "Wall", "Bullet" };
        void FixedUpdate()
        {
            if (IsDead == true || DisableAllMove == true || JUPauseGame.IsPaused) { return; }
            Movement();
            SlopeSlide();
            StepCorrectionMovement();
        }

        void Update()
        {
            if (JUPauseGame.IsPaused) return;

            FootPlacementIKController();

            GroundCheck();
            HealthCheck();

            SetAnimatorParameters();
            SetupDefaultLayersWeights();

            if (IsDead) return;

            DrivingCheck();
            WallAHeadCheck();

            if (DisableAllMove == false)
            {
                ControllerInputs();

                StepCorrectionCalculation();

                Rotate(HorizontalX, VerticalY);

                RefreshItemAimRotationPivot();
                WieldingIKWeightController();

                WeaponOrientator();
            }
            else
            {
                LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, 0, 8 * Time.deltaTime);
            }

            Events.UpdateRuntimeEventsCallbacks(this);
        }


        #region INPUTS
        public virtual void ControllerInputs()
        {
            if (UseDefaultControllerInput == false) return;


            bool ShotInput = JUInput.GetButton(JUInput.Buttons.ShotButton);
            //bool ShotInputUp = JUInput.GetButtonUp(JUInput.Buttons.ShotButton);
            bool ShotInputDown = JUInput.GetButtonDown(JUInput.Buttons.ShotButton);
            //bool PunchInputDown = EnablePunchAttacks ? JUInput.GetButtonDown(JUInput.Buttons.PunchButton) : false;
            bool ReloadInput = JUInput.GetButtonDown(JUInput.Buttons.ReloadButton);
            bool AimInput = JUInput.GetButton(JUInput.Buttons.AimingButton);
            bool AimInputDown = JUInput.GetButtonDown(JUInput.Buttons.AimingButton);

            FireModeTimer(ShotInput, AimInput);

            if (!BlockHorizontalInput) HorizontalX = JUInput.GetAxis(JUInput.Axis.MoveHorizontal);
            if (!BlockVerticalInput) VerticalY = JUInput.GetAxis(JUInput.Axis.MoveVertical);

            HorizontalX = Mathf.Clamp(HorizontalX, -1, 1);
            VerticalY = Mathf.Clamp(VerticalY, -1, 1);


            //Crouch
            if (JUInput.GetButtonDown(JUInput.Buttons.CrouchButton))
            {
                if (IsRunning && AutoRun == true)
                {
                    IsRunning = false; ReachedMaxSprintSpeed = false; CanSprint = true; CurrentSprintSpeedIntensity = 0;
                }

                if (IsCrouched == false || IsProne == true)
                {
                    _Crouch();
                }
                else
                {
                    _GetUp();
                }
            }

            //Prone
            if (JUInput.GetButtonDown(JUInput.Buttons.ProneButton))
            {
                if (IsProne == false)
                {
                    _Prone();
                }
                else
                {
                    _GetUp();
                    _GetUp();
                }
            }

            // Prone Get Up
            if (MaxWalkableAngle > 0 && (GroundAngle > MaxWalkableAngle / 1.5f && IsProne))
            {
                _GetUp();
            }

            //Get Up
            if (IsCrouched == true && JUInput.GetButton(JUInput.Buttons.RunButton))
            {
                _GetUp();
            }

            //Jump
            if (JUInput.GetButton(JUInput.Buttons.JumpButton) && IsJumping == false)
            {
                _Jump();
            }
            //New Jump Delay
            _NewJumpDelay(0.2f, DecreaseSpeedOnJump);


            //Roll
            if (JUInput.GetButtonDown(JUInput.Buttons.RollButton) && EnableRoll)
            {
                _Roll();
            }

            //Running
            if (JUInput.GetButton(JUInput.Buttons.RunButton))
            {
                if (WalkOnRunButton)
                {
                    IsRunning = false;
                }
                else
                {
                    IsRunning = true;
                }
                if (SprintingSkill)
                {
                    //Force Sprinting
                    if (SprintOnRunButton == true && FiringMode == false && IsCrouched == false && CanSprint && (Mathf.Abs(HorizontalX) > 0.5f || Mathf.Abs(VerticalY) > 0.5f))
                    {
                        IsRunning = true;
                        IsSprinting = true;
                        CanSprint = true;
                    }
                }
            }
            else
            {
                IsRunning = false;
                if (SprintingSkill == true && SprintOnRunButton == true && IsCrouched == false && CanSprint)
                {
                    IsSprinting = false;
                }
            }

            //Start sprinting impulse
            //if (JUInput.GetButtonDown(JUInput.Buttons.RunButton) && AutoRun && SprintOnRunButton && FiringMode == false)
            //{
            //CurrentSprintSpeedIntensity = 6;
            //Debug.Log("Start Sprinting");
            //}
            //Force stop sprinting
            if (JUInput.GetButtonUp(JUInput.Buttons.RunButton) && SprintOnRunButton == true)
            {
                IsSprinting = false;
            }

            //Infinite Sprinting
            if (SprintingSkill && IsSprinting == true && UnlimitedSprintDuration && FiringMode == false) ReachedMaxSprintSpeed = false;



            //Auto Run
            if (WalkOnRunButton)
            {
                if (AutoRun && JUInput.GetButton(JUInput.Buttons.RunButton) == false && IsCrouched == false)
                {
                    if (Mathf.Abs(HorizontalX) > 0.5f || Mathf.Abs(VerticalY) > 0.5f)
                    {
                        IsRunning = true;
                    }
                }
            }
            else
            {
                if (AutoRun && IsCrouched == false)
                {
                    if (Mathf.Abs(HorizontalX) > 0.5f || Mathf.Abs(VerticalY) > 0.5f)
                    {
                        IsRunning = true;
                    }
                }
            }

            //Mobile Run Button Auto-Run
            if (JUGameManager.IsMobileControls && BlockVerticalInput == false)
            {
                if (JUInput.GetButton(JUInput.Buttons.RunButton) && JUInput.GetAxis(JUInput.Axis.MoveHorizontal) == 0 && JUInput.GetAxis(JUInput.Axis.MoveVertical) == 0)
                {
                    if (SprintOnRunButton)
                    {
                        IsSprinting = true;
                        if (UnlimitedSprintDuration) ReachedMaxSprintSpeed = false;
                    }

                    IsRunning = true;
                    IsCrouched = false;
                    IsMoving = true;
                    VerticalY = Mathf.Lerp(VerticalY, 1f, 8 * Time.deltaTime);

                }
            }

            //Disable Aiming if isnt in Fire Mode
            if (!FiringMode) IsAiming = false;

            //Block firemod if cursor isnot visible
            if (Cursor.visible == true && JUGameManager.IsMobileControls == false && BlockFireModeOnCursorVisible) { return; }

            //All Items Using. Tags: Weapon, Melee, Gun, Shot, Reload, Aim
            if (Inventory != null)
            {
                DefaultUseOfAllItems(ShotInput, ShotInputDown, Inventory.IsPickingItem ? false : ReloadInput, AimInput, AimInputDown, EnablePunchAttacks ? ShotInputDown : false);
            }
            else
            {
                DefaultUseOfAllItems(ShotInput, ShotInputDown, ReloadInput, AimInput, AimInputDown, EnablePunchAttacks ? ShotInputDown : false);
            }
            // >>> Firing Mode Trigger
            if ((ShotInput || AimInput) && FiringMode == false && IsRolling == false && IsDriving == false && IsReloading == false && LocomotionMode != MovementMode.JuTpsClassic)
            {
                if ((BlockFireModeOnPunching && IsPunching) == false)
                {
                    FiringModeIK = true;
                    FiringMode = true;
                }
            }
            if (BlockFireModeOnPunching && (IsPunching || !IsItemEquiped))
            {
                FiringMode = false;
                FiringModeIK = false;
            }

            // >>> Force Block Firing Mode
            if (HoldableItemInUseRightHand) { if (HoldableItemInUseRightHand.BlockFireMode) { FiringMode = false; FiringModeIK = false; } }
        }
        #endregion




        #region ANIMATOR
        protected virtual void SetupDefaultLayersWeights()
        {
            SetDefaultAnimatorsLayersWeight(AnimatorParameters, LegsLayerWeight, RightArmLayerWeight, LeftArmLayerWeight, BothArmsLayerWeight, WeaponSwitchLayerWeight);

            if (IsMeleeAttacking)
            {
                return;
            }

            // >>> Firing Mode Legs Animation Weight
            bool LegMotionEnabledCondition = (FiringMode == true && IsRolling == false && IsDriving == false && DisableAllMove == false);
            LegsLayerWeight = Mathf.Lerp(LegsLayerWeight, (LegMotionEnabledCondition) ? 1 : 0, 5 * Time.deltaTime);
            //Debug.Log("Can use leg layer: " + LegMotionEnabledCondition);

            //Reloading Condition
            bool isReloading = (IsReloading || anim.GetCurrentAnimatorStateInfo(AnimatorParameters._BothArmsLayerIndex).IsName("Reload Right Hand Weapon")
                                            || anim.GetCurrentAnimatorStateInfo(AnimatorParameters._BothArmsLayerIndex).IsName("Reload Left Hand Weapon"));

            // >>> Righ Arm Layer Weight
            if (HoldableItemInUseRightHand != null)
            {
                bool EnabledCondition = (!IsDriving && !IsRolling && !IsReloading);

                if (HoldableItemInUseRightHand.HoldPose != JUHoldableItem.ItemHoldingPose.Free)
                {
                    RightArmLayerWeight = Mathf.MoveTowards(RightArmLayerWeight, EnabledCondition ? 0.8f : 0, 2 * Time.deltaTime);
                }
                else
                {
                    RightArmLayerWeight = Mathf.MoveTowards(RightArmLayerWeight, 0.0f, 2 * Time.deltaTime);
                }
            }
            else
            {
                RightArmLayerWeight = Mathf.MoveTowards(RightArmLayerWeight, 0, 2f * Time.deltaTime);
            }

            // >>> Left Arm Layer Weight
            if (HoldableItemInUseLeftHand != null)
            {
                bool EnabledCondition = (!IsDriving && !IsRolling && !IsReloading);
                LeftArmLayerWeight = Mathf.MoveTowards(LeftArmLayerWeight, EnabledCondition ? 0.8f : 0, 2 * Time.deltaTime);
            }
            else
            {
                LeftArmLayerWeight = Mathf.MoveTowards(LeftArmLayerWeight, 0, 2f * Time.deltaTime);
            }

            bool LeftHandWielding = HoldableItemInUseLeftHand == null ? false : true;
            bool RightHandWielding = HoldableItemInUseRightHand == null ? false : true;

            // >>> Both Arms Layer Weight
            if ((LeftHandWielding || RightHandWielding))
            {
                // Debug.Log("LW: " + LeftHandWielding + " | RW: " + RightHandWielding);

                //Right Hand (!= | ==)
                if (HoldableItemInUseRightHand != null && HoldableItemInUseLeftHand == null)
                {
                    if (HoldableItemInUseRightHand.OppositeHandPosition != null || isReloading)
                    {
                        BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, 1, 2f * Time.deltaTime);
                    }
                    else
                    {
                        BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, 0, 2f * Time.deltaTime);
                    }
                }
                //Left Hand ( == | !=)
                if (HoldableItemInUseRightHand == null && HoldableItemInUseLeftHand != null)
                {
                    if (HoldableItemInUseLeftHand.OppositeHandPosition != null || isReloading)
                    {
                        BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, isReloading ? 1 : 0, 2f * Time.deltaTime);
                    }
                    else
                    {
                        BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, 0, 2f * Time.deltaTime);
                    }
                }
                //Dual ( != | != )
                if (HoldableItemInUseRightHand != null && HoldableItemInUseLeftHand != null)
                {
                    BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, isReloading ? 1 : 0, 2f * Time.deltaTime);
                }
            }
            else
            {
                BothArmsLayerWeight = Mathf.MoveTowards(BothArmsLayerWeight, isReloading ? 1 : 0, 2f * Time.deltaTime);
            }
            if (isReloading)
            {
                BothArmsLayerWeight = 1;
            }

            if (IsWeaponSwitching)
            {
                FiringModeIK = false;
                WeaponSwitchLayerWeight = Mathf.MoveTowards(WeaponSwitchLayerWeight, 1, 10 * Time.deltaTime);
            }
            else
            {
                WeaponSwitchLayerWeight = Mathf.MoveTowards(WeaponSwitchLayerWeight, 0, 3 * Time.deltaTime);
            }
        }
        public virtual void SetAnimatorParameters()
        {
            //Get World Space blend tree values from horizontal and vertical inputs
            Vector3 CorrectBlendTreeValues = WordSpaceToBlendTreeSpace(GetLookPosition(), DirectionTransform);
            //CorrectBlendTreeValues.x = (Vector3.Dot(transform.up, Vector3.up) > -0.3f ? 1 : -1) * CorrectBlendTreeValues.x;
            float VY = 0;
            float HX = 0;

            if (FiringMode)
            {
                //Different movement on mobile to avoid joystick bugs. 
                if (JUGameManager.IsMobileControls)
                {
                    VY = Mathf.MoveTowards(anim.GetFloat(AnimatorParameters.VerticalInput), 8 * VelocityMultiplier * CorrectBlendTreeValues.z, 8 * Time.deltaTime);
                    HX = Mathf.MoveTowards(anim.GetFloat(AnimatorParameters.HorizontalInput), 8 * VelocityMultiplier * CorrectBlendTreeValues.x, 8 * Time.deltaTime);
                }
                else
                {
                    VY = Mathf.Lerp(anim.GetFloat(AnimatorParameters.VerticalInput), 3 * VelocityMultiplier * CorrectBlendTreeValues.z, 4 * Time.deltaTime);
                    HX = Mathf.Lerp(anim.GetFloat(AnimatorParameters.HorizontalInput), 3 * VelocityMultiplier * CorrectBlendTreeValues.x, 4 * Time.deltaTime);
                }
            }

            //Set health parameters
            if (AnimatorParameters.Dying != "") anim.SetBool(AnimatorParameters.Dying, IsDead);

            //Set move direction parameters
            if (AnimatorParameters.VerticalInput != "") anim.SetFloat(AnimatorParameters.VerticalInput, VY);
            if (AnimatorParameters.HorizontalInput != "") anim.SetFloat(AnimatorParameters.HorizontalInput, HX);

            //Set velocity parameter
            if (AnimatorParameters.Speed != "") anim.SetFloat(AnimatorParameters.Speed, VelocityMultiplier);

            //Set locomotion parameters
            if (AnimatorParameters.Moving != "") anim.SetBool(AnimatorParameters.Moving, IsMoving);
            if (AnimatorParameters.Running != "") anim.SetBool(AnimatorParameters.Running, IsRunning);

            //Ungrounded parameters
            if (AnimatorParameters.Grounded != "") anim.SetBool(AnimatorParameters.Grounded, IsGrounded);
            if (AnimatorParameters.Jumping != "") anim.SetBool(AnimatorParameters.Jumping, IsJumping);

            //Crouch parameters
            if (AnimatorParameters.Crouch != "") anim.SetBool(AnimatorParameters.Crouch, IsCrouched);

            //Prone parameters
            if (AnimatorParameters.Prone != "") anim.SetBool(AnimatorParameters.Prone, IsProne);

            //Roll
            //Note: the roll action is a trigger, call "_Roll();" when you want the character roll
            //Moving Body Rotation Animation
            if (AnimatorParameters.MovingTurn != "")
            {
                CalculateBodyRotation(ref BodyRotation);
                anim.SetFloat(AnimatorParameters.MovingTurn, BodyRotation);
            }

            //Idle Turn/Rotating Animation
            if (AnimatorParameters.IdleTurn != "")
            {
                CalculateRotationIntensity(ref IdleTurn);
                anim.SetFloat(AnimatorParameters.IdleTurn, IdleTurn);
            }

            //Fire mode parameters
            if (AnimatorParameters.ItemEquiped != "") anim.SetBool(AnimatorParameters.ItemEquiped, IsItemEquiped);
            if (AnimatorParameters.FireMode != "") anim.SetBool(AnimatorParameters.FireMode, FiringMode);

            //Reset Switch Animation
            if (IsWeaponSwitching == false && WeaponSwitchLayerWeight == 0 && AnimatorParameters._SwitchWeaponLayerIndex > -1) anim.Play("Empty", AnimatorParameters._SwitchWeaponLayerIndex);

            //Set Item Wielding
            if (AnimatorParameters.ItemsWieldingIdentifier != "") anim.SetInteger(AnimatorParameters.ItemsWieldingIdentifier, GetWieldingID());

            //Set Item Wielding
            if (AnimatorParameters.ItemWieldingRightHandPoseID != "") anim.SetFloat(AnimatorParameters.ItemWieldingRightHandPoseID, (HoldableItemInUseRightHand != null) ? HoldableItemInUseRightHand.GetWieldingPoseIndex() : 0);
            if (AnimatorParameters.ItemWieldingLeftHandPoseID != "") anim.SetFloat(AnimatorParameters.ItemWieldingLeftHandPoseID, (HoldableItemInUseLeftHand != null) ? HoldableItemInUseLeftHand.GetWieldingPoseIndex() : 0);
        }
        #endregion



        #region CONTROLLER LOCOMOTION
        protected virtual void Movement()
        {
            LocomotionModeController();

            //Ragdoll Controll
            if (Ragdoller != null)
            {
                if (Ragdoller.State != AdvancedRagdollController.RagdollState.Animated)
                {
                    IsRagdolled = true;
                    IsAiming = false;
                    FiringMode = false;
                    IsDriving = false;
                    ArmsWeightIK = 0;
                    LeftHandWeightIK = 0;
                    RightHandWeightIK = 0;
                    IsArmedWeight = 0;
                    BothArmsLayerWeight = 0;
                    DisableLocomotion();
                }
                else
                {
                    IsRagdolled = false;
                }
            }

            //Handle Root Motion
            if (RootMotion)
            {
                if (IsGrounded == true && IsJumping == false && IsRolling == false && FiringMode == false)
                {
                    anim.applyRootMotion = true;
                }
                else
                {
                    anim.applyRootMotion = false;
                }
            }

            // >>> Rolling Movement
            if (IsRolling)
            {
                if (CurvedMovement) { MoveForward(1.5f); } else { MoveDirectional(1.5f); }
            }

            // >>> No move condition
            if (CanMove == false) { VelocityMultiplier = 0; return; }

            // >>> In Air Movement Controller
            InAirMovementControl(JumpInert: true);

            //Moving Check
            IsMoving = (Mathf.Abs(VerticalY) != 0 || Mathf.Abs(HorizontalX) != 0);

            //>>> Locomotion / Movement <<<
            //If you want to edit the movement, you can edit it in the script "JUCharacterControllerCore.cs" or override the methods below.

            // Default Free Locomotion
            DoFreeMovement(FiringMode);
            //Default Fire Mode Locomotion
            DoFireModeMovement(FiringMode);


            //Weapon Switching Animation Timer
            if (WeaponSwitchingCurrentTime <= 1 && IsWeaponSwitching)
            {
                WeaponSwitchingCurrentTime += Time.deltaTime;
                if (WeaponSwitchingCurrentTime >= 0.5f)
                {
                    if (FiringMode) { FiringModeIK = true; }

                    IsWeaponSwitching = false;
                    WeaponSwitchingCurrentTime = 0;
                }
            }
        }
        protected virtual void LocomotionModeController()
        {
            switch (LocomotionMode)
            {
                case MovementMode.AwaysInFireMode:
                    /*if (VerticalY < 0.2f || (Mathf.Abs(HorizontalX) - VerticalY / 5) >= 0.9f)
                    {
                    }*/
                    FiringMode = true;
                    //FiringModeIK = true;
                    CurrentTimeToDisableFireMode = 0;

                    if (IsReloading) FiringModeIK = false;
                    break;
                case MovementMode.JuTpsClassic:
                    FiringMode = IsItemEquiped;
                    CurrentTimeToDisableFireMode = IsItemEquiped ? 0 : CurrentTimeToDisableFireMode;
                    break;
            }
        }

        public void DisableLocomotion(float duration = 0)
        {
            HorizontalX = 0;
            VerticalY = 0;
            VelocityMultiplier = 0;
            DisableAllMove = true;
            FiringMode = false;
            CanMove = false;

            if (duration > 0) Invoke(nameof(enableMove), duration);
        }
        public void EnableMove()
        {
            DisableAllMove = false;
        }

        #endregion



        #region ITEM MANAGEMENT
        private void WeaponOrientator()
        {
            if (Inventory != null)
            {
                CurrentItemIDRightHand = Inventory.CurrentRightHandItemID;
                CurrentItemIDLeftHand = Inventory.CurrentLeftHandItemID;

                IsItemEquiped = Inventory.IsItemSelected;
            }
            //Weapon Control
            if (IsItemEquiped)
            {
                //Weapon Orientation with camera
                if (MyCamera != null)
                {
                    if (WeaponInUseRightHand != null)
                    {
                        Vector3 orientation = (LookAtPosition != Vector3.zero) ? GetCurrentWeaponLookDirection() : MyCamera.transform.forward;
                        WeaponInUseRightHand.SetWeaponOrientation(MyCamera.transform.position, orientation);
                    }
                    if (WeaponInUseLeftHand != null)
                    {
                        Vector3 orientation = (LookAtPosition != Vector3.zero) ? GetCurrentWeaponLookDirection() : MyCamera.transform.forward;
                        WeaponInUseLeftHand.SetWeaponOrientation(MyCamera.transform.position, orientation);
                    }
                }
                else
                {
                    //Weapon orientation without camera
                    if (WeaponInUseRightHand != null)
                    {
                        WeaponInUseRightHand.SetWeaponOrientation(Vector3.zero, GetCurrentWeaponLookDirection());
                    }
                    if (WeaponInUseLeftHand != null)
                    {
                        WeaponInUseLeftHand.SetWeaponOrientation(Vector3.zero, GetCurrentWeaponLookDirection());
                    }
                }
            }

            //If is Armed
            if (CurrentItemIDRightHand == -1 && CurrentItemIDLeftHand == -1)
            {
                IsItemEquiped = false;
            }
            else
            {
                IsItemEquiped = true;
            }

            if (HoldableItemInUseRightHand != null)
            {
                if (HoldableItemInUseRightHand.BlockFireMode) { FiringMode = false; FiringModeIK = false; }
            }

            if (HoldableItemInUseLeftHand != null)
            {
                if (HoldableItemInUseLeftHand.BlockFireMode) { FiringMode = false; FiringModeIK = false; }
            }
        }
        private void WieldingIKWeightController()
        {
            //Disable FireMode IK when isn't in FireMode
            if (FiringMode == false) FiringModeIK = false;

            // Hands IK
            if (IsItemEquiped)
            {
                SmoothLeftHandPosition(25);
                SmoothRightHandPosition(25);
                if (IsAiming)
                {
                    DoHandPositioningNoSmoothing();
                }
            }

            //Look IK Weight Control (Head, Spine)
            if (FiringMode && IsReloading == false)
            {
                LookWeightIK = Mathf.Lerp(LookWeightIK, 1f, 5f * Time.deltaTime);
            }
            else
            {
                LookWeightIK = Mathf.Lerp(LookWeightIK, 0f, 6f * Time.deltaTime);
            }

            // Arms IK Weight Control
            if (FiringModeIK == true && IsWeaponSwitching == false && IsRolling == false)
            {
                ArmsWeightIK = Mathf.MoveTowards(ArmsWeightIK, 1f, 2 * Time.deltaTime);
            }
            else
            {
                ArmsWeightIK = Mathf.MoveTowards(ArmsWeightIK, 0f, 0.5f * Time.deltaTime);
            }

            if (IsWeaponSwitching || IsRolling)
            {
                LeftHandWeightIK = 0;
                RightHandWeightIK = 0;
            }
            else
            {
                float IKTransitionSpeed = 15;
                if (IsRolling == false && IsReloading == false)
                {
                    // >>> Left Hand IK Weight 
                    if (HoldableItemInUseLeftHand != null)
                    {
                        if (HoldableItemInUseLeftHand.HoldPose != JUHoldableItem.ItemHoldingPose.Free)
                        {
                            if (FiringModeIK)
                            {
                                LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 1, IKTransitionSpeed * Time.deltaTime);
                            }
                            else
                            {
                                LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                        else
                        {
                            if (HoldableItemInUseRightHand != null)
                            {
                                if (HoldableItemInUseRightHand.OppositeHandPosition != null)
                                {
                                    LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 1, IKTransitionSpeed * Time.deltaTime);
                                }
                                else
                                {
                                    LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                                }
                            }
                            else
                            {
                                LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                    }
                    else
                    {
                        if (HoldableItemInUseRightHand != null)
                        {
                            if (HoldableItemInUseRightHand.OppositeHandPosition != null)
                            {
                                LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, (!FiringModeIK && FiringMode) ? 0 : 1, IKTransitionSpeed * Time.deltaTime);
                            }
                            else
                            {
                                LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                        else
                        {
                            LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                        }
                    }

                    // >>> Right Hand IK Weight
                    if (HoldableItemInUseRightHand != null)
                    {
                        if (HoldableItemInUseRightHand.HoldPose != JUHoldableItem.ItemHoldingPose.Free)
                        {
                            if (FiringModeIK)
                            {
                                RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 1, IKTransitionSpeed * Time.deltaTime);
                            }
                            else
                            {
                                RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                        else
                        {
                            if (HoldableItemInUseLeftHand != null)
                            {
                                if (HoldableItemInUseLeftHand.OppositeHandPosition != null)
                                {
                                    RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 1, IKTransitionSpeed * Time.deltaTime);
                                }
                                else
                                {
                                    RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                                }
                            }
                            else
                            {
                                RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                    }
                    else
                    {
                        if (HoldableItemInUseLeftHand != null)
                        {
                            if (HoldableItemInUseLeftHand.OppositeHandPosition != null)
                            {
                                RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, (!FiringModeIK && FiringMode) ? 0 : 1, IKTransitionSpeed * Time.deltaTime);
                            }
                            else
                            {
                                RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                            }
                        }
                        else
                        {
                            RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                        }
                    }
                }
                else
                {
                    LeftHandWeightIK = Mathf.Lerp(LeftHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                    RightHandWeightIK = Mathf.Lerp(RightHandWeightIK, 0, IKTransitionSpeed * Time.deltaTime);
                }
            }
        }
        private void RefreshItemAimRotationPivot()
        {
            //Pivot Weapons Rotation

            //Prone IK Adjust
            Vector3 HumanoidSpinePronePosition = HumanoidSpine.position + transform.forward * 0.2f - transform.up * 0.2f;
            Vector3 TargetHumanoidSpinePosition = IsProne == false ? HumanoidSpine.position : HumanoidSpinePronePosition;

            // Items/Weapons Aim Rotation Center Rotation
            PivotItemRotation.transform.position = TargetHumanoidSpinePosition;
            Vector3 CamEuler = GetLookDirectionEulerAngles();


            //Refresh item wielding rotation center rotation
            if (FiringMode)
            {
                PivotItemRotation.transform.rotation = Quaternion.Lerp(PivotItemRotation.transform.rotation, Quaternion.Euler(CamEuler), 10 * RotationSpeed * Time.deltaTime);
            }
            else
            {
                PivotItemRotation.transform.rotation = Quaternion.Lerp(PivotItemRotation.transform.rotation, transform.rotation, 8 * RotationSpeed * Time.deltaTime);
            }

            if (IsAiming)
            {
                PivotItemRotation.transform.parent = null;
                PivotItemRotation.transform.rotation = Quaternion.Euler(CamEuler);
            }
            else
            {
                //PivotItemRotation.transform.parent = transform;
            }

            //PivotItemRotation.transform.localEulerAngles = new Vector3(PivotItemRotation.transform.localEulerAngles.x, PivotItemRotation.transform.localEulerAngles.y, transform.localEulerAngles.z);
        }
        #endregion



        #region JU FOOT PLACEMENT INTEGRATION
        private void FootPlacementIKController()
        {
            if (FootPlacerIK != null)
            {
                if (!IsGrounded || IsJumping || IsDriving || IsDead || IsRolling || IsRagdolled || IsProne)
                {
                    FootPlacerIK.SmoothIKTransition = false;
                    FootPlacerIK.BlockBodyPositioning = true;
                    FootPlacerIK.EnableDynamicBodyPlacing = false;
                }
                else
                {
                    //Enable Foot Placement
                    FootPlacerIK.SmoothIKTransition = true;
                    if (!IsCrouched)
                    {
                        FootPlacerIK.EnableDynamicBodyPlacing = true;
                    }
                    else
                    {
                        FootPlacerIK.EnableDynamicBodyPlacing = false;
                    }
                }
            }
        }
        #endregion

        #region PHYSICAL DAMAGE FUNCTIONS
        public void EnablePhysicalDamage() => PhysicalDamage = true;
        public void DisablePhysicalDamage() => PhysicalDamage = false;
        public void DisablePhysicalDamageForSeconds(float seconds = 1)
        {
            DisablePhysicalDamage();
            if (IsInvoking(nameof(EnablePhysicalDamage))) { CancelInvoke(nameof(EnablePhysicalDamage)); }
            Invoke(nameof(EnablePhysicalDamage), seconds);
        }

        #endregion

        #region Physics Check
        void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.tag == "DeadZone")
            {
                KillCharacter();
            }
            if (other.gameObject.tag == "RagdollZone" && Ragdoller != null && !IsDriving)
            {
                Ragdoller.State = AdvancedRagdollController.RagdollState.Ragdolled;
                IsArmedWeight = 0;
            }
        }

        void OnCollisionEnter(Collision other)
        {
            if (other.gameObject.tag == "DeadZone")
            {
                KillCharacter();
            }
            if (other.gameObject.tag == "RagdollZone" && Ragdoller != null)
            {
                Ragdoller.State = AdvancedRagdollController.RagdollState.Ragdolled;
            }

            // >>> Physical Damage System
            if (PhysicalDamage == false) return;

            if (AI.JUCharacterArtificialInteligenceBrain.TagMatches(other.collider.gameObject.tag, PhysicalDamageIgnoreTags)) return;


            if (other.gameObject.TryGetComponent(out Rigidbody rbOtherPhysicObject))
            {
                Vector3 contactDirection = (other.contacts[0].point - transform.position).normalized;

                // -1 = a physical object is moving into character direction
                // 1 = a physical object is moving in character oposite direction
                float CollisionPressure = Vector3.Dot(contactDirection, rbOtherPhysicObject.linearVelocity.normalized);
                //Debug.Log(gameObject.name + " ◄ " + rbOtherPhysicObject.name + " ] Collision Pressure = " + CollisionPressure.ToString());
                if (CollisionPressure > -0.1) return;
                float physicalDamage = PhysicalDamageMultiplier * (rbOtherPhysicObject.linearVelocity.magnitude / 20 * rbOtherPhysicObject.mass);

                // Do physical damage
                if (physicalDamage > PhysicalDamageStartAt)
                {
                    TakeDamage(physicalDamage, other.contacts[0].point);

                    //Hit damage on screen
                    //FX.HitMarkerEffect.HitCheck("Skin", "Player", other.contacts[0].point, physicalDamage);
                }
                // Do ragdoll
                if (Ragdoller)
                {
                    if (physicalDamage > RagdollStartAtDamage) Ragdoller.State = AdvancedRagdollController.RagdollState.Ragdolled;
                }

            }
        }


        #endregion


        void OnAnimatorIK(int layerIndex)
        {
            if (IsDead || InverseKinematics == false) return;

            //Get Original Spine Rotation
            OriginalSpineRotation = anim.GetBoneTransform(HumanBodyBones.Spine).transform.localRotation;
            //Firing Mode IK
            if (IsRolling == false && IsDriving == false)
            {
                //If you have problems with the elbow ik on your left arm, uncomment the line below
                //LeftHandToRespectiveIKPosition(LeftHandWeightIK, LeftHandWeightIK / 1.2f);

                LeftHandToRespectiveIKPosition(LeftHandWeightIK, 0);
                RightHandToRespectiveIKPosition(RightHandWeightIK, RightHandWeightIK / 1.2f);

                Vector3 LookingPosition = GetLookPosition();
                float BodyWeight = (IsProne) ? 0.1f : 0.3f;

                float LookingIntensity = Vector3.Dot(transform.forward, (LookingPosition - transform.position).normalized);
                LookAtIK(LookingPosition, LookingIntensity * LookWeightIK, BodyWeight, 0.6f);
            }
        }
        private void OnAnimatorMove()
        {
            ApplyRootMotionOnLocomotion();
        }
    }

}