using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using JUTPS.JUInputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem;

using JUTPS.CrossPlataform;

namespace JUTPS.JUInputSystem
{

	[AddComponentMenu("JU TPS/Input/JU Input Manager")]

	public class JUInputManager : MonoBehaviour
	{
		private JUTPSInputControlls _inputs;

		public JUTPSInputControlls InputActions
		{
			get
			{
				if (_inputs == null)
				{
					_inputs = new JUTPSInputControlls();
					_inputs.Enable();
					AddInputUpListeners(InputActions.Player);
				}

				return _inputs;
			}
		}

		private bool BlockStandardInputs;
		public bool IsBlockingDefaultInputs { get => BlockStandardInputs; }


		/// <summary>
		/// When calling this function it blocks the default JU Input Manager inputs, useful if you want to rewrite all input controls
		/// for example the MobileRig Script.
		/// </summary>
		public void EnableBlockStandardInputs() { BlockStandardInputs = true; }

		/// <summary>
		/// When calling this function, it disables blocking for the default JU Input Manager inputs.
		/// use EnableBlockStandardInputs to enable blocking default inputs.
		/// </summary>
		public void DisableBlockStandardInputs() { BlockStandardInputs = false; }

		//Move and Rotate Axis
		[HideInInspector] public float MoveHorizontal;
		[HideInInspector] public float MoveVertical;
		[HideInInspector] public float RotateHorizontal;
		[HideInInspector] public float RotateVertical;

		//>>> Input Bools
		//OnPressing
		[HideInInspector]
		public bool PressedShooting, PressedAiming, PressedReload, PressedRun, PressedJump, PressedPunch,
			 PressedCrouch, PressedProne, PressedRoll, PressedPickup, PressedInteract, PressedNextItem, PressedPreviousItem;
		//OnDown
		[HideInInspector]
		public bool PressedShootingDown, PressedAimingDown, PressedReloadDown, PressedRunDown, PressedJumpDown, PressedPunchDown,
			 PressedCrouchDown, PressedProneDown, PressedRollDown, PressedPickupDown, PressedInteractDown, PressedNextItemDown, PressedPreviousItemDown, PressedOpenInventoryDown;
		//OnUp
		[HideInInspector]
		public bool PressedShootingUp, PressedAimingUp, PressedReloadUp, PressedRunUp, PressedJumpUp, PressedPunchUp,
			 PressedCrouchUp, PressedProneUp, PressedRollUp, PressedPickupUp, PressedInteractUp, PressedNextItemUp, PressedPreviousItemUp;



		public CustomTouchButton[] CustomTouchButton;
		public CustomTouchfield[] CustomTouchfield;
		public CustomJoystickVirtual[] CustomJoystickVirtual;
		[Header("(Old Input System)")]
		public CustomInputButton[] CustomButton;

		public static bool IsUsingGamepad;
		private void Update()
		{
			double gamepad = Gamepad.current != null ? Gamepad.current.lastUpdateTime : 0;
			//If are mouse and keyboard conected |                  |if mouse last update are lower than keyboard last update   >        value = KeyboardLastUpdate    :else  >  value = MouseLastUpdate
			double keyboardAndMouseLastUsed = (Keyboard.current != null && Mouse.current != null) ? ((Mouse.current.lastUpdateTime < Keyboard.current.lastUpdateTime) ? Keyboard.current.lastUpdateTime : Mouse.current.lastUpdateTime) : 0;
			IsUsingGamepad = (gamepad > keyboardAndMouseLastUsed) ? true : false;

			if (BlockStandardInputs) return;

			UpdateGetButtonDown();
			UpdateGetButton();

			//In the new input system the "GetUp" method are now events (see the method "AddInputUpListeners" bellow)
			//UpdateGetButtonUp();

			UpdateAxis();
		}

