using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using JUTPSActions;

using JUTPS.JUInputSystem;
using JUTPS.CameraSystems;

namespace JUTPS.ActionScripts
{

    public class AimOnRightJoystickDirection : JUTPSAction
    {
        [HideInInspector] public static Vector3 AimPosition;
        [HideInInspector] private JUCameraController cameraController;

        [Header("Settings")]
        public bool Enabled = true;
        public float DistanceFromCenter = 5;
        public float UpOffset;
        public bool FireModeWhenHasJoystickDirection = true;
        [Header("Aim Mode Settings")]
        public bool SidescrollerAimMode;

        float Xinput, Yinput;


        [HideInInspector] public bool IsUsingJoystick;
        void Start()
        {
            cameraController = FindObjectOfType<JUCameraController>();
        }

        // Update is called once per frame
        void Update()
        {
            if (Enabled == false)
            {
                return;
            }
            float realXinput = Mathf.Clamp(Mathf.Abs(JUInput.GetAxis(JUInput.Axis.RotateHorizontal)), -1, 1);
            float realYinput = Mathf.Clamp(Mathf.Abs(JUInput.GetAxis(JUInput.Axis.RotateVertical)), -1, 1);

            if (realYinput > 0.1f || realXinput > 0.1f)
            {
                IsUsingJoystick = true;
                Yinput = JUInput.GetAxis(JUInput.Axis.RotateVertical);
                Xinput = JUInput.GetAxis(JUInput.Axis.RotateHorizontal);
                if (FireModeWhenHasJoystickDirection)
                {
                    if (TPSCharacter.HoldableItemInUseRightHand == null)
                    {
                        TPSCharacter.CurrentTimeToDisableFireMode = 0;
                        TPSCharacter.FiringMode = true;
                        TPSCharacter.FiringModeIK = true;
                    }
                    else
                    {
                        if (TPSCharacter.HoldableItemInUseRightHand.BlockFireMode == false)
                        {
                            TPSCharacter.CurrentTimeToDisableFireMode = 0;
                            TPSCharacter.FiringMode = true;
                            TPSCharacter.FiringModeIK = true;
                        }
                    }
                }
            }
            else
            {
                if (FireModeWhenHasJoystickDirection && IsUsingJoystick)
                {
                    TPSCharacter.FiringMode = false; TPSCharacter.FiringModeIK = false;
                    IsUsingJoystick = false;
                }
            }

            if (SidescrollerAimMode)
            {
                Vector3 Direction = new Vector3(Xinput, Yinput);

                //Modify position
                //Direction.z = Mathf.Lerp(TPSCharacter.transform.position.z - 3f, Yinput, new Vector3(Xinput, Yinput).magnitude + 0.5f);

                AimPosition = TPSCharacter.PivotItemRotation.transform.position + Direction.normalized * DistanceFromCenter;
                TPSCharacter.LookAtPosition = AimPosition;
            }
            else
            {
                Vector3 Direction = new Vector3(Xinput, 0, Yinput).normalized;
                Quaternion CamDirection = Quaternion.Euler(0, cameraController.mCamera.transform.eulerAngles.y, 0);
                Quaternion DesiredDirection = Direction.magnitude > 0 ? Quaternion.LookRotation(Direction, Vector3.up) * CamDirection : Quaternion.identity;
                Vector3 DesiredAimPosition = TPSCharacter.PivotItemRotation.transform.position + (DesiredDirection * Vector3.forward) * DistanceFromCenter + transform.up * UpOffset;
                AimPosition = DesiredAimPosition;

                //AimPosition = TPSCharacter.PivotItemRotation.transform.position + (Direction * DistanceFromCenter) + transform.up * UpOffset;
                TPSCharacter.LookAtPosition = AimPosition;
            }


        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (SidescrollerAimMode)
            {
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.1f, 0.1f, 0));
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.5f, 0.5f, 0));
            }
            else
            {
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.1f, 0, 0.1f));
                Gizmos.DrawWireCube(AimPosition, new Vector3(0.5f, 0, 0.5f));
            }
        }
    }
}