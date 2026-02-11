using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.CameraSystems
{

	[AddComponentMenu("JU TPS/Third Person System/Cameras/JU TopDown Camera Controller")]
	public class TDCameraController : JUCameraController
	{
		[HideInInspector] public JUCharacterController PlayerTarget;

		[Header("Default Camera States")]
		public CameraState NormalCameraState = new CameraState("Normal Camera State", 15, 15, 50, 0, 0, 0, 0, 0, 0);
		public CameraState FireModeCameraState = new CameraState("Fire Mode Camera State", 15, 15, 50, 0, 0, 0, 0, 0, 0);
		public CameraState AimModeCameraState = new CameraState("Scope Mode Camera State", 15, 15, 50, 0, 0, 0, 0, 0, 0);
		public CameraState DrivingVehicleCameraState = new CameraState("Driving Vehicle Camera State", 15, 15, 50, 0, 0, 0, 0, 0, 0);
		public CameraState DeadPlayerCameraState = new CameraState("Dead Player Camera State", 15, 15, 30, 0, 0, 0, 0, 0, 0);

		protected override void Start()
		{
			base.Start();
			//Get JU Character Controller reference
			if (TargetToFollow != null)
			{
				if (TargetToFollow.TryGetComponent(out JUCharacterController JUcharacter))
				{
					PlayerTarget = JUcharacter; TargetToFollow = PlayerTarget.HumanoidSpine;
				}
			}
		}
		//update camera states
		protected virtual void Update()
		{
			UpdateCharacterState(PlayerTarget ? PlayerTarget : null);
			ChangeCameraStateAccordingCharacterState(CharacterState);
		}

		//Move camera pivot
		protected virtual void FixedUpdate()
		{
			if (TargetToFollow == null) return;

			SetPivotCameraPosition(GetCurrentCameraState.GetCameraPivotPosition(TargetToFollow), true);
		}

		//Move real camera and change camera states
		protected virtual void LateUpdate()
		{
			SetCameraPosition(GetCurrentCameraState.GetCameraPosition(mCamera.transform), false);
			SetFieldOfView(GetCurrentCameraState.CameraFieldOfView);
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
	}

}