		private void AddInputUpListeners(JUTPSInputControlls.PlayerActions input)
		{
			input.Run.performed += ctx => { PressedRunUp = false; };
			input.Run.canceled += ctx => { PressedRunUp = true; };

			input.Roll.performed += ctx => { PressedRollUp = false; };
			input.Roll.canceled += ctx => { PressedRollUp = true; };

			input.Jump.performed += ctx => { PressedJumpUp = false; };
			input.Jump.canceled += ctx => { PressedJumpUp = true; };

			input.Punch.performed += ctx => { PressedPunchUp = false; };
			input.Punch.canceled += ctx => { PressedPunchUp = true; };


			input.Crouch.performed += ctx => { PressedCrouchUp = false; };
			input.Crouch.canceled += ctx => { PressedCrouchUp = true; };

			input.Prone.performed += ctx => { PressedProneUp = false; };
			input.Prone.canceled += ctx => { PressedProneUp = true; };



			input.Fire.performed += ctx => { PressedShootingUp = false; };
			input.Fire.canceled += ctx => { PressedShootingUp = true; };

			input.Aim.performed += ctx => { PressedAimingUp = false; };
			input.Aim.canceled += ctx => { PressedAimingUp = true; };

			input.Reload.performed += ctx => { PressedReloadUp = false; };
			input.Reload.canceled += ctx => { PressedReloadUp = true; };



			input.Pickup.performed += ctx => { PressedPickupUp = false; };
			input.Pickup.canceled += ctx => { PressedPickupUp = true; };

			input.Interact.performed += ctx => { PressedInteractUp = false; };
			input.Interact.canceled += ctx => { PressedInteractUp = true; };

			input.Next.performed += ctx => { PressedNextItemUp = false; };
			input.Next.canceled += ctx => { PressedNextItemUp = true; };

			input.Previous.performed += ctx => { PressedPreviousItemUp = false; };
			input.Previous.canceled += ctx => { PressedPreviousItemUp = true; };
		}
		protected virtual void UpdateAxis()
		{
			// >>> OLD INPUT SYSTEM Joystick Movements
			//MoveHorizontal = Input.GetAxis("Horizontal");
			//MoveVertical = Input.GetAxis("Vertical");


			// >>> Joystick Movements
			MoveHorizontal = InputActions.Player.Move.ReadValue<Vector2>().x;
			MoveVertical = InputActions.Player.Move.ReadValue<Vector2>().y;

			MoveHorizontal = Mathf.Clamp(MoveHorizontal, -1, 1);
			MoveVertical = Mathf.Clamp(MoveVertical, -1, 1);


			if (JUTPS.JUGameManager.IsMobileControls)
			{
				if (IsBlockingDefaultInputs) Debug.LogWarning("In the Game Manager the ''IsMobile'' variable is set to true, but there is no script blocking the default inputs. Add a Mobile Rig from the prefabs folder or create one.");
			}
			else
			{
				RotateHorizontal = InputActions.Player.Look.ReadValue<Vector2>().x;
				RotateVertical = InputActions.Player.Look.ReadValue<Vector2>().y;

				//OLD INPUT SYSTEM
				//RotateHorizontal = Input.GetAxis("Mouse X");
				//RotateVertical = Input.GetAxis("Mouse Y");
			}

			//GamePadCrouchAxisControll();

			//PressedCrouch = actions.Player.Crouch.triggered;
			//PressedCrouch = actions.Player.Crouch.phase = UnityEngine.InputSystem.InputActionPhase.Started;
			//actions.Player.Crouch.canceled += ctx => { PressedCrouch = ctx.ReadValue<float>() == 1; Debug.Log("Exit"); };

			//if (PressedCrouch)
			// {
			//	Debug.Log("Enter");
			//}
		}

		/*
		/// <summary>
		/// On xbox controller the value of the down button is an axis(7th axis). 
		/// </summary>
		protected virtual void GamePadCrouchAxisControll()
		{
			//Crouch Gamepad
			if (Input.GetAxis("CrouchGamepad") < -0.9 && PressedCrouch == false)
			{
				PressedCrouch = true;
			}
			else
			{
				PressedCrouch = false;
			}
			if (Input.GetAxis("CrouchGamepad") > 0.9 && PressedCrouch == false)
			{
				PressedCrouchUp = true;
			}
			else
			{
				PressedCrouchUp = false;
			}
		}*/

