using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.JUInputSystem;
using JUTPS.WeaponSystem;

namespace JUTPS.CameraSystems
{

    public class FPSCameraController : JUCameraController
    {
        private JUTPS.CharacterBrain.JUCharacterBrain characterTarget;
        float xmouse, ymouse;
        float SmoothedYMouse, SmoothedXMouse;
        float ScopeModeRecoil;
        float weight;
        Vector3 CamPosition;

        Vector3 SmoothedCameraPosition;

        public CameraState FPSCameraState = new CameraState("FPS Camera State", 0, 100, 50);
        public CameraState AimModeCameraState = new CameraState("FPS Camera State", 0, 100, 50);
        public CameraState DrivingModeCameraState = new CameraState("FPS Camera State", 0, 1000, 70);

        [Header("Weapon Sway Config")]
        public float AimInSpeed = 6;
        public float AimOutSpeed = 6;
        public float SwaySpeed = 5;
        public float HorizontalIntensity = 1;
        public float VerticalIntensity = 1;
        public float AimHorizontalIntensity = 1;
        public float AimVerticalIntensity = 1;
        // Start is called before the first frame update
        protected override void Start()
        {
            base.Start();

            if (TargetToFollow.TryGetComponent(out JUTPS.CharacterBrain.JUCharacterBrain JUcharacter))
            {
                characterTarget = JUcharacter;
                TargetToFollow = characterTarget.HumanoidSpine;
                characterTarget.LocomotionMode = JUTPS.CharacterBrain.JUCharacterBrain.MovementMode.AwaysInFireMode;
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (Cursor.lockState != CursorLockMode.Locked && JUGameManager.IsMobileControls == false)
            {
                xmouse = 0;
                ymouse = 0;
                return;
            }

            // Mouse Input
            xmouse = (Aiming ? 30 : 100) * JUInput.GetAxis(JUInput.Axis.RotateVertical) / 100;
            ymouse = (Aiming ? 30 : 100) * JUInput.GetAxis(JUInput.Axis.RotateHorizontal) / 100;

            // Driving Camera 
            if (characterTarget != null)
            {
                if (characterTarget.IsDriving && characterTarget.VehicleInArea != null)
                {
                    xmouse = 0;
                    ymouse = 0;
                    SetCameraRotation(TargetToFollow.transform.rotation.x, characterTarget.VehicleInArea.transform.eulerAngles.y, false);

                }
            }
            else
            {
                RotateCamera(xmouse, ymouse, upward: characterTarget == null ? TargetToFollow.up : characterTarget.transform.up);
                return;
            }


            characterTarget.IsRolling = false;

            if (characterTarget.IsDriving)
            {
                SetCameraStateTransition(GetCurrentCameraState, DrivingModeCameraState);
                RotateCamera(xmouse, ymouse, upward: characterTarget.VehicleInArea.transform.up, AlternativeTargetToCalculate: characterTarget.VehicleInArea.transform);
            }
            else
            {
                SetCameraStateTransition(GetCurrentCameraState, Aiming ? AimModeCameraState : FPSCameraState);
                RotateCamera(xmouse, ymouse, upward: characterTarget == null ? TargetToFollow.up : characterTarget.transform.up);
            }

        }
        private void LateUpdate()
        {
            SetFieldOfView(GetCurrentCameraState.CameraFieldOfView);
            SetCameraPositionToScopePosition();
        }
        private void FixedUpdate()
        {
            SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(TargetToFollow), false);
        }
        public override void RecoilReaction(float Force)
        {
            base.RecoilReaction(Force);

            ScopeModeRecoil -= Force / 30;
        }
        public void SetCameraPositionToScopePosition()
        {
            if (characterTarget == null) return;

            Aiming = characterTarget.IsAiming;

            if (characterTarget.WeaponInUseRightHand == null || characterTarget.IsDriving) return;

            if (characterTarget.WeaponInUseRightHand.AimMode != Weapon.WeaponAimMode.None && characterTarget.FiringMode)
            {
                var gun = characterTarget.WeaponInUseRightHand;

                SmoothedYMouse = Mathf.Lerp(SmoothedYMouse, ymouse * (Aiming ? AimHorizontalIntensity : HorizontalIntensity), SwaySpeed * Time.deltaTime);
                SmoothedXMouse = Mathf.Lerp(SmoothedXMouse, xmouse * (Aiming ? AimVerticalIntensity : VerticalIntensity), SwaySpeed * Time.deltaTime);
                ScopeModeRecoil = Mathf.Lerp(ScopeModeRecoil, 0, 5 * Time.deltaTime);
                //Debug.Log("rot int = " + SmoothedXMouse);

                Vector3 scopePosition = gun.transform.position
                    + gun.transform.right * (gun.CameraAimingPosition.x - SmoothedYMouse / 20)
                    + gun.transform.up * (gun.CameraAimingPosition.y - SmoothedXMouse / 20)
                    + mCamera.transform.parent.forward * (gun.CameraAimingPosition.z - ScopeModeRecoil);

                //var target = TargetToFollow;
                Vector3 normalPosition = TargetToFollow.transform.position
                    + mCamera.transform.parent.right * (GetCurrentCameraState.RightCameraOffset - SmoothedYMouse / 10)
                    + mCamera.transform.parent.up * (GetCurrentCameraState.UpCameraOffset - SmoothedXMouse / 4)
                    + mCamera.transform.parent.forward * GetCurrentCameraState.ForwardCameraOffset;

                if (Aiming == false)
                {
                    weight = Mathf.MoveTowards(weight, 1, AimInSpeed * Time.deltaTime);
                }
                else
                {
                    weight = Mathf.MoveTowards(weight, 0, AimOutSpeed * Time.deltaTime);
                }

                CamPosition = Vector3.Lerp(scopePosition, normalPosition, weight);
                SmoothedCameraPosition = Vector3.Slerp(SmoothedCameraPosition, CamPosition, 60 * Time.deltaTime);
                SetCameraPosition(CamPosition, false);

                //Set Field Of View
                AimModeCameraState.CameraFieldOfView = Mathf.Lerp(AimModeCameraState.CameraFieldOfView, gun.CameraFOV, 15 * Time.deltaTime);
            }
            else
            {
                AimModeCameraState.CameraFieldOfView = GetCurrentCameraState.CameraFieldOfView;
            }
        }
    }

}