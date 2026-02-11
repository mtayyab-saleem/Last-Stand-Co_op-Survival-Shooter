using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

using JUTPS.ExtendedInverseKinematics;
using JUTPSEditor.JUHeader;
using JUTPS.CameraSystems;

namespace JUTPS.CoverSystem
{
    [AddComponentMenu("JU TPS/Third Person System/Cover System/JU Cover Controller")]
    public class JUCoverController : JUTPSActions.JUTPSAction
    {
        [JUHeader("Detect Cover Walls Settings")]
        public string CoverTriggerTag = "CoverTrigger";
        public JUCoverTrigger CurrentCoverTrigger;
        public bool EnableCover = true;
        public bool AutoMode = true;

        [JUHeader("Character Positioning Settings")]
        public float SneakSpeed = 1;
        public float RotationSpeed = 10;
        public float CrouchedSneakSpeed = 1.3f;
        public float CornerPositionOffset = 0.5f;
        public float WallOffsetPosition = 0f;
        public float WallOffsetPositionOnFireMode = 0.2f;
        public string CoverAnimatorParameter = "Cover";

        [JUHeader("Camera Settings")]
        public float CameraRightOffset = 0.6f;
        public float SwitchSideSpeed = 8;

        [JUHeader("Lean Settings")]
        public float SpineInclination = -5;
        public float LeftWeaponInclinationIntensity = 5;
        public float RightWeaponInclinationIntensity = 2.5f;
        public Vector3 LeanAxis = new Vector3(0, 0, 1);

        [JUHeader("States")]
        public bool IsCovering;
        public bool IsCrouchCover;
        public bool IsLeftSide = true;
        public bool OnWallEdge;

        [JUHeader("FireMode Options")]
        public float FireModeDurationInCover = 0.1f;
        private float StartFireModeDuration;
        