		protected virtual void UpdateGetButtonDown()
		{
			PressedJumpDown = InputActions.Player.Jump.triggered;
			PressedRunDown = InputActions.Player.Run.triggered;
			PressedPunchDown = InputActions.Player.Punch.triggered;
			PressedRollDown = InputActions.Player.Roll.triggered;
			PressedProneDown = InputActions.Player.Prone.triggered;
			PressedCrouchDown = InputActions.Player.Crouch.triggered;

			PressedShootingDown = InputActions.Player.Fire.triggered;
			PressedAimingDown = InputActions.Player.Aim.triggered;
			PressedReloadDown = InputActions.Player.Reload.triggered;

			PressedPickupDown = InputActions.Player.Pickup.triggered;
			PressedInteractDown = InputActions.Player.Interact.triggered;
			PressedNextItemDown = InputActions.Player.Next.triggered;
			PressedPreviousItemDown = InputActions.Player.Previous.triggered;
			PressedOpenInventoryDown = InputActions.Player.OpenInventory.triggered;

			#region old code
			// >>> Get Button Down

			/*
			if (Input.GetButtonDown("Fire1"))
			{
				PressedShootingDown = true;
			}
			else
			{
				PressedShootingDown = false;
			}

			if (Input.GetButtonDown("Fire2"))
			{
				PressedAimingDown = true;
			}
			else
			{
				PressedAimingDown = false;
			}

			if (Input.GetButtonDown("Reload"))
			{
				PressedReloadDown = true;
			}
			else
			{
				PressedReloadDown = false;
			}

			if (Input.GetButtonDown("Jump"))
			{
				PressedJumpDown = true;
			}
			else
			{
				PressedJumpDown = false;
			}

			if (Input.GetButtonDown("Run"))
			{
				PressedRunDown = true;
			}
			else
			{
				PressedRunDown = false;
			}

			if (Input.GetButtonDown("Roll"))
			{
				PressedRollDown = true;
			}
			else
			{
				PressedRollDown = false;
			}

			if (Input.GetButtonDown("Crouch"))
			{
				PressedCrouchDown = true;
			}
			else
			{
				PressedCrouchDown = false;
			}
			if (Input.GetButtonDown("Prone"))
			{
				PressedProneDown = true;
			}
			else
			{
				PressedProneDown = false;
			}

			if (Input.GetButtonDown("Interact"))
			{
				PressedPickupWeaponDown = true;
			}
			else
			{
				PressedPickupWeaponDown = false;
			}

			if (Input.GetButtonDown("Interact"))
			{
				PressedEnterVehicleDown = true;
			}
			else
			{
				PressedEnterVehicleDown = false;
			}

			if (Input.GetButtonDown("Next"))
			{
				PressedNextWeaponDown = true;
			}
			else
			{
				PressedNextWeaponDown = false;
			}

			if (Input.GetButtonDown("Previous"))
			{
				PressedPreviousWeaponDown = true;
			}
			else
			{
				PressedPreviousWeaponDown = false;
			}
			*/
			#endregion
		}
		protected virtual void UpdateGetButton()
		{
			PressedJump = InputActions.Player.Jump.ReadValue<float>() == 1;
			PressedRun = InputActions.Player.Run.ReadValue<float>() == 1;
			PressedPunch = InputActions.Player.Punch.ReadValue<float>() == 1;
			PressedRoll = InputActions.Player.Roll.ReadValue<float>() == 1;
			PressedProne = InputActions.Player.Prone.ReadValue<float>() == 1;
			PressedCrouch = InputActions.Player.Crouch.ReadValue<float>() == 1;

			PressedShooting = InputActions.Player.Fire.ReadValue<float>() == 1;
			PressedAiming = InputActions.Player.Aim.ReadValue<float>() == 1;
			PressedReload = InputActions.Player.Reload.ReadValue<float>() == 1;

			PressedPickup = InputActions.Player.Pickup.ReadValue<float>() == 1;
			PressedInteract = InputActions.Player.Interact.ReadValue<float>() == 1;
			PressedNextItem = InputActions.Player.Next.ReadValue<float>() == 1;
			PressedPreviousItem = InputActions.Player.Previous.ReadValue<float>() == 1;

			//if(PressedProneDown) Debug.Log("hold to prone");

			#region old code
			/*
			// >>> Get Button 
			//this check is so that when you touch the touch of the cell phone the player does not shoot or aim unwantedly.

			if (Input.GetButton("Fire1"))
			{
				PressedShooting = true;
			}
			else
			{
				PressedShooting = false;
			}

			if (Input.GetButton("Fire2"))
			{
				PressedAiming = true;
			}
			else
			{
				PressedAiming = false;
			}


			if (Input.GetButton("Reload"))
			{
				PressedJump = true;
			}
			else
			{
				PressedJump = false;
			}

			if (Input.GetButton("Jump"))
			{
				PressedJump = true;
			}
			else
			{
				PressedJump = false;
			}

			if (Input.GetButton("Run"))
			{
				PressedRun = true;
			}
			else
			{
				PressedRun = false;
			}

			if (Input.GetButton("Roll"))
			{
				PressedRoll = true;
			}
			else
			{
				PressedRoll = false;
			}

			if (Input.GetButton("Crouch"))
			{
				PressedCrouch = true;
			}
			else
			{
				PressedCrouch = false;
			}


			if (Input.GetButton("Prone"))
			{
				PressedProne = true;
			}
			else
			{
				PressedProne = false;
			}

			if (Input.GetButton("Reload"))
			{
				PressedReload = true;
			}
			else
			{
				PressedReload = false;
			}

			if (Input.GetButton("Interact"))
			{
				PressedPickup = true;
			}
			else
			{
				PressedPickup = false;
			}

			if (Input.GetButton("Interact"))
			{
				PressedInteract = true;
			}
			else
			{
				PressedInteract = false;
			}

			if (Input.GetButton("Next"))
			{
				PressedNextWeapon = true;
			}
			else
			{
				PressedNextWeapon = false;
			}
			if (Input.GetButton("Previous"))
			{
				PressedPreviousWeapon = true;
			}
			else
			{
				PressedPreviousWeapon = false;
			}
			*/
			#endregion
		}

