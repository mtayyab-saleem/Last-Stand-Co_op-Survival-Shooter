using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//using JU_INPUT_SYSTEM;

namespace JUTPS.CameraSystems
{

	#region Camera State Base Class
	[System.Serializable]
	public class CameraState
	{
		[Header("Settings")]
		public string StateName = "Camera State";
		public float Distance;
		public float MovementSpeed;

		[Header("Camera FOV")]
		public float CameraFieldOfView;

		[Header("Camera Pivot Position Offsets")]
		public float UpTargetOffset = 1;
		public float RightTargetOffset = 0;
		public float ForwardTargetOffset;


		[Header("Camera Position Adjustment")]
		public float RightCameraOffset = 0.6f;
		public float UpCameraOffset = 0.45f;
		public float ForwardCameraOffset = 0f;

		[Header("Camera Rotation Settings")]
		public float RotationSensibility = 1f;
		public float VerticalRotationSensibility = 0.7f;

		public float MaxRotation = -80;
		public float MinRotation = 80;

		[Header("Camera Layer Collisions")]
		public LayerMask CollisionLayers;

		public Vector3 GetCameraPivotPosition(Transform target)
		{
			return (target != null) ? target.position + target.up * UpTargetOffset + target.forward * ForwardTargetOffset + target.right * RightTargetOffset : Vector3.zero;
		}
		public Vector3 GetCameraPosition(Transform camera)
		{
			return camera.parent.position - camera.forward * (Distance + ForwardCameraOffset) + camera.right * RightCameraOffset + camera.up * UpCameraOffset;
		}

		public CameraState(string stateName, float distance = 3, float movementSpeed = 15, float cameraFielOfView = 60, float upOffset = 0f, float rightOffset = 0, float forwardOffset = 0f, float xAdjust = 0.6f, float yAdjust = 0.6f, float zAdjust = 0, float rotationSensibility = 5f, float minRotation = -80, float maxRotation = 80)
		{
			StateName = stateName;
			Distance = distance;
			MovementSpeed = movementSpeed;

			CameraFieldOfView = cameraFielOfView;

			UpTargetOffset = upOffset;
			RightTargetOffset = rightOffset;
			ForwardTargetOffset = forwardOffset;

			RightCameraOffset = xAdjust;
			UpCameraOffset = yAdjust;
			ForwardCameraOffset = zAdjust;

			RotationSensibility = rotationSensibility;
			MinRotation = minRotation;
			MaxRotation = maxRotation;

			SettingsIDName = stateName;
		}

		[HideInInspector] public string SettingsIDName;
	}
	#endregion

	[System.Serializable]
	public struct WeaponSwayOptions
	{
		public bool EnableWeaponSway;
		public float GeneralIntensity;
		public float HorizontalIntensity;
		public float VerticalIntensity;
		public WeaponSwayOptions(bool enable, float intensity, float horizontalIntensity, float verticalIntensity)
		{
			this.EnableWeaponSway = enable;
			this.GeneralIntensity = intensity;
			this.HorizontalIntensity = horizontalIntensity;
			this.VerticalIntensity = verticalIntensity;
		}
	}
	public class JUCameraController : MonoBehaviour
	{
		[HideInInspector] public bool Aiming;
		[HideInInspector] public bool IsTransitioningToCustomState;
		[HideInInspector] public bool IsTargetInForward;
		[HideInInspector] public Camera mCamera;

		[Header("Camera Settings")]
		public Transform TargetToFollow;
		public LayerMask CameraCollisionLayerMask;
		public LayerMask CrosshairRaycastLayerMask;

		public bool LockCursor = true;
		public bool HideCursor = true;
		//[Header("Custom Camera States")]
		public CameraState[] CustomCameraStates = new CameraState[] { new CameraState("Example State", distance: 2, cameraFielOfView: 80) };

		//Current Camera State
		private CameraState CurrentCameraState = new CameraState("Standard Camera State");
		public CameraState GetCurrentCameraState { get => CurrentCameraState; }


		#region Camera Rotation
		[Header("Camera Rotation")]
		[Range(0, 5)] public float GeneralSensibility = 1;
		[Range(0, 5)] public float GeneralVerticalSensibility = 1;
		public bool InvertHorizontal;
		public bool InvertVertical;

		//Smoothed camera rotation axis
		[HideInInspector] public float rotX;
		[HideInInspector] public float rotY;

		//Camera target rotation axis
		[HideInInspector] public float rotxtarget;
		[HideInInspector] public float rotytarget;
		#endregion

