using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.JUInputSystem;
using JUTPS.CrossPlataform;
using JUTPS.ActionScripts;
using JUTPS.VehicleSystem;
using System.Xml.Schema;





#if UNITY_EDITOR
using UnityEditor;
#endif

namespace JUTPS.CrossPlataform
{

    public class MobileRig : MonoBehaviour
    {
        [Header("Panels")]
        public GameObject MobileScreenPanel;
        public GameObject NormalScreenPanel;
        public GameObject DrivingScreenPanel;

        [Header("Joysticks")]
        [SerializeField] private JoystickVirtual MovementJoystick;
        [SerializeField] private JoystickVirtual RightJoystick;
        [SerializeField] private bool RightJoystickIsShootInput = true;
        [SerializeField] private float RightJoystickShotSensibility = 0.5f;
        private bool PressedRightJoystickDown;
        [Header("Touch Fields")]
        [SerializeField] private Touchfield RotateCameraTouchfield;
        [SerializeField] private Touchfield ShotButtonTouchfield;

        [Header("Buttons")]
        public bool ShowShotButtonOnlyUsingItem;
        [SerializeField] private ButtonVirtual ShotButton;
        [SerializeField]
        private ButtonVirtual AimingButton, ReloadButton, RunButton, RunButtonRight, JumpButton,
        CrouchButton, RollButton, PickItemButton, EnterVehicleButton, NextWeaponButton, PreviousWeaponButton, RightButton, LeftButton, ForwardButton, BackButton, BrakeButton;
        public void FindButtonsAndTouches()
        {
            //Screen Panels
            MobileScreenPanel = GameObject.Find("Mobile Screen");
            NormalScreenPanel = GameObject.Find("Normal Mobile Screen Panel");
            DrivingScreenPanel = GameObject.Find("Driving Mobile Screen Panel");

            //Touchfields
            RotateCameraTouchfield = GameObject.Find("Rotate Camera Touchfield").GetComponent<Touchfield>();
            ShotButtonTouchfield = GameObject.Find("ShotButton").GetComponent<Touchfield>();

            //Controll Buttons
            MovementJoystick = GameObject.Find("Joystick").GetComponent<JoystickVirtual>();
            ShotButton = GameObject.Find("ShotButton").GetComponent<ButtonVirtual>();
            AimingButton = GameObject.Find("AimingButton").GetComponent<ButtonVirtual>();
            JumpButton = GameObject.Find("JumpButton").GetComponent<ButtonVirtual>();
            RunButton = GameObject.Find("RunButton").GetComponent<ButtonVirtual>();
            RunButtonRight = GameObject.Find("RightRunButton").GetComponent<ButtonVirtual>();
            RollButton = GameObject.Find("RollButton").GetComponent<ButtonVirtual>();
            CrouchButton = GameObject.Find("CrouchButton").GetComponent<ButtonVirtual>();
            ReloadButton = GameObject.Find("ReloadButton").GetComponent<ButtonVirtual>();

            //Interact Buttons
            PickItemButton = GameObject.Find("InteractButton").GetComponent<ButtonVirtual>();
            EnterVehicleButton = GameObject.Find("Enter The Vehicle Button").GetComponent<ButtonVirtual>();

            //Weapon Switch
            PreviousWeaponButton = GameObject.Find("PreviousWeaponButton").GetComponent<ButtonVirtual>();
            NextWeaponButton = GameObject.Find("NextWeaponButton").GetComponent<ButtonVirtual>();

            //Driving Buttons
            RightButton = GameObject.Find("RightButton").GetComponent<ButtonVirtual>();
            LeftButton = GameObject.Find("LeftButton").GetComponent<ButtonVirtual>();
            ForwardButton = GameObject.Find("ForwardButton").GetComponent<ButtonVirtual>();
            BackButton = GameObject.Find("BackButton").GetComponent<ButtonVirtual>();
            BrakeButton = GameObject.Find("BrakeButton").GetComponent<ButtonVirtual>();
        }
        private void Update()
        {
            if (JUGameManager.IsMobileControls)
            {
                //Controls mobile screens and buttons that are not seen all the time, for example the get-in-car button.
                UpdateMobileScreen();
                UpdateMobileButtons();

                //Block Default Inputs
                if (JUInput.Instance().IsBlockingDefaultInputs == false)
                {
                    JUInput.Instance().EnableBlockStandardInputs();
                }

                //Rewrite Inputs Value
                RewriteGetButtonDown();
                RewriteGetButtonUp();
                RewriteGetButton();
                RewriteAxis();
            }
            else
            {
                MobileScreenPanel.SetActive(false);

                if (JUInput.Instance().IsBlockingDefaultInputs == true) { JUInput.Instance().DisableBlockStandardInputs(); }
            }
        }