		/*
		protected virtual void UpdateGetButtonUp()
		{
			// >>> Get Button Up

			if (Input.GetButtonUp("Fire1"))
			{
				PressedShootingUp = true;
			}
			else
			{
				PressedShootingUp = false;
			}

			if (Input.GetButtonUp("Fire2"))
			{
				PressedAimingUp = true;
			}
			else
			{
				PressedAimingUp = false;
			}


			if (Input.GetButtonUp("Reload"))
			{
				PressedJumpUp = true;
			}
			else
			{
				PressedJumpUp = false;
			}

			if (Input.GetButtonUp("Jump"))
			{
				PressedJumpUp = true;
			}
			else
			{
				PressedJumpUp = false;
			}

			if (Input.GetButtonUp("Run"))
			{
				PressedRunUp = true;
			}
			else
			{
				PressedRunUp = false;
			}

			if (Input.GetButtonUp("Roll"))
			{
				PressedRollUp = true;
			}
			else
			{
				PressedRollUp = false;
			}

			if (Input.GetButtonUp("Crouch"))
			{
				PressedCrouchUp = true;
			}
			else
			{
				PressedCrouchUp = false;
			}

			if (Input.GetButtonUp("Prone"))
			{
				PressedProneUp = true;
			}
			else
			{
				PressedProneUp = false;
			}

			if (Input.GetButtonUp("Reload"))
			{
				PressedReloadUp = true;
			}
			else
			{
				PressedReloadUp = false;
			}

			if (Input.GetButtonUp("Interact"))
			{
				PressedPickupUp = true;
			}
			else
			{
				PressedPickupUp = false;
			}

			if (Input.GetButtonUp("Interact"))
			{
				PressedInteractUp = true;
			}
			else
			{
				PressedInteractUp = false;
			}

			if (Input.GetButtonUp("Next"))
			{
				PressedNextItemUp = true;
			}
			else
			{
				PressedNextItemUp = false;
			}
			if (Input.GetButtonUp("Previous"))
			{
				PressedPreviousItemUp = true;
			}
			else
			{
				PressedPreviousItemUp = false;
			}
		}
		*/
	}




	[System.Serializable]
	public class CustomInputButton
	{
		public string Name;
		[SerializeField] private KeyCode Input = KeyCode.P;
		public bool Pressed()
		{
			return UnityEngine.Input.GetKey(Input);
		}
		public bool PressedDown()
		{
			return UnityEngine.Input.GetKeyDown(Input);
		}
		public bool PressedUp()
		{
			return UnityEngine.Input.GetKeyUp(Input);
		}
	}

	[System.Serializable]
	public class CustomTouchButton
	{
		public string Name;
		[SerializeField] private ButtonVirtual ButtonInput = null;
		public bool Pressed()
		{
			return ButtonInput.IsPressed;
		}

		public bool PressedDown()
		{
			return ButtonInput.IsPressedDown;
		}

		public bool PressedUp()
		{
			return ButtonInput.IsPressedUp;
		}
	}