        [JUHeader("Events")]
        public UnityEvent OnCover;
        public UnityEvent OnExitCover;
        private bool onExitCoverCalled;

        
        private bool isSwitchingCoverTrigger;
        private float switchingCoverProgression;
        private float toCornerProgression;
        private Vector3 oldCharacterPosition;
        private Vector3 movementVector;
        private Quaternion CharacterRotation;
        private float coverPoint;
        private float moveAnimationSpeedValue;
        //Camera Variables
        private TPSCameraController camThirdPerson;
        private float camRightOffsetValue;
        void Start()
        {
            IsLeftSide = true;
            StartFireModeDuration = TPSCharacter.FireModeMaxTime;
            camThirdPerson = FindObjectOfType<TPSCameraController>();
        }
        private void OnEnable()
        {
            OnCover.AddListener(EnterCoverAnimationState);
            OnExitCover.AddListener(EnterCoverAnimationState);
            OnCover.AddListener(OnEnteringCover);
            OnExitCover.AddListener(OnExitingCover);
        }
        private void OnDisable()
        {
            OnCover.RemoveListener(EnterCoverAnimationState);
            OnExitCover.RemoveListener(EnterCoverAnimationState);
            OnCover.RemoveListener(OnEnteringCover);
            OnExitCover.RemoveListener(OnExitingCover);
        }
        private void FixedUpdate()
        {
           
            if (IsCovering == false) return;

            //Move cover target position
            if (!TPSCharacter.WallAHead)
            {
                movementVector = new Vector3(JUInputSystem.JUInput.GetAxis(JUInputSystem.JUInput.Axis.MoveHorizontal), 0, JUInputSystem.JUInput.GetAxis(JUInputSystem.JUInput.Axis.MoveVertical));
                if (TPSCharacter.FiringMode)
                {
                    //Fire Mode Covering Movement
                    coverPoint += movementVector.x * TPSCharacter.VelocityMultiplier * SneakSpeed / CurrentCoverTrigger.CoverMovementLineLenght() * Time.deltaTime;
                }
                else
                {
                    //Normal Covering Movement
                    coverPoint += Vector3.Dot(TPSCharacter.DirectionTransform.forward, -transform.right) * TPSCharacter.VelocityMultiplier * (IsCrouchCover ? CrouchedSneakSpeed : SneakSpeed) / CurrentCoverTrigger.CoverMovementLineLenght() * Time.deltaTime;
                }
            }
            coverPoint = Mathf.Clamp01(coverPoint);

            //Get target position on cover
            Vector3 targetPosition = Vector3.Lerp(CurrentCoverTrigger.LeftEndPoint(), CurrentCoverTrigger.RightEndPoint(), coverPoint) - CurrentCoverTrigger.transform.forward * (TPSCharacter.FiringMode ? WallOffsetPositionOnFireMode : WallOffsetPosition);
            if (TPSCharacter.FiringMode)
            {
                toCornerProgression = Mathf.Lerp(toCornerProgression, CornerPositionOffset, 8 * Time.deltaTime);
                if (OnWallEdge) targetPosition = targetPosition - CurrentCoverTrigger.transform.right * (IsLeftSide ? -toCornerProgression : toCornerProgression);
            }
            else
            {
                toCornerProgression = Mathf.Lerp(toCornerProgression, 0, 8 * Time.deltaTime);
            }
            if (isSwitchingCoverTrigger)
            {
                switchingCoverProgression = Mathf.Lerp(switchingCoverProgression, 2, 2*SneakSpeed * Time.deltaTime);
                transform.position = Vector3.Lerp(oldCharacterPosition, targetPosition, switchingCoverProgression);
                rb.linearVelocity = Vector3.zero;
                TPSCharacter.VelocityMultiplier = 1;

                if (switchingCoverProgression >= 1)
                {
                    isSwitchingCoverTrigger = false;
                    switchingCoverProgression = 0;
                }
            }
            else
            {
                transform.position = targetPosition;
                oldCharacterPosition = transform.position;
            }
        }
        void LateUpdate()
        {
            //Get old position
            oldCharacterPosition = isSwitchingCoverTrigger ? oldCharacterPosition : transform.position;
            //Update camera
            UpdateCameraRightOffset();

            //Disabling Cover
            if (EnableCover == false || CurrentCoverTrigger == null)
            {
                IsCovering = false;
                isSwitchingCoverTrigger = false;
                return;
            }

            //Press Interaction Button to Cover
            if (JUInputSystem.JUInput.GetButtonDown(JUInputSystem.JUInput.Buttons.EnterVehicleButton) && CurrentCoverTrigger != null && AutoMode == false)
            {
                if (IsCovering == false)
                {
                    StartCover(CurrentCoverTrigger);
                }
                else
                {
                    IsCovering = false;
                }
            }

            //Animations
            UpdateAnimator();

            //Set Crouch Covering
            if (IsCovering && !TPSCharacter.IsCrouched && IsCrouchCover && TPSCharacter.FiringMode == false)
            {
                if (IsCrouchCover && !TPSCharacter.IsCrouched && TPSCharacter.FiringMode == false)
                {
                    TPSCharacter._Crouch();
                }
                if ((IsCrouchCover == false && TPSCharacter.IsCrouched) || TPSCharacter.FiringMode)
                {
                    TPSCharacter._GetUp();
                }
            }

            //Disable Cover State
            if (CurrentCoverTrigger == null) { IsCovering = false; IsCrouchCover = false; }


            //Exit Cover State
            if (IsCovering && ((movementVector.normalized.magnitude > 0.3f && Vector3.Dot(CurrentCoverTrigger.transform.forward, TPSCharacter.DirectionTransform.forward) < -0.85f && isSwitchingCoverTrigger == false) || TPSCharacter.IsJumping || TPSCharacter.IsGrounded == false || TPSCharacter.IsRolling || TPSCharacter.IsDead || TPSCharacter.IsMeleeAttacking))
            {
                IsCovering = false;
                CurrentCoverTrigger = null;
                isSwitchingCoverTrigger = false;
            }

            //On exit cover event call
            if (IsCovering == false && onExitCoverCalled == false)
            {
                OnExitCover.Invoke();
                switchingCoverProgression = 0;
                onExitCoverCalled = true;
            }


            //Cover Movement
            if (IsCovering)
            {
                TPSCharacter.VelocityMultiplier = Mathf.Clamp(TPSCharacter.VelocityMultiplier, 0, 0.8f);
                TPSCharacter.rb.linearVelocity = Vector3.zero;
                TPSCharacter.IsSprinting = false;

                Quaternion desiredRotation = Quaternion.LookRotation(-CurrentCoverTrigger.transform.forward, TPSCharacter.UpDirection == Vector3.zero ? Vector3.up : TPSCharacter.UpDirection);
                CharacterRotation = Quaternion.Lerp(CharacterRotation, desiredRotation, RotationSpeed * Time.deltaTime);

                //Check wall edge
                OnWallEdge = (coverPoint >= 0.98f || coverPoint <= 0.02f);


                if (TPSCharacter.FiringMode == false)
                {
                    transform.rotation = CharacterRotation;


                    //Change Cover Direction
                    if (Vector3.Dot(transform.right, TPSCharacter.DirectionTransform.forward) > 0.5f && IsLeftSide == true)
                    {
                        IsLeftSide = false;
                        switchingCoverProgression = 0;
                    }
                    //Change Cover Direction
                    if (Vector3.Dot(transform.right, TPSCharacter.DirectionTransform.forward) < -0.5f && IsLeftSide == false)
                    {
                        IsLeftSide = true;
                        switchingCoverProgression = 0;
                    }
                }
                else
                {
                    CharacterRotation = transform.rotation;
                }


                // Item Aim Rotation Adjust
                if (IsCrouchCover == false)
                {
                    Vector3 localItemPivotEuler = TPSCharacter.PivotItemRotation.transform.localEulerAngles;
                    TPSCharacter.PivotItemRotation.transform.localEulerAngles = new Vector3(localItemPivotEuler.x, localItemPivotEuler.y, (IsLeftSide ? LeftWeaponInclinationIntensity : -RightWeaponInclinationIntensity) * SpineInclination);
                }
            }
            else
            {
                CharacterRotation = TPSCharacter.transform.rotation;
            }
        }