		#region Camera Recoil
		[Header("Camera Recoil Reaction")]
		public bool CameraRecoilReaction = true;
		public bool RecoilRotateCamera = false;
		[Range(0, 2)] public float CameraRecoilSensibility = 0.5f;

		#endregion

		#region Events
#pragma warning disable 67
		public static event System.Action event_OnCameraRotate;
		public static event System.Action event_OnCameraMove;
		public static event System.Action event_OnCameraStateChange;
#pragma warning restore 67
		#endregion

		protected virtual void OnEnable()
		{
			//Add events callbacks
			event_OnCameraRotate += OnCameraRotate;
			event_OnCameraMove += OnCameraMove;
			event_OnCameraStateChange += OnCameraStateChange;
		}
		protected virtual void OnDestroy()
		{
			//Remove events callbacks
			event_OnCameraRotate -= OnCameraRotate;
			event_OnCameraMove -= OnCameraMove;
			event_OnCameraStateChange -= OnCameraStateChange;
		}

		/// <summary>
		/// necessary references are defined in awake, override carefully.
		/// </summary>
		protected virtual void Start()
		{
			//Get camera
			mCamera = gameObject.GetComponentInChildren<Camera>();

			//Find player
			GameObject player = GameObject.FindGameObjectWithTag("Player");
			if (TargetToFollow == null && player != null) { TargetToFollow = player.transform; }
			//Set Start Rotation
			if (TargetToFollow != null) { SetCameraRotation(0, TargetToFollow.eulerAngles.y, false); }

			LockMouse(LockCursor, HideCursor);
			Time.fixedDeltaTime = 0.015f;
		}


		protected virtual void OnCameraRotate()
		{

		}
		protected virtual void OnCameraMove()
		{

		}
		protected virtual void OnCameraStateChange()
		{

		}