	[System.Serializable]
	public class CustomTouchfield
	{
		public string Name;
		[SerializeField] private Touchfield TouchfieldInput = null;
		public Vector2 TouchDistance()
		{
			return TouchfieldInput.TouchDistance;
		}
	}
	[System.Serializable]
	public class CustomJoystickVirtual
	{
		public string Name;
		[SerializeField] private JoystickVirtual Joystick = null;
		public Vector2 JoystickInput()
		{
			return Joystick.InputVector;
		}
	}
	public class JUInput
	{
		static JUInputManager JUInputInstance;
		static void GetJUInputInstance()
		{
			if (JUInputInstance != null) return;
			if (GameObject.FindObjectOfType<JUInputManager>() != null)
			{
				JUInputInstance = GameObject.FindObjectOfType<JUInputManager>();
			}
			else
			{
				JUInputManager NewJUInputManager = new GameObject("JU Input Manager").AddComponent<JUInputManager>();
				JUInputInstance = NewJUInputManager;
				Debug.Log("New JU Input Manager was created because none were found on the scene");
			}
		}

		/// <summary>
		/// Get JU Input Manager Instance
		/// </summary>
		/// <returns></returns>
		public static JUInputManager Instance()
		{
			if (JUInputInstance != null)
			{
				return JUInputInstance;
			}
			else
			{
				GetJUInputInstance();
				JUInputInstance = GameObject.FindObjectOfType<JUInputManager>();
				return JUInputInstance;
			}
		}
		public enum Axis { MoveHorizontal, MoveVertical, RotateHorizontal, RotateVertical }
		public enum Buttons
		{
			ShotButton, AimingButton, JumpButton, RunButton, PunchButton,
			RollButton, CrouchButton, ProneButton, ReloadButton,
			PickupButton, EnterVehicleButton, PreviousWeaponButton, NextWeaponButton, OpenInventory
		}

		/// <summary>
		/// Return default axis values.
		/// </summary>
		/// <returns></returns>
		public static float GetAxis(Axis axis)
		{
			GetJUInputInstance();
			switch (axis)
			{
				case Axis.MoveHorizontal:
					return JUInputInstance.MoveHorizontal;

				case Axis.MoveVertical:
					return JUInputInstance.MoveVertical;

				case Axis.RotateHorizontal:
					return JUInputInstance.RotateHorizontal;

				case Axis.RotateVertical:
					return JUInputInstance.RotateVertical;

				default:
					return 0;
			}
		}
		/// <summary>
		/// Returns the value of the default buttons when pressed down
		/// </summary>
		/// <returns></returns>
		public static bool GetButtonDown(Buttons Button)
		{
			GetJUInputInstance();
			switch (Button)
			{
				case Buttons.ShotButton:
					return JUInputInstance.PressedShootingDown;

				case Buttons.AimingButton:
					return JUInputInstance.PressedAimingDown;

				case Buttons.JumpButton:
					return JUInputInstance.PressedJumpDown;

				case Buttons.RunButton:
					return JUInputInstance.PressedRunDown;

				case Buttons.PunchButton:
					return JUInputInstance.PressedPunchDown;

				case Buttons.RollButton:
					return JUInputInstance.PressedRollDown;

				case Buttons.CrouchButton:
					return JUInputInstance.PressedCrouchDown;

				case Buttons.ProneButton:
					return JUInputInstance.PressedProneDown;

				case Buttons.ReloadButton:
					return JUInputInstance.PressedReloadDown;

				case Buttons.PickupButton:
					return JUInputInstance.PressedPickupDown;

				case Buttons.EnterVehicleButton:
					return JUInputInstance.PressedInteractDown;

				case Buttons.PreviousWeaponButton:
					return JUInputInstance.PressedPreviousItemDown;

				case Buttons.NextWeaponButton:
					return JUInputInstance.PressedNextItemDown;
				case Buttons.OpenInventory:
					return JUInputInstance.PressedOpenInventoryDown;

				default:
					return false;

			}
		}
		/// <summary>
		/// Returns the value of the default buttons when pressed
		/// </summary>
		/// <returns></returns>
		public static bool GetButton(Buttons Button)
		{
			GetJUInputInstance();
			switch (Button)
			{
				case Buttons.ShotButton:
					return JUInputInstance.PressedShooting;

				case Buttons.AimingButton:
					return JUInputInstance.PressedAiming;

				case Buttons.JumpButton:
					return JUInputInstance.PressedJump;

				case Buttons.RunButton:
					return JUInputInstance.PressedRun;

				case Buttons.PunchButton:
					return JUInputInstance.PressedPunch;

				case Buttons.RollButton:
					return JUInputInstance.PressedRoll;

				case Buttons.CrouchButton:
					return JUInputInstance.PressedCrouch;

				case Buttons.ProneButton:
					return JUInputInstance.PressedProne;

				case Buttons.ReloadButton:
					return JUInputInstance.PressedReload;

				case Buttons.PickupButton:
					return JUInputInstance.PressedPickup;

				case Buttons.EnterVehicleButton:
					return JUInputInstance.PressedInteract;

				case Buttons.PreviousWeaponButton:
					return JUInputInstance.PressedPreviousItem;

				case Buttons.NextWeaponButton:
					return JUInputInstance.PressedNextItem;


				default:
					return false;

			}
		}
		/// <summary>
		/// Returns the value of the default buttons when pressed up
		/// </summary>
		/// <returns></returns>
		public static bool GetButtonUp(Buttons Button)
		{
			GetJUInputInstance();
			switch (Button)
			{
				case Buttons.ShotButton:
					return JUInputInstance.PressedShootingUp;

				case Buttons.AimingButton:
					return JUInputInstance.PressedAimingUp;

				case Buttons.JumpButton:
					return JUInputInstance.PressedJumpUp;

				case Buttons.RunButton:
					return JUInputInstance.PressedRunUp;

				case Buttons.PunchButton:
					return JUInputInstance.PressedPunchUp;

				case Buttons.RollButton:
					return JUInputInstance.PressedRollUp;

				case Buttons.CrouchButton:
					return JUInputInstance.PressedCrouchUp;

				case Buttons.ProneButton:
					return JUInputInstance.PressedProneUp;

				case Buttons.ReloadButton:
					return JUInputInstance.PressedReloadUp;

				case Buttons.PickupButton:
					return JUInputInstance.PressedPickupUp;

				case Buttons.EnterVehicleButton:
					return JUInputInstance.PressedInteractUp;

				case Buttons.PreviousWeaponButton:
					return JUInputInstance.PressedPreviousItemUp;

				case Buttons.NextWeaponButton:
					return JUInputInstance.PressedNextItemUp;


				default:
					return false;

			}
		}

