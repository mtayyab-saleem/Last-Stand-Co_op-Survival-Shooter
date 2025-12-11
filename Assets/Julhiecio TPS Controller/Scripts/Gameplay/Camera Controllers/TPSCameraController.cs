using UnityEngine;
using JUTPS.JUInputSystem;
using JUTPS.WeaponSystem;
using JUTPS.ActionScripts;
namespace JUTPS.CameraSystems
{

	[AddComponentMenu("JU TPS/Third Person System/Cameras/JU Third Person Camera Controller")]
	public class TPSCameraController : JUCameraController
	{

		public JUCharacterController characterTarget;

		[Header("Settings")]
		public bool FollowUpTarget;

		[Header("Auto Rotator Settings")]
		public bool EnableAutoRotator;
		public float AutoRotateTime = 5;
		public float AutoRotationSpeed = 4;

		public bool EnableVehicleAutoRotation;
		public float VehicleAutoRotateTime = 3;
		public float VehicleAutoRotationSpeed = 8;

		//[Header("WeaponSway")]
		public WeaponSwayOptions AimingSwaySettings = new WeaponSwayOptions(true, 1, 1, 1);

		//[Header("Camera States")]
		public CameraState NormalCameraState = new CameraState("Normal Camera State");
		public CameraState FireModeCameraState = new CameraState("Fire Mode Camera State", movementSpeed: 40);
		public CameraState AimModeCameraState = new CameraState("Scope Mode Camera State", 0, 15, 40, 0, 0, 0, 0, 0, 0, 2.5f);
		public CameraState DrivingVehicleCameraState = new CameraState("Driving Vehicle Camera State", 8, 25, 70, 1.5f, 0, 0, 0, 0, 0, 5, -20, 80);
		public CameraState DeadPlayerCameraState = new CameraState("Dead Player Camera State", 6, 5, 40, 0, 0, 0, 0, 0, 0, 2.5f, -30, 60);

		//Vehicle Auto rotation
		protected float CurrentTimeToAutoRotation;
		protected bool IsAutoRotationActivated;

		float xmouse;
		float ymouse;
		protected override void Start()
		{
			base.Start();
			//Get JU Character Controller reference
			if (TargetToFollow.TryGetComponent(out JUCharacterController JUcharacter)) { characterTarget = JUcharacter; TargetToFollow = characterTarget.HumanoidSpine; }
		}
		//Rotate camera and update camera states
		protected virtual void Update()
		{
			SetRotationInput();

			if (FollowUpTarget)
			{
				RotateCamera(xmouse, ymouse, upward: characterTarget == null ? TargetToFollow.up : characterTarget.transform.up);
			}
			else
			{
				RotateCamera(xmouse, ymouse);
			}

			if (EnableAutoRotator)
			{
				//Normal Auto Rotation
				if (characterTarget != null)
				{
					NormalAutoRotation(characterTarget);
				}
				else
				{
					NormalAutoRotation(TargetToFollow);
				}
			}

			//Camera State Update
			UpdateCharacterState(characterTarget);
			ChangeCameraStateAccordingCharacterState(CharacterState);
			SetCameraToScopePosition();
		}

		protected virtual void SetRotationInput()
		{
			if (Cursor.lockState != CursorLockMode.Locked && JUGameManager.IsMobileControls == false)
			{
				xmouse = 0;
				ymouse = 0;
				return;
			}

			xmouse = JUInput.GetAxis(JUInput.Axis.RotateVertical);
			ymouse = JUInput.GetAxis(JUInput.Axis.RotateHorizontal);
		}