        public void StartCover(JUCoverTrigger coverTrigger)
        {
            if (isSwitchingCoverTrigger) return;
            if (TPSCharacter.IsGrounded == false) return;
            if (CurrentCoverTrigger != coverTrigger) { isSwitchingCoverTrigger = true; }
            CharacterRotation = transform.rotation;

            //Get cover point in line
            coverPoint = coverTrigger.LerpValue(TPSCharacter.transform.position);

            //Set Current Cover Trigger
            CurrentCoverTrigger = coverTrigger;
            switchingCoverProgression = 0;

            //Start Cover System
            IsCovering = true;
            IsCrouchCover = coverTrigger.IsCrouchingCover;

            //OnCover Event Call
            onExitCoverCalled = false;
            OnCover.Invoke();
        }
        protected void UpdateCameraRightOffset()
        {
            // >>> Cover Camera Controll
            if (IsCovering && camThirdPerson != null)
            {
                camRightOffsetValue = Mathf.Lerp(camRightOffsetValue, IsLeftSide ? CameraRightOffset : -CameraRightOffset, SwitchSideSpeed * Time.deltaTime);
                camThirdPerson.GetCurrentCameraState.RightCameraOffset = camRightOffsetValue;
            }
            else if (camThirdPerson != null)
            {
                camRightOffsetValue = camThirdPerson.GetCurrentCameraState.RightCameraOffset;
            }
        }
        protected void UpdateAnimator()
        {
            anim.SetBool("Cover", IsCovering);
            anim.SetBool("CoverLeftSide", IsLeftSide);

            if(TPSCharacter.FiringMode && IsCovering)
            {
                //TPSCharacter.SetMoveInput(0,0);
                if (TPSCharacter.FiringMode && TPSCharacter.IsCrouched) TPSCharacter._GetUp();
            }

            if (OnWallEdge)
            {
                moveAnimationSpeedValue = Mathf.Lerp(moveAnimationSpeedValue, 0, 3 * Time.deltaTime);
                //Stop Moving
                anim.SetFloat("Speed", moveAnimationSpeedValue);
                anim.SetFloat("Horizontal", moveAnimationSpeedValue);
                anim.SetFloat("Vertical", moveAnimationSpeedValue);
            }
            else
            {
                moveAnimationSpeedValue = TPSCharacter.VelocityMultiplier;
            }
        }
        
        private void EnterCoverAnimationState()
        {
            if (IsCovering) onExitCoverCalled = false;
            anim.SetBool("Cover", IsCovering);
        }


        private void OnAnimatorIK(int layerIndex)
        {
            if (SpineInclination == 0 || IsCovering == false || TPSCharacter.FiringMode == false || IsCrouchCover) return;
            anim.SpineInclination(LeanAxis, IsLeftSide ? SpineInclination : -SpineInclination, 0.5f);
        }


        protected void OnEnteringCover()
        {
            TPSCharacter.FireModeMaxTime = FireModeDurationInCover;
            if (gameObject.TryGetComponent(out JUTPS.FX.BodyLeanInert bodylean)) { bodylean.enabled = false; }

            if (IsCrouchCover == false && TPSCharacter.IsCrouched == true) TPSCharacter._GetUp();

            

        }
        protected void OnExitingCover()
        {
            TPSCharacter.FireModeMaxTime = StartFireModeDuration;
            if (gameObject.TryGetComponent(out JUTPS.FX.BodyLeanInert bodylean)) { bodylean.enabled = true; }
        }


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag(CoverTriggerTag))
            {
                if (other.TryGetComponent(out JUCoverTrigger coverTrigger))
                {
                    if (AutoMode || IsCovering)
                    {
                        StartCover(coverTrigger);
                    }
                    else
                    {
                        CurrentCoverTrigger = coverTrigger;
                    }
                }
            }
        }
        private void OnTriggerExit(Collider other)
        {
            if (AutoMode == true) return;

            if (other.CompareTag(CoverTriggerTag) && IsCovering == false)
            {
                CurrentCoverTrigger = null;
            }
        }
    }
}