		/// <summary>
		/// Returns the value of the custom buttons when pressed
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomButton(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomButton.Length; i++)
			{
				if (JUInputInstance.CustomButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomButton[i].Pressed();
				}
				if (JUInputInstance.CustomButton[i].Name != CustomButtonName && i == JUInputInstance.CustomButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}
		/// <summary>
		/// Returns the value of the custom buttons when pressed down
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomButtonDown(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomButton.Length; i++)
			{
				if (JUInputInstance.CustomButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomButton[i].PressedDown();
				}
				if (JUInputInstance.CustomButton[i].Name != CustomButtonName && i == JUInputInstance.CustomButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}

		/// <summary>
		/// Returns the value of the custom buttons when pressed up
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomButtonUp(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomButton.Length; i++)
			{
				if (JUInputInstance.CustomButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomButton[i].PressedUp();
				}
				if (JUInputInstance.CustomButton[i].Name != CustomButtonName && i == JUInputInstance.CustomButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}


		/// <summary>
		/// Returns the value of the custom touch buttons when pressed
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom touch button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomTouchButton(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomTouchButton.Length; i++)
			{
				if (JUInputInstance.CustomTouchButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomTouchButton[i].Pressed();
				}
				if (JUInputInstance.CustomTouchButton[i].Name != CustomButtonName && i == JUInputInstance.CustomTouchButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}

		/// <summary>
		/// Returns the value of the custom touch buttons when pressed down
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom touch button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomTouchButtonDown(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomTouchButton.Length; i++)
			{
				if (JUInputInstance.CustomTouchButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomTouchButton[i].PressedDown();
				}
				if (JUInputInstance.CustomTouchButton[i].Name != CustomButtonName && i == JUInputInstance.CustomTouchButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}

		/// <summary>
		/// Returns the value of the custom touch buttons when pressed up
		/// </summary>
		/// <param name="CustomButtonName"> The name of the custom touch button, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static bool GetCustomTouchButtonUp(string CustomButtonName)
		{
			bool value = false;
			for (int i = 0; i < JUInputInstance.CustomTouchButton.Length; i++)
			{
				if (JUInputInstance.CustomTouchButton[i].Name == CustomButtonName)
				{
					value = JUInputInstance.CustomTouchButton[i].PressedUp();
				}
				if (JUInputInstance.CustomTouchButton[i].Name != CustomButtonName && i == JUInputInstance.CustomTouchButton.Length)
				{
					Debug.Log("Could not find an input with this name");
					value = false;
				}
			}

			return value;
		}

		public static bool GetKeyDown(UnityEngine.InputSystem.Controls.KeyControl Key)
		{
			return Key.isPressed;
		}
		public static Vector2 GetMousePosition()
		{
			if (Instance() == null) return Vector2.zero;
			if (Instance().InputActions == null) return Vector2.zero;
			return Instance().InputActions.Player.MousePosition.ReadValue<Vector2>();
		}
		public static int GetTouchsLengh()
		{
			int touches = -1;
			if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled == false)
			{
				UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
				UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
				Debug.Log("Started Touch Simulation");
				touches = Touchscreen.current.touches.Count;
			}
			else
			{
				touches = Touchscreen.current.touches.Count;
			}
			return touches;
		}
		public static UnityEngine.InputSystem.Controls.TouchControl[] GetTouches()
		{
			if (UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.enabled == false)
			{
				UnityEngine.InputSystem.EnhancedTouch.EnhancedTouchSupport.Enable();
				UnityEngine.InputSystem.EnhancedTouch.TouchSimulation.Enable();
				Debug.Log("Started Touch Simulation");
			}
			return Touchscreen.current.touches.ToArray();
		}

		/// <summary>
		/// Returns the value of the custom Touchfield
		/// </summary>
		/// <param name="CustomTouchfield"> The name of the custom Touchfield, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static Vector2 GetCustomTouchfieldAxis(string CustomTouchfield)
		{
			Vector2 value = new Vector2(0, 0);
			for (int i = 0; i < JUInputInstance.CustomTouchfield.Length; i++)
			{
				if (JUInputInstance.CustomTouchfield[i].Name == CustomTouchfield)
				{
					value = JUInputInstance.CustomTouchfield[i].TouchDistance();
				}
				if (JUInputInstance.CustomTouchfield[i].Name != CustomTouchfield && i == JUInputInstance.CustomTouchfield.Length)
				{
					Debug.Log("Could not find an input with this name");
				}
			}

			return value;
		}


		/// <summary>
		/// Returns the value of the custom Joystick
		/// </summary>
		/// <param name="CustomJoystickName"> The name of the custom Joystick, it must be set to the same name in the JU Input Manager </param>
		/// <returns></returns>
		public static Vector2 GetCustomVirtualJoystickAxis(string CustomJoystickName)
		{
			Vector2 value = new Vector2(0, 0);
			for (int i = 0; i < JUInputInstance.CustomJoystickVirtual.Length; i++)
			{
				if (JUInputInstance.CustomJoystickVirtual[i].Name == CustomJoystickName)
				{
					value = JUInputInstance.CustomJoystickVirtual[i].JoystickInput();
				}
				if (JUInputInstance.CustomJoystickVirtual[i].Name != CustomJoystickName && i == JUInputInstance.CustomJoystickVirtual.Length)
				{
					Debug.Log("Could not find an input with this name");
				}
			}

			return value;
		}

		/// <summary>
		/// Rewrite the value of a default JU input axis
		/// </summary>
		/// <param name="axis"> The axis thar will be rewritten </param>
		/// <param name="AxisValue">value that will rewrite</param>
		public static void RewriteInputAxis(Axis axis, float AxisValue)
		{
			GetJUInputInstance();
			switch (axis)
			{
				case Axis.MoveHorizontal:
					JUInputInstance.MoveHorizontal = AxisValue;
					break;

				case Axis.MoveVertical:
					JUInputInstance.MoveVertical = AxisValue;
					break;

				case Axis.RotateHorizontal:
					JUInputInstance.RotateHorizontal = AxisValue;
					break;

				case Axis.RotateVertical:
					JUInputInstance.RotateVertical = AxisValue;
					break;

				default:
					Debug.LogWarning("No axis is being rewritten");
					break;

			}
		}

		/// <summary>
		/// Rewrite the value of a default JU input button
		/// </summary>
		/// <param name="button"> The button thar will be rewritten </param>
		/// <param name="ButtonValue">value that will rewrite</param>
		public static void RewriteInputButtonPressed(Buttons button, bool ButtonValue)
		{
			GetJUInputInstance();
			switch (button)
			{
				case Buttons.ShotButton:
					JUInputInstance.PressedShooting = ButtonValue;
					break;

				case Buttons.AimingButton:
					JUInputInstance.PressedAiming = ButtonValue;
					break;

				case Buttons.JumpButton:
					JUInputInstance.PressedJump = ButtonValue;
					break;

				case Buttons.RunButton:
					JUInputInstance.PressedRun = ButtonValue;
					break;

				case Buttons.RollButton:
					JUInputInstance.PressedRoll = ButtonValue;
					break;

				case Buttons.CrouchButton:
					JUInputInstance.PressedCrouch = ButtonValue;
					break;

				case Buttons.ReloadButton:
					JUInputInstance.PressedReload = ButtonValue;
					break;

				case Buttons.PickupButton:
					JUInputInstance.PressedPickup = ButtonValue;
					break;

				case Buttons.EnterVehicleButton:
					JUInputInstance.PressedInteract = ButtonValue;
					break;

				case Buttons.PreviousWeaponButton:
					JUInputInstance.PressedPreviousItem = ButtonValue;
					break;

				case Buttons.NextWeaponButton:
					JUInputInstance.PressedNextItem = ButtonValue;
					break;


				default:
					Debug.LogWarning("No button is being rewritten");
					break;
			}
		}
		/// <summary>
		/// Rewrite the value of a default JU input button
		/// </summary>
		/// <param name="button"> The button thar will be rewritten </param>
		/// <param name="ButtonValue">value that will rewrite</param>
		public static void RewriteInputButtonPressedDown(Buttons button, bool ButtonValue)
		{
			GetJUInputInstance();
			switch (button)
			{
				case Buttons.ShotButton:
					JUInputInstance.PressedShootingDown = ButtonValue;
					break;

				case Buttons.AimingButton:
					JUInputInstance.PressedAimingDown = ButtonValue;
					break;

				case Buttons.JumpButton:
					JUInputInstance.PressedJumpDown = ButtonValue;
					break;

				case Buttons.RunButton:
					JUInputInstance.PressedRunDown = ButtonValue;
					break;

				case Buttons.RollButton:
					JUInputInstance.PressedRollDown = ButtonValue;
					break;

				case Buttons.CrouchButton:
					JUInputInstance.PressedCrouchDown = ButtonValue;
					break;

				case Buttons.ReloadButton:
					JUInputInstance.PressedReloadDown = ButtonValue;
					break;

				case Buttons.PickupButton:
					JUInputInstance.PressedPickupDown = ButtonValue;
					break;

				case Buttons.EnterVehicleButton:
					JUInputInstance.PressedInteractDown = ButtonValue;
					break;

				case Buttons.PreviousWeaponButton:
					JUInputInstance.PressedPreviousItemDown = ButtonValue;
					break;

				case Buttons.NextWeaponButton:
					JUInputInstance.PressedNextItemDown = ButtonValue;
					break;


				default:
					Debug.LogWarning("No button down is being rewritten");
					break;
			}
		}
		/// <summary>
		/// Rewrite the value of a default JU input button
		/// </summary>
		/// <param name="button"> The button thar will be rewritten </param>
		/// <param name="ButtonValue">value that will rewrite</param>
		public static void RewriteInputButtonPressedUp(Buttons button, bool ButtonValue)
		{
			GetJUInputInstance();
			switch (button)
			{
				case Buttons.ShotButton:
					JUInputInstance.PressedShootingUp = ButtonValue;
					break;

				case Buttons.AimingButton:
					JUInputInstance.PressedAimingUp = ButtonValue;
					break;

				case Buttons.JumpButton:
					JUInputInstance.PressedJumpUp = ButtonValue;
					break;

				case Buttons.RunButton:
					JUInputInstance.PressedRunUp = ButtonValue;
					break;

				case Buttons.RollButton:
					JUInputInstance.PressedRollUp = ButtonValue;
					break;

				case Buttons.CrouchButton:
					JUInputInstance.PressedCrouchUp = ButtonValue;
					break;

				case Buttons.ReloadButton:
					JUInputInstance.PressedReloadUp = ButtonValue;
					break;

				case Buttons.PickupButton:
					JUInputInstance.PressedPickupUp = ButtonValue;
					break;

				case Buttons.EnterVehicleButton:
					JUInputInstance.PressedInteractUp = ButtonValue;
					break;

				case Buttons.PreviousWeaponButton:
					JUInputInstance.PressedPreviousItemUp = ButtonValue;
					break;

				case Buttons.NextWeaponButton:
					JUInputInstance.PressedNextItemUp = ButtonValue;
					break;


				default:
					Debug.LogWarning("No button up is being rewritten");
					break;
			}
		}

	}


}