		//Move camera pivot
		protected virtual void FixedUpdate()
		{
			//SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(TargetToFollow), true);
			if (characterTarget != null)
			{
				DriveVehicles driveAbility = characterTarget.DriveVehicleAbility;

				if (characterTarget.IsAiming) return;
				if (driveAbility && driveAbility.IsDriving && driveAbility.CurrentVehicle)
				{
					SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(driveAbility.CurrentVehicle.transform), true);
					//Driving Auto Rotation
					if (EnableVehicleAutoRotation) DrivingVehicleAutoRotation(driveAbility.CurrentVehicle);
				}
				else
				{
					SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(TargetToFollow), true);
				}
			}
			else
			{
				SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(TargetToFollow), true);
			}
		}

		//Move real camera and change camera states
		protected virtual void LateUpdate()
		{
			if (characterTarget) { if (characterTarget.IsAiming) return; }
			SetCameraPosition(GetCurrentCameraState.GetCameraPosition(mCamera.transform), false);
			SetCameraCollision(GetCurrentCameraState.CollisionLayers);
			SetFieldOfView(GetCurrentCameraState.CameraFieldOfView);
		}
		public override void RecoilReaction(float Force)
		{
			base.RecoilReaction(Force);

			if (characterTarget == null) return;

			Aiming = characterTarget.IsAiming;


		}

		//When Camera Rotate, vehicle auto rotation is off.
		protected override void OnCameraRotate()
		{
			StopAutoRotation();
		}

		protected enum PlayerStates { Normal, FireMode, Aiming, Driving, Dead }
		protected PlayerStates CharacterState;
		protected virtual void UpdateCharacterState(JUCharacterController character)
		{
			if (character == null) return;
			if (!character.IsAiming && !character.IsDriving && !character.FiringMode && !character.IsDead) { CharacterState = PlayerStates.Normal; }

			if (character.IsAiming) { CharacterState = PlayerStates.Aiming; }
			if (character.FiringMode) { CharacterState = PlayerStates.FireMode; }
			if (character.IsDriving) { CharacterState = PlayerStates.Driving; }
			if (character.IsDead) { CharacterState = PlayerStates.Dead; }
		}
		protected virtual void ChangeCameraStateAccordingCharacterState(PlayerStates characterState)
		{
			if (IsTransitioningToCustomState) return;

			switch (characterState)
			{
				case PlayerStates.Normal:
					SetCameraStateTransition(GetCurrentCameraState, NormalCameraState);
					break;
				case PlayerStates.FireMode:
					SetCameraStateTransition(GetCurrentCameraState, FireModeCameraState);
					break;
				case PlayerStates.Aiming:
					SetCameraStateTransition(GetCurrentCameraState, AimModeCameraState);
					break;
				case PlayerStates.Driving:
					SetCameraStateTransition(GetCurrentCameraState, DrivingVehicleCameraState);
					break;
				case PlayerStates.Dead:
					SetCameraStateTransition(GetCurrentCameraState, DeadPlayerCameraState);
					break;
			}
		}

		//[HideInInspector]Vector3 SmoothedScopeCameraPosition;
		float SmoothedXMouse;
		float SmoothedYMouse;
		protected virtual void SetCameraToScopePosition()
		{
			if (characterTarget == null) return;

			Aiming = characterTarget.IsAiming;

			if (characterTarget.IsItemEquiped == false) return;

			if (Aiming && characterTarget.WeaponInUseRightHand.AimMode != Weapon.WeaponAimMode.None && characterTarget.FiringMode)
			{
				var gun = characterTarget.WeaponInUseRightHand;

				SmoothedYMouse = Mathf.Lerp(SmoothedYMouse, ymouse, 10 * Time.deltaTime);
				SmoothedXMouse = Mathf.Lerp(SmoothedXMouse, xmouse, 10 * Time.deltaTime);
				float xSway = AimingSwaySettings.EnableWeaponSway ? (gun.CameraAimingPosition.x - AimingSwaySettings.GeneralIntensity * AimingSwaySettings.HorizontalIntensity * SmoothedYMouse / 80) : gun.CameraAimingPosition.x;
				float ySway = AimingSwaySettings.EnableWeaponSway ? (gun.CameraAimingPosition.y - AimingSwaySettings.GeneralIntensity * AimingSwaySettings.VerticalIntensity * SmoothedXMouse / 80) : gun.CameraAimingPosition.y;

				// > Weapon Sway Position
				Vector3 scopePosition = gun.transform.position
					+ gun.transform.right * xSway
					+ gun.transform.up * ySway
					+ mCamera.transform.parent.forward * gun.CameraAimingPosition.z;

				//Set Scope Position + Sway
				SetCameraPosition(scopePosition, false);

				//Set Field Of View
				AimModeCameraState.CameraFieldOfView = Mathf.Lerp(AimModeCameraState.CameraFieldOfView, gun.CameraFOV, 15 * Time.deltaTime);
				SetFieldOfView(AimModeCameraState.CameraFieldOfView);
			}
			else
			{
				//SmoothedScopeCameraPosition = mCamera.transform.position;
				AimModeCameraState.CameraFieldOfView = GetCurrentCameraState.CameraFieldOfView;
			}
		}
		protected virtual void NormalAutoRotation(JUCharacterController character)
		{
			if (character == null || EnableAutoRotator == false) return;
			if (character.FiringMode) { CurrentTimeToAutoRotation = 0; return; }
			if (character.IsMoving) CurrentTimeToAutoRotation += 2 * Time.deltaTime;
			AutoRotator(character.transform, AutoRotateTime, AutoRotationSpeed, AutoRotationSpeed);
		}
		protected virtual void NormalAutoRotation(Transform targetToFollow)
		{
			if (targetToFollow == null || EnableAutoRotator == false) return;

			AutoRotator(targetToFollow, AutoRotateTime, AutoRotationSpeed, AutoRotationSpeed);
		}
		protected virtual void DrivingVehicleAutoRotation(JUTPS.VehicleSystem.Vehicle drivingVehicle)
		{
			if (drivingVehicle == null) return;
			if (drivingVehicle.IsOn == false) return;
			AutoRotator(drivingVehicle.transform, VehicleAutoRotateTime, VehicleAutoRotationSpeed, VehicleAutoRotationSpeed);
		}
		public virtual void AutoRotator(Transform targetRotation, float MaxTimeToAutoRotation, float HorizontalSpeed = 5, float VerticalSpeed = 3, float AngleToStopAutoRotation = 90)
		{
			if (Vector3.Angle(targetRotation.up, Vector3.up) > AngleToStopAutoRotation)
			{
				Debug.Log("Disabled Camera Auto Rotation in angle " + AngleToStopAutoRotation);
				return;
			}
			if (IsAutoRotationActivated == true)
			{
				rotytarget = Mathf.LerpAngle(rotytarget, targetRotation.rotation.eulerAngles.y, HorizontalSpeed * Time.fixedDeltaTime);
				rotxtarget = Mathf.LerpAngle(rotxtarget, 0, VerticalSpeed * Time.fixedDeltaTime);
				if (FollowUpTarget)
				{
					RotateCamera(xmouse, ymouse, upward: characterTarget == null ? TargetToFollow.up : characterTarget.transform.up);
				}
				else
				{
					RotateCamera(xmouse, ymouse);
				}
			}
			else
			{
				CurrentTimeToAutoRotation += Time.deltaTime;
				if (CurrentTimeToAutoRotation >= MaxTimeToAutoRotation) { IsAutoRotationActivated = true; CurrentTimeToAutoRotation = 0; }
			}
		}
		public virtual void StopAutoRotation()
		{
			//Debug.Log("Stopped Camera Auto Rotation");
			CurrentTimeToAutoRotation = 0; IsAutoRotationActivated = false;
		}

		/// <summary>
		/// this will disable forever
		/// </summary>
		public virtual void DisableVehicleAutoRotation()
		{
			StopAutoRotation(); EnableVehicleAutoRotation = false;
		}
	}

}