        //Mobile Screens
        private void UpdateMobileScreen()
        {
            if (!JUGameManager.IsMobileControls) { MobileScreenPanel.SetActive(false); return; }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            MobileScreenPanel.SetActive(JUGameManager.IsMobileControls);
            NormalScreenPanel.SetActive(!JUGameManager.PlayerController.IsDriving);
            DrivingScreenPanel.SetActive(JUGameManager.PlayerController.IsDriving);
        }

        //Update Buttons On Screen View
        private void UpdateMobileButtons()
        {
            if (PickItemButton != null)
            {
                PickItemButton.gameObject.SetActive(JUGameManager.PlayerController.Inventory != null ? JUGameManager.PlayerController.Inventory.ItemToPickUp : false);
            }

            if (ReloadButton != null) ReloadButton.gameObject.SetActive((JUGameManager.PlayerController.WeaponInUseRightHand != null ||
                                               JUGameManager.PlayerController.WeaponInUseLeftHand != null) ? true : false);

            if (AimingButton != null) AimingButton.gameObject.SetActive((JUGameManager.PlayerController.WeaponInUseRightHand != null) ? true : false);

            if (ShotButton != null) ShotButton.gameObject.SetActive(ShowShotButtonOnlyUsingItem ? JUGameManager.PlayerController.IsItemEquiped : true);

            if (JUGameManager.PlayerController.TryGetComponent<DriveVehicles>(out var driver))
            {
                if (driver.IsDriving)
                {
                    EnterVehicleButton.gameObject.SetActive(driver.CanExitVehicle);
                }
                else
                {
                    Vehicle nearestVehicle = driver.NearestVehicle;
                    JUVehicleCharacterIK nearesVehicleCharacterIK = driver.NearestVehicleCharacterIK;

                    bool canDriveVehicle = nearestVehicle && driver.CanEnterVehicle;
                    if (nearesVehicleCharacterIK)
                        canDriveVehicle = !nearesVehicleCharacterIK.CharactersCanGetVehicle ? false : canDriveVehicle;

                    EnterVehicleButton.gameObject.SetActive(canDriveVehicle);
                }
            }
            else
            {
                EnterVehicleButton.gameObject.SetActive(false);
            }
        }

        //Rewrite all inputs
        private void RewriteAxis()
        {
            // >>> Rewrite Joystick Movement Vector
            if (MovementJoystick != null)
            {
                JUInput.RewriteInputAxis(JUInput.Axis.MoveHorizontal, Mathf.Clamp(MovementJoystick.Horizontal(), -1, 1));
                JUInput.RewriteInputAxis(JUInput.Axis.MoveVertical, Mathf.Clamp(MovementJoystick.Vertical(), -1, 1));
            }

            // >>> Driving Screen Buttons Mapping
            if (ForwardButton != null)
            {
                if (ForwardButton.IsPressed) JUInput.RewriteInputAxis(JUInput.Axis.MoveVertical, 1);
            }
            if (BackButton != null)
            {
                if (BackButton.IsPressed) JUInput.RewriteInputAxis(JUInput.Axis.MoveVertical, -1);
            }
            if (RightButton != null)
            {
                if (RightButton.IsPressed) JUInput.RewriteInputAxis(JUInput.Axis.MoveHorizontal, 1);
            }
            if (LeftButton != null)
            {
                if (LeftButton.IsPressed) JUInput.RewriteInputAxis(JUInput.Axis.MoveHorizontal, -1);
            }

            //Rotate Screen
            if (JUGameManager.IsMobileControls && RotateCameraTouchfield != null)
            {
                if (RightJoystick == null)
                {
                    if (ShotButtonTouchfield != null)
                    {
                        JUInput.RewriteInputAxis(JUInput.Axis.RotateHorizontal, RotateCameraTouchfield.TouchDistance.x / 5 + ShotButtonTouchfield.TouchDistance.x / 5);
                        JUInput.RewriteInputAxis(JUInput.Axis.RotateVertical, RotateCameraTouchfield.TouchDistance.y / 5 + ShotButtonTouchfield.TouchDistance.y / 5);
                    }
                    else
                    {
                        JUInput.RewriteInputAxis(JUInput.Axis.RotateHorizontal, RotateCameraTouchfield.TouchDistance.x / 5);
                        JUInput.RewriteInputAxis(JUInput.Axis.RotateVertical, RotateCameraTouchfield.TouchDistance.y / 5);
                    }
                }
                else
                {
                    JUInput.RewriteInputAxis(JUInput.Axis.RotateHorizontal, Mathf.Clamp(RightJoystick.Horizontal(), -1, 1));
                    JUInput.RewriteInputAxis(JUInput.Axis.RotateVertical, Mathf.Clamp(RightJoystick.Vertical(), -1, 1));
                }
            }
        }
        private void RewriteGetButtonDown()
        {
            // >>> Get Button Down


            if (ShotButton != null) JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.ShotButton, ShotButton.IsPressedDown);