		/// <summary>
		/// Make a transition between camera states. If you don't want a transition and just set the values, set the speed parameter to -1.
		/// </summary>
		/// <param name="current">Current Camera State</param>
		/// <param name="target">Target Camera State</param>
		/// <param name="speed">Speed of Transition</param>
		/// <param name="lerp">If true, it will make a smooth transition, if not, a linear transition</param>
		public void SetCameraStateTransition(CameraState current, CameraState target, float speed = 8, bool lerp = true)
		{
			if (speed != -1)
			{
				switch (lerp)
				{
					case true:
						current.Distance = Mathf.Lerp(current.Distance, target.Distance, speed * Time.deltaTime);
						current.MovementSpeed = Mathf.Lerp(current.MovementSpeed, target.MovementSpeed, speed * Time.deltaTime);

						current.UpTargetOffset = Mathf.Lerp(current.UpTargetOffset, target.UpTargetOffset, speed * Time.deltaTime);
						current.ForwardTargetOffset = Mathf.Lerp(current.ForwardTargetOffset, target.ForwardTargetOffset, speed * Time.deltaTime);
						current.RightTargetOffset = Mathf.Lerp(current.RightTargetOffset, target.RightTargetOffset, speed * Time.deltaTime);

						current.CameraFieldOfView = Mathf.Lerp(current.CameraFieldOfView, target.CameraFieldOfView, speed * Time.deltaTime);

						current.RightCameraOffset = Mathf.Lerp(current.RightCameraOffset, target.RightCameraOffset, speed * Time.deltaTime);
						current.UpCameraOffset = Mathf.Lerp(current.UpCameraOffset, target.UpCameraOffset, speed * Time.deltaTime);
						current.ForwardCameraOffset = Mathf.Lerp(current.ForwardCameraOffset, target.ForwardCameraOffset, speed * Time.deltaTime);

						current.RotationSensibility = Mathf.Lerp(current.RotationSensibility, target.RotationSensibility, speed * Time.deltaTime);
						current.VerticalRotationSensibility = Mathf.Lerp(current.VerticalRotationSensibility, target.VerticalRotationSensibility, speed * Time.deltaTime);

						current.MaxRotation = Mathf.Lerp(current.MaxRotation, target.MaxRotation, speed * Time.deltaTime);
						current.MinRotation = Mathf.Lerp(current.MinRotation, target.MinRotation, speed * Time.deltaTime);
						break;
					case false:
						current.Distance = Mathf.MoveTowards(current.Distance, target.Distance, speed * Time.deltaTime);
						current.MovementSpeed = Mathf.MoveTowards(current.MovementSpeed, target.MovementSpeed, speed * Time.deltaTime);

						current.UpTargetOffset = Mathf.MoveTowards(current.UpTargetOffset, target.UpTargetOffset, speed * Time.deltaTime);
						current.ForwardTargetOffset = Mathf.MoveTowards(current.ForwardTargetOffset, target.ForwardTargetOffset, speed * Time.deltaTime);
						current.RightTargetOffset = Mathf.MoveTowards(current.RightTargetOffset, target.RightTargetOffset, speed * Time.deltaTime);

						current.CameraFieldOfView = Mathf.MoveTowards(current.CameraFieldOfView, target.CameraFieldOfView, speed * Time.deltaTime);

						current.RightCameraOffset = Mathf.MoveTowards(current.RightCameraOffset, target.RightCameraOffset, speed * Time.deltaTime);
						current.UpCameraOffset = Mathf.MoveTowards(current.UpCameraOffset, target.UpCameraOffset, speed * Time.deltaTime);
						current.ForwardCameraOffset = Mathf.MoveTowards(current.ForwardCameraOffset, target.ForwardCameraOffset, speed * Time.deltaTime);

						current.RotationSensibility = Mathf.MoveTowards(current.RotationSensibility, target.RotationSensibility, speed * Time.deltaTime);
						current.VerticalRotationSensibility = Mathf.MoveTowards(current.VerticalRotationSensibility, target.VerticalRotationSensibility, speed * Time.deltaTime);

						current.MaxRotation = Mathf.MoveTowards(current.MaxRotation, target.MaxRotation, speed * Time.deltaTime);
						current.MinRotation = Mathf.MoveTowards(current.MinRotation, target.MinRotation, speed * Time.deltaTime);
						break;
				}
			}
			else
			{
				current.Distance = target.Distance;
				current.MovementSpeed = target.MovementSpeed;

				current.UpTargetOffset = target.UpTargetOffset;
				current.ForwardTargetOffset = target.ForwardTargetOffset;
				current.RightTargetOffset = target.RightTargetOffset;

				current.CameraFieldOfView = target.CameraFieldOfView;

				current.RightCameraOffset = target.RightCameraOffset;
				current.UpCameraOffset = target.UpCameraOffset;
				current.ForwardCameraOffset = target.ForwardCameraOffset;

				current.RotationSensibility = target.RotationSensibility;
				current.VerticalRotationSensibility = target.RotationSensibility;

				current.MaxRotation = target.MaxRotation;
				current.MinRotation = target.MinRotation;
			}
			//Debug.Log("target:" + target.StateName);
			if (current.Distance == target.Distance && current.MovementSpeed == target.MovementSpeed && current.SettingsIDName != target.SettingsIDName)
			{
				OnCameraStateChange();
				current.SettingsIDName = target.SettingsIDName;
			}


			current.CollisionLayers = target.CollisionLayers;
		}

		/// <summary>
		/// Make a transition to custom Camera States. It is used in Custom Camera State Trigger, if you want to create a different camera state change method, remember to call "DisableCustomStateTransitioningState() to use other camera states."
		/// </summary>
		/// <param name="current">Current Camera State</param>
		/// <param name="customCameraStateName">Name of target Camera State</param>
		public void SetCustomCameraStateTransition(CameraState current, string customCameraStateName, float speed = 8)
		{
			CameraState SelectedState = null;
			foreach (CameraState s in CustomCameraStates) { if (s.StateName == customCameraStateName) SelectedState = s; }

			if (SelectedState == null) { Debug.LogWarning("Unable to find a Camera State with this name, please check if the name is correct", gameObject); return; }

			SetCameraStateTransition(current, SelectedState, speed);
			IsTransitioningToCustomState = true;
		}
		public void DisableCustomStateTransitioningState()
		{
			IsTransitioningToCustomState = false;
		}


		/// <summary>
		/// Set the camera pivot position smoothly or not. This must be called on FixedUpdate
		/// </summary>
		/// <param name="TargetPosition">position to camera pivot </param>
		/// <param name="SmoothMove"> smooth movement       </param>
		/// <param name="Speed"> camera movement speed      </param>
		public virtual void SetPivotCameraPosition(Vector3 TargetPosition, bool SmoothMove = true, float Speed = 0)
		{
			if (transform.position != TargetPosition) { OnCameraMove(); }

			if (SmoothMove)
			{
				transform.position = Vector3.Lerp(transform.position, TargetPosition, (Speed != 0) ? Speed * Time.fixedDeltaTime : CurrentCameraState.MovementSpeed * Time.fixedDeltaTime);
			}
			else
			{
				transform.position = TargetPosition;
			}
		}

		/// <summary>
		/// Set the camera position smoothly or not. Must be called on Late Update.
		/// </summary>
		/// <param name="TargetPosition">position to camera pivot </param>
		/// <param name="SmoothMove"> smooth movement       </param>
		/// <param name="Speed"> camera movement speed      </param>
		public virtual void SetCameraPosition(Vector3 TargetPosition, bool SmoothMove = true, float Speed = 0)
		{
			if (mCamera.transform.position != TargetPosition) { OnCameraMove(); }
			if (SmoothMove)
			{
				mCamera.transform.position = Vector3.Lerp(transform.position, TargetPosition, (Speed != 0) ? Speed : CurrentCameraState.MovementSpeed * Time.fixedDeltaTime);
			}
			else
			{
				mCamera.transform.position = TargetPosition;
			}
		}



		/// <summary>
		/// Rotate the camera, uses the RotationSensibility, Min/Max Rotation parameters of the Current Camera State
		/// </summary>
		/// <param name="VerticalAxis"> x axis input   </param>
		/// <param name="HorizonalAxis"> y axis input  </param>
		/// <param name="LerpSpeed"> lerp speed value  </param>
		public virtual void RotateCamera(float VerticalAxis, float HorizonalAxis, float LerpSpeed = 30, Vector3 upward = default(Vector3), Transform AlternativeTargetToCalculate = null, bool UseTimeScale = true)
		{
			if (VerticalAxis != 0 && HorizonalAxis != 0)
			{
				OnCameraRotate();
			}

			if (InvertVertical) VerticalAxis *= -1;
			if (InvertHorizontal) HorizonalAxis *= -1;

			//Rotation X
			rotxtarget -= (UseTimeScale ? Time.timeScale : 1) * GeneralVerticalSensibility * VerticalAxis * CurrentCameraState.RotationSensibility;
			//Rotation Y
			rotytarget += (UseTimeScale ? Time.timeScale : 1) * GeneralSensibility * HorizonalAxis * CurrentCameraState.RotationSensibility;
			//Rotation X Clamp
			rotxtarget = Mathf.Clamp(rotxtarget, CurrentCameraState.MinRotation, CurrentCameraState.MaxRotation);

			//Smooth Rotations
			rotX = Mathf.Lerp(rotX, rotxtarget, LerpSpeed * Time.fixedDeltaTime * (UseTimeScale ? Time.timeScale : 1));
			rotY = Mathf.Lerp(rotY, rotytarget, LerpSpeed * Time.fixedDeltaTime * (UseTimeScale ? Time.timeScale : 1));

			//Set Rotations
			var rot = Quaternion.Euler(new Vector3(rotX, rotY, 0));

			if (AlternativeTargetToCalculate == null)
			{
				Quaternion targetRotation = TargetToFollow.root.rotation;
				targetRotation = Quaternion.AngleAxis(rotY, transform.up);
				targetRotation = Quaternion.AngleAxis(0, transform.forward);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50 * Time.deltaTime);
			}
			else
			{
				Quaternion targetRotation = AlternativeTargetToCalculate.rotation;
				targetRotation = Quaternion.AngleAxis(rotY, transform.up);
				targetRotation = Quaternion.AngleAxis(0, transform.forward);
				transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 50 * Time.deltaTime);
			}
			mCamera.transform.parent.localRotation = Quaternion.FromToRotation(transform.up, upward) * rot;
		}

		/// <summary>
		/// Set camera rotation
		/// </summary>
		/// <param name="Xtarget">target rotation of x axis   </param>
		/// <param name="Ytarget">target rotation of y axis   </param>
		/// <param name="SmoothRotate"> lerp camera rotation  </param>
		public virtual void SetCameraRotation(float Xtarget, float Ytarget, bool SmoothRotate = true)
		{
			if (SmoothRotate)
			{
				rotxtarget = Xtarget;
				rotytarget = Ytarget;
			}
			else
			{
				rotxtarget = Xtarget;
				rotytarget = Ytarget;
				rotX = Xtarget;
				rotY = Ytarget;
			}

			if (rotX != Xtarget && rotY != Ytarget)
			{
				OnCameraRotate();
			}
			//Debug.Log("Camera Rotation are defined");

		}



		/// <summary>
		/// Control the camera collision
		/// </summary>
		public virtual void SetCameraCollision(LayerMask CollisionLayer, bool Enabled = true)
		{
			if (Enabled == false) return;

			RaycastHit CameraCollisionHit;
			if (Physics.Linecast(transform.position, mCamera.transform.position, out CameraCollisionHit, CollisionLayer))
			{
				mCamera.transform.position = CameraCollisionHit.point + CameraCollisionHit.normal * 0.05f;
			}
		}

		/// <summary>
		/// Change the field of view of the camera
		/// </summary>
		/// <param name="FOV"> field of view value </param>
		public virtual void SetFieldOfView(float FOV)
		{
			if (mCamera.orthographic == true)
			{
				mCamera.orthographicSize = FOV / 10;
				mCamera.fieldOfView = FOV;
			}
			else
			{
				mCamera.fieldOfView = FOV;
			}
		}

		/// <summary>
		/// By calling this void, the camera will rotate upwards and slightly sideways
		/// </summary>
		/// <param name="Force"> Recoil force </param>
		public virtual void RecoilReaction(float Force)
		{
			if (CameraRecoilReaction == false) return;

			if (RecoilRotateCamera)
			{
				rotxtarget -= CameraRecoilSensibility * Force;
				rotX -= CameraRecoilSensibility * Force;
				rotytarget += CameraRecoilSensibility * Random.Range(-Force, Force);
			}
			else
			{
				rotX -= CameraRecoilSensibility * Force;
			}
		}

		/// <summary>
		/// Lock and hide mouse
		/// </summary>
		/// <param name="Lock"> enable and disable </param>
		public static void LockMouse(bool Lock = true, bool Hide = true)
		{
			Cursor.lockState = Lock ? CursorLockMode.Locked : CursorLockMode.None;
			Cursor.visible = !Hide;
		}

		/// <summary>
		/// Get all objects the camera is pointing at
		/// </summary>
		/// <param name="MaxCameraDistance">Raycast Distance</param>
		/// <param name="Layer">Layer Mask</param>
		/// <returns></returns>
		public RaycastHit[] GetAllObjectsOnCameraCenter(float MaxCameraDistance, LayerMask Layer)
		{
			return Physics.RaycastAll(transform.position, transform.forward, MaxCameraDistance, Layer);
		}

		/// <summary>
		/// Get object the camera is pointing at
		/// </summary>
		/// <param name="MaxCameraDistance">Raycast Distance</param>
		/// <param name="Layer">Layer Mask</param>
		/// <returns></returns>
		public GameObject GetObjectOnCameraCenter(float MaxCameraDistance, LayerMask Layer)
		{
			Physics.Raycast(mCamera.transform.position, mCamera.transform.forward, out RaycastHit hit, MaxCameraDistance, Layer);
			return hit.collider == null ? null : hit.collider.gameObject;
		}

		/// <summary>
		/// Draw gizmos
		/// </summary>
		protected virtual void OnDrawGizmos()
		{
			if (mCamera == null)
			{
				if (gameObject.GetComponentInChildren<Camera>() != null)
				{
					mCamera = gameObject.GetComponentInChildren<Camera>(); return;
				}
			}
			else
			{
				float ScaleFactor = mCamera.nearClipPlane + 0.015f;
				Vector3 ForwardCameraSpace = mCamera.transform.position + mCamera.transform.forward * ScaleFactor;

				Color TransparentWhite = new Color(1, 1, 1, 0.5f);

				Gizmos.color = Color.white;
				Gizmos.DrawLine(transform.position, ForwardCameraSpace);

				//Camera Sphere
				Gizmos.DrawWireSphere(mCamera.transform.position, 0.01f);

				//Pivot Sphere
				Gizmos.DrawWireSphere(transform.position, 0.03f);
				Gizmos.color = TransparentWhite;
				Gizmos.DrawSphere(transform.position, 0.03f);


				Gizmos.color = new Color(1, 1, 1, 0.1f);

				// >>> Center Lines <<<

				///Horizontal Line
				Gizmos.DrawLine(ForwardCameraSpace + mCamera.transform.right * ScaleFactor, ForwardCameraSpace - mCamera.transform.right * ScaleFactor);
				///Vertical Line
				Gizmos.DrawLine(ForwardCameraSpace + mCamera.transform.up * ScaleFactor, ForwardCameraSpace - mCamera.transform.up * ScaleFactor);
			}
		}
	}

}