            if (AimingButton != null) JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.AimingButton, AimingButton.IsPressedDown);

            if (RightJoystick != null && RightJoystickIsShootInput)
            {
                //Rewrite Button Down
                if (PressedRightJoystickDown == false)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.ShotButton, RightJoystick.IsPressed);
                    PressedRightJoystickDown = true;
                }

                //Reset State
                if (RightJoystick.IsPressed == false && PressedRightJoystickDown == true) PressedRightJoystickDown = false;
            }


            if (JumpButton != null)
            {
                if (JumpButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.JumpButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.JumpButton, false);
                }
            }

            if (RunButton != null)
            {
                if (RunButton.IsPressedDown || RunButtonRight.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.RunButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.RunButton, false);
                }
            }


            if (RollButton != null)
            {
                if (RollButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.RollButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.RollButton, false);
                }
            }


            if (CrouchButton != null)
            {
                if (CrouchButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.CrouchButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.CrouchButton, false);
                }
            }


            if (ReloadButton != null)
            {
                if (ReloadButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.ReloadButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.ReloadButton, false);
                }
            }


            if (PickItemButton != null)
            {
                if (PickItemButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.PickupButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.PickupButton, false);
                }
            }


            if (EnterVehicleButton != null)
            {
                if (EnterVehicleButton.IsPressedDown)
                {
                    if (JUGameManager.PlayerController.TryGetComponent<DriveVehicles>(out var driveVehicles))
                    {
                        if (driveVehicles.IsDriving) driveVehicles.ExitVehicle();
                        else driveVehicles.TryDriveNearestVehicle();
                    }
                }
            }

            if (NextWeaponButton != null)
            {
                if (NextWeaponButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.NextWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.NextWeaponButton, false);
                }
            }


            if (PreviousWeaponButton != null)
            {
                if (PreviousWeaponButton.IsPressedDown)
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.PreviousWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedDown(JUInput.Buttons.PreviousWeaponButton, false);
                }
            }
        }
        private void RewriteGetButton()
        {
            // >>> Get Button Down


            if (RightJoystick == null)
            {
                if (ShotButton != null) JUInput.RewriteInputButtonPressed(JUInput.Buttons.ShotButton, ShotButton.IsPressed);
            }
            else
            {
                if (ShotButton != null && RightJoystickIsShootInput == false)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.ShotButton, ShotButton.IsPressed);
                }
                if (RightJoystickIsShootInput == true && RightJoystick != null)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.ShotButton, RightJoystick.Intensity > RightJoystickShotSensibility);
                }
            }

            if (AimingButton != null) { JUInput.RewriteInputButtonPressed(JUInput.Buttons.AimingButton, AimingButton.IsPressed); }


            if (JumpButton != null)
            {
                if (JumpButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.JumpButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.JumpButton, false);
                }

            }

            if (BrakeButton != null && DrivingScreenPanel.activeInHierarchy == true)
            {
                if (BrakeButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.JumpButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.JumpButton, false);
                }
            }

            if (RunButton != null)
            {
                if (RunButton.IsPressed || RunButtonRight.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.RunButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.RunButton, false);
                }
            }


            if (RollButton != null)
            {
                if (RollButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.RollButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.RollButton, false);
                }
            }


            if (CrouchButton != null)
            {
                if (CrouchButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.CrouchButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.CrouchButton, false);
                }
            }


            if (ReloadButton != null)
            {
                if (ReloadButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.ReloadButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.ReloadButton, false);
                }
            }


            if (PickItemButton != null)
            {
                if (PickItemButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.PickupButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.PickupButton, false);
                }
            }


            if (EnterVehicleButton != null)
            {
                if (EnterVehicleButton.IsPressed)
                    if (JUGameManager.PlayerController.TryGetComponent<DriveVehicles>(out var driveVehicles))
                    {
                        if (driveVehicles.IsDriving) driveVehicles.ExitVehicle();
                        else driveVehicles.TryDriveNearestVehicle();
                    }
            }


            if (NextWeaponButton != null)
            {
                if (NextWeaponButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.NextWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.NextWeaponButton, false);
                }
            }


            if (PreviousWeaponButton != null)
            {
                if (PreviousWeaponButton.IsPressed)
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.PreviousWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressed(JUInput.Buttons.PreviousWeaponButton, false);
                }
            }
        }
        private void RewriteGetButtonUp()
        {
            // >>> Get Button Up

            if (JUGameManager.IsMobileControls)
            {
                if (ShotButton != null)
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.ShotButton, ShotButton.IsPressedUp);
                if (AimingButton != null)
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.AimingButton, AimingButton.IsPressedUp);
            }

            if (JumpButton != null)
            {
                if (JumpButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.JumpButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.JumpButton, false);
                }
            }

            if (RunButton != null)
            {
                if (RunButton.IsPressedUp || RunButtonRight.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.RunButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.RunButton, false);
                }
            }


            if (RollButton != null)
            {
                if (RollButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.RollButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.RollButton, false);
                }
            }


            if (CrouchButton != null)
            {
                if (CrouchButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.CrouchButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.CrouchButton, false);
                }
            }


            if (ReloadButton != null)
            {
                if (ReloadButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.ReloadButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.ReloadButton, false);
                }
            }


            if (PickItemButton != null)
            {
                if (PickItemButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.PickupButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.PickupButton, false);
                }
            }


            if (EnterVehicleButton != null)
            {
                if (EnterVehicleButton.IsPressedUp)
                    if (JUGameManager.PlayerController.TryGetComponent<DriveVehicles>(out var driveVehicles))
                    {
                        if (driveVehicles.IsDriving) driveVehicles.ExitVehicle();
                        else driveVehicles.TryDriveNearestVehicle();
                    }
            }

            if (NextWeaponButton != null)
            {
                if (NextWeaponButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.NextWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.NextWeaponButton, false);
                }
            }


            if (PreviousWeaponButton != null)
            {
                if (PreviousWeaponButton.IsPressedUp)
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.PreviousWeaponButton, true);
                }
                else
                {
                    JUInput.RewriteInputButtonPressedUp(JUInput.Buttons.PreviousWeaponButton, false);
                }
            }
        }

    }

}

namespace JUTPS.CustomEditors
{
#if UNITY_EDITOR
    [CustomEditor(typeof(MobileRig))]
    public class MobileRigEditor : Editor
    {
        private static readonly string[] _dontIncludeMe = new string[] { "m_Script" };

        public override void OnInspectorGUI()
        {
            MobileRig tg = (MobileRig)target;

            serializedObject.Update();

            if (GUILayout.Button(" ► Auto Setup", GUILayout.Height(30)))
            {
                tg.FindButtonsAndTouches();
            }
            DrawPropertiesExcluding(serializedObject, _dontIncludeMe);

            serializedObject.ApplyModifiedProperties();
        }
    }
#endif
}
