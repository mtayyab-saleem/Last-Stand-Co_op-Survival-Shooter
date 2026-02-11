using UnityEngine;

namespace JUTPS.VehicleSystem
{
	/// <summary>
	/// Base for Ju vehicle controllers.
	/// </summary>
	public class Vehicle : MonoBehaviour
	{
		/// <summary>
		/// Stores backend wheels data.
		/// </summary>
		public class WheelData
		{
			private float _wheelRotationX;

			/// <summary>
			/// The wheel collider to apply vehicle suspension physics.
			/// </summary>
			public WheelCollider WheelCollider;

			/// <summary>
			/// The wheel model that will follow the <see cref="WheelCollider"/> position and rotation.
			/// </summary>
			public Transform WheelMesh;

			/// <summary>
			/// Only rotate the <see cref="WheelMesh"/> on X axes? Very usefull for motorcycles.
			/// </summary>
			public bool JustRotateWheelXAxis;

			/// <summary>
			/// The throttle influence, a value between 0 and 1 where 0 is no throttle and 1 is full throttle force.
			/// </summary>
			public float ThrottleInfluence;

			/// <summary>
			/// The max steer angle, a value betweeen -180 and 180.
			/// </summary>
			public float MaxSteerAngle;

			/// <summary>
			/// The brake influence, a value between 0 and 1 where 0 is no brake and 1 is full brake force when the player try brake the vehicle.
			/// </summary>
			public float BrakeIncluence;

			/// <summary>
			/// Returns the <see cref="WheelCollider"/> pose position.
			/// </summary>
			public Vector3 WheelPosition { get; private set; }

			/// <summary>
			/// Returns the <see cref="WheelCollider"/> pose rotation.
			/// </summary>
			public Quaternion WheelRotation { get; private set; }

			/// <summary>
			/// returns the wheel acceleration direction.
			/// </summary>
			public Vector3 WheelForward { get; private set; }

			/// <summary>
			/// Return true if the <see cref="WheelCollider"/> is grounded.
			/// </summary>
			public bool IsGrounded { get; private set; }

			/// <summary>
			/// Get's the ground hit info if <see cref="IsGrounded"/> is true. 
			/// </summary>
			public WheelHit WheelHit { get; private set; }

			/// <summary>
			/// Create an instance of <see cref="WheelData"/>.
			/// </summary>
			/// <param name="collider">The wheel collider of this wheel.</param>
			/// <param name="mesh">The wheel model that will follow the collider position/rotation.</param>
			/// <param name="justRotateWheelXAxis">The whell only will rotate the X axis? Helpful for motorcycle wheels.</param>
			/// <param name="throttleIntensity">The throttle intensity, a value between 0 and 1 where 0 has not wheel torque even 
			/// with engine has acceleration and 1 has full acceleration when the engine is accelerating.</param>
			/// <param name="brakeIntensity">The brake intensity, a value between 0 and 1 where 0 has not brake on this wheel 
			/// even if the vehicle is braking and 1 has full brake force when the vehicle is braking.</param>
			/// <param name="maxSteer">The max wheel steer angle.</param>
			public WheelData(WheelCollider collider, Transform mesh, bool justRotateWheelXAxis, float throttleIntensity, float brakeIntensity, float maxSteer)
			{
				WheelCollider = collider;
				WheelMesh = mesh;

				JustRotateWheelXAxis = justRotateWheelXAxis;
				ThrottleInfluence = throttleIntensity;
				MaxSteerAngle = maxSteer;
				BrakeIncluence = brakeIntensity;
			}

			/// <summary>
			/// Call it on vehicle update to sync the <see cref="WheelCollider"/> data.
			/// </summary>
			internal void UpdateData()
			{
				if (!WheelCollider)
					return;

				IsGrounded = WheelCollider.GetGroundHit(out WheelHit hit);
				WheelHit = hit;
				WheelForward = (WheelCollider.transform.rotation * Quaternion.Euler(0, WheelCollider.steerAngle, 0)) * Vector3.forward;
			}

			/// <summary>
			/// Apply control to the <see cref="WheelCollider"/>.
			/// </summary>
			/// <param name="throttle">The throttle force.</param>
			/// <param name="steer">The steer value, a value between -1 to left, 0 to no steer and 1 to right.</param>
			/// <param name="brake">The brake force.</param>
			internal void ApplyControl(float throttle, float steer, float brake)
			{
				if (!WheelCollider)
					return;

				WheelCollider.motorTorque = throttle * ThrottleInfluence;
				WheelCollider.steerAngle = steer * MaxSteerAngle;
				WheelCollider.brakeTorque = brake * BrakeIncluence;
			}

			/// <summary>
			/// Call it on the vehicle update to sync the <see cref="WheelMesh"/> position and rotation with the <see cref="WheelCollider"/>.
			/// </summary>
			internal void SyncWheelTransform()
			{
				if (!WheelCollider)
					return;

				WheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
				_wheelRotationX += WheelCollider.rpm / 60 * 360 * Time.deltaTime;

				if (_wheelRotationX > 360) _wheelRotationX -= 360;
				if (_wheelRotationX < 0) _wheelRotationX += 360;

				if (JustRotateWheelXAxis && WheelMesh && WheelMesh.parent)
				{
					// Set directly the rot to Quaternion.Euler(angle, 0, 0) not works very well
					// Because the new rot only rotates from 0 to 90 instead of 0 to 360, idk why this happen...
					// So, apply the rotation calculating manually the axis angle works better for this case.
					rot = Quaternion.AngleAxis(_wheelRotationX, Vector3.right);
					rot = WheelMesh.parent.rotation * rot;
				}

				WheelPosition = pos;
				WheelRotation = rot;

				if (WheelMesh)
					WheelMesh.SetPositionAndRotation(WheelPosition, WheelRotation);
			}
		}

		/// <summary>
		/// Stores vehicle engine settings.
		/// </summary>
		[System.Serializable]
		public class EngineSettings
		{
			/// <summary>
			/// The max vehicle forward speed.
			/// </summary>
			[Min(1)] public float MaxForwardSpeed;

			/// <summary>
			/// The max vehicle backward speed.
			/// </summary>
			[Min(0.5f)] public float MaxRearSpeed;

			/// <summary>
			/// The engine acceleration force.
			/// </summary>
			[Min(0)] public float TorqueForce;

			/// <summary>
			/// The brake force.
			/// </summary>
			[Min(0)] public float BrakeForce;

			/// <summary>
			/// The force to brake the vehicle automatically when the vehicle throttle is 0.
			/// </summary>
			[Min(0)] public float AutoBrakeForce;

			/// <summary>
			/// The acceleration curve based on current vehicle forward speed.
			/// Basically, each <see cref="Keyframe"/> of the curve represents and
			/// speed where <see cref="Keyframe.time"/> is the normalized velocity (speed/maxSpeed), a value between 0 to 1.
			/// The <see cref="Keyframe.value"/> is the engine force multiplier (0 to 1).
			/// The first <see cref="Keyframe"/> would be value = 1 and time = 0 to have the max acceleration force and the last keyframe
			/// would be time = 1 and value = 0 to the current speed not exceed the <see cref="MaxForwardSpeed"/>.
			/// </summary>
			public AnimationCurve AccelerationCurve;

			/// <summary>
			/// The vehicle center of mass relative to the vehicle transform. <para/>
			/// Call the <see cref="Vehicle.UpdateCenterOfMass"/> after change it.
			/// </summary>
			public Vector3 CenterOfMass;

			/// <summary>
			/// Create an instance of the vehicle engine settings.
			/// </summary>
			public EngineSettings()
			{
				MaxForwardSpeed = 30;
				MaxRearSpeed = 20;
				TorqueForce = 600;
				BrakeForce = 1200;
				AutoBrakeForce = 100;
				AccelerationCurve = new AnimationCurve(new Keyframe(0, 1), new Keyframe(1, 0));
			}
		}

		/// <summary>
		/// Stores drive pad smooth settings.
		/// </summary>
		[System.Serializable]
		public class PadSmooth
		{
			/// <summary>
			/// The throttle force.
			/// </summary>
			public float RiseRateThrottle;

			/// <summary>
			/// The throttle fall rate speed.
			/// </summary>
			public float FallRateThrotte;

			/// <summary>
			/// The steer force.
			/// </summary>
			public float RiseRateSteer;

			/// <summary>
			/// The steer fall rate speed.
			/// </summary>
			public float FallRateSteer;

			/// <summary>
			/// The brake force.
			/// </summary>
			public float RiseRateBrake;

			/// <summary>
			/// The brake fall rate speed.
			/// </summary>
			public float RiseFallBrake;

			/// <summary>
			/// Create an instance of the drive pad smooth.
			/// </summary>
			public PadSmooth()
			{
				RiseRateThrottle = 2f;
				FallRateThrotte = 2f;
				RiseRateSteer = 4f;
				FallRateSteer = 4f;
				RiseRateBrake = 2f;
				RiseFallBrake = 2f;
			}
		}

		private float _steer;
		private float _throttle;
		private float _brake;

		private const float INPUTS_DEATH_ZONE = 0.1f;

		/// <summary>
		/// The vehicle wheel.
		/// </summary>
		protected WheelData[] WheelsData;

		/// <summary>
		/// When true, the vehicle will turn when <seealso cref="IsGrounded"/> is false to align the 
		/// <see cref="Transform.up"/> with <see cref="Vector3.up"/> if <see cref="RotateToUpInAirSpeed"/> is greater than 0.
		/// </summary>
		protected bool CanTurnToUpInAir;

		/// <summary>
		/// Use the standard <see cref="JUInputSystem.JUInputManager"/> to provide player controls?
		/// </summary>
		[Header("Settings")]
		public bool UseDefaultInputs;

		/// <summary>
		/// The engine settings.
		/// </summary>
		public EngineSettings Engine;

		/// <summary>
		/// The drive smooth control settings.
		/// </summary>
		public PadSmooth DrivePadSmooth;

		/// <summary>
		/// Used by the steer system to make curves more smoothy on high speeds. <para />
		/// This graph represents the steer intensity by speed where curve <see cref="Keyframe.time"/> = speed and 
		/// curve <see cref="Keyframe.value"/> = steer multiplier.  <para />
		/// Always sets a curve keyframes with <see cref="Keyframe.value"/> between (1, 0) where 1 is full steer and 0 is no steer.
		/// The <see cref="Keyframe.time"/> would always absolute values according with vehicle speed.
		/// </summary>
		public AnimationCurve SteerVsSpeed;

		/// <summary>
		/// Vehicle rotation speed to align the up direction with <see cref="Vector3.up"/> when <see cref="IsGrounded"/> is false.
		/// </summary>
		[Header("Arcade")]
		[Min(0)] public float RotateToUpInAirSpeed;

		/// <summary>
		/// Return true if the vehicle engine is on.
		/// The vehicle can't be controled if the engine is off.
		/// </summary>
		[Header("Vehicle State")]
		public bool IsOn;

		/// <summary>
		/// When true the vehicle can be controlled by the player or IAs.
		/// </summary>
		public bool ControlsEnabled;

		/// <summary>
		/// Returns the steer value similar to <see cref="Steer"/>input but with smooth provided from <see cref="DrivePadSmooth"/>.
		/// </summary>
		public float FinalSteer { get; private set; }

		/// <summary>
		/// Returns the throttle value similar to <see cref="Throttle"/>input but with smooth provided from <see cref="DrivePadSmooth"/>.
		/// </summary>
		public float FinalThrottle { get; private set; }

		/// <summary>
		/// Returns the brake value similar to <see cref="Brake"/> input but with smooth provided from <see cref="DrivePadSmooth"/>.
		/// </summary>
		public float FinalBrake { get; private set; }

		/// <summary>
		/// Returns true if the vehicle is grounded.
		/// </summary>
		public bool IsGrounded { get; private set; }

		/// <summary>
		/// Return ground hit info if <see cref="IsGrounded"/> is true.
		/// </summary>
		public WheelHit WheelHit { get; private set; }

		/// <summary>
		/// Returns the relative vehicle velocity.
		/// </summary>
		public Vector3 LocalVelocity { get; private set; }

		/// <summary>
		/// Returns the vehicle velocity relative to the world.
		/// </summary>
		public Vector3 Velocity { get; private set; }

		/// <summary>
		/// Returns the vehicle up based on the median direction of all wheels positions.
		/// </summary>
		public Vector3 WheelsUpDirection { get; private set; }

		/// <summary>
		/// Returns the <see cref="SteerVsSpeed"/> value relative to the current velocity. <para/>
		/// Used to improve the control on high speeds, decreasing the steer to make curves more smoothly.
		/// </summary>
		public float CurrentSteerVsSpeed { get; private set; }

		/// <summary>
		/// The rigidbody body of this vehicle.
		/// </summary>
		public Rigidbody RigidBody { get; private set; }

		public Bounds CollisionBounds { get; private set; }

		/// <summary>
		/// Returns the vehicle forward speed.
		/// </summary>
		public float ForwardSpeed
		{
			get => LocalVelocity.z;
		}

		/// <summary>
		/// The throttle input to control the vehicle engine acceleration. <para/>
		/// A value between -1 to 1 where -1 is to move to backward, 0 to be stoped and 1 to move to forward.
		/// </summary>
		public float Throttle
		{
			get => _throttle;
			set => _throttle = Mathf.Clamp(value, -1, 1);
		}

		/// <summary>
		/// The steer input to control the vehicle move direction.
		/// A value between -1 to 1 where -1 is to turn to left, 0 to not turn and 1 is to move to right.
		/// </summary>
		public float Steer
		{
			get => _steer;
			set => _steer = Mathf.Clamp(value, -1, 1);
		}

		/// <summary>
		/// A value between 0 and 1 where 0 is without brake and 1 is full brake.
		/// </summary>
		public float Brake
		{
			get => _brake;
			set => _brake = Mathf.Clamp(value, -1, 1);
		}

		/// <summary>
		/// Return the number of wheels from the vehicle.
		/// </summary>
		public int WheelsCount => WheelsData.Length;

		/// <summary>
		/// Create a JUVehicle controller component instance.
		/// </summary>
		protected Vehicle()
		{
			WheelsData = new WheelData[0];

			Engine = new EngineSettings();
			DrivePadSmooth = new PadSmooth();

			UseDefaultInputs = true;
			SteerVsSpeed = new AnimationCurve(new Keyframe(10, 1f), new Keyframe(35, 0.3f));

			RotateToUpInAirSpeed = 1;
			ControlsEnabled = true;
		}

#if UNITY_EDITOR

		/// <summary>
		/// Used only by the editor, called after change some property on the editor to validade variables.
		/// </summary>
		protected virtual void OnValidate()
		{
			// Validating the vehicle accelerating curve.

			int keyframeCount = Engine.AccelerationCurve.length;
			while (keyframeCount < 2)
			{
				keyframeCount++;
				Engine.AccelerationCurve.AddKey(new Keyframe());
			}

			Keyframe[] engineAccelerationCurve = Engine.AccelerationCurve.keys;
			engineAccelerationCurve[0] = new Keyframe(0, 1);
			engineAccelerationCurve[engineAccelerationCurve.Length - 1] = new Keyframe(1, 0);

			for (int i = 1; i < engineAccelerationCurve.Length - 1; i++)
			{
				Keyframe keyframe = engineAccelerationCurve[i];
				keyframe.time = Mathf.Clamp(keyframe.time, 0, 1);
				keyframe.value = Mathf.Clamp(keyframe.value, 0, 1);
				engineAccelerationCurve[i] = keyframe;
			}

			Engine.AccelerationCurve.keys = engineAccelerationCurve;
		}
#endif

		/// <summary>
		/// Called when the scene load.
		/// </summary>
		protected virtual void Awake()
		{
			CanTurnToUpInAir = true;
			RigidBody = GetComponent<Rigidbody>();
			UpdateWheelsData();
		}

		/// <summary>
		/// Called when the game starts, after <see cref="Awake"/>.
		/// </summary>
		protected virtual void Start()
		{
			UpdateCenterOfMass();
		}

		/// <summary>
		/// Called each physx update before <see cref="Update"/>.
		/// </summary>
		protected virtual void FixedUpdate()
		{
			if (!IsGrounded && CanTurnToUpInAir)
				AlignVehicleToNormal(Vector3.up, RotateToUpInAirSpeed);
		}

		/// <summary>
		/// Called on each frame updating vehicle logic and controls.
		/// </summary>
		protected virtual void Update()
		{
			if (!RigidBody)
				return;

			// Update properties
			Velocity = RigidBody.linearVelocity;
			LocalVelocity = transform.InverseTransformDirection(Velocity);
			CurrentSteerVsSpeed = Mathf.Clamp01(SteerVsSpeed.Evaluate(Mathf.Abs(ForwardSpeed)));

			ProcessInputs();
			UpdateWheels();

			IsGrounded = false;
			WheelHit = default;

			for (int i = 0; i < WheelsData.Length; i++)
			{
				if (WheelsData[i].IsGrounded)
				{
					WheelHit = WheelsData[i].WheelHit;
					IsGrounded = true;
					break;
				}
			}

			// Calculate wheels up direction based on wheels positions.
			{
				WheelsUpDirection = Vector3.up;
				int wheelsCount = WheelsData.Length;
				for (int i = 0; i < wheelsCount; i += 2)
				{
					if (i > wheelsCount - 1)
						break;

					WheelData wheel = WheelsData[i];
					WheelData nextWheel = WheelsData[i + 1];

					if (!wheel.WheelCollider || !nextWheel.WheelCollider)
						continue;

					// Get's the up direction of the wheels.
					WheelsUpDirection += Quaternion.LookRotation(nextWheel.WheelPosition - wheel.WheelPosition) * Vector3.up;
				}

				WheelsUpDirection /= WheelsUpDirection.magnitude;
			}
		}

		private void UpdateWheels()
		{
			float throttle = 0f;
			float steer = 0f;
			float brake = 0f;

			if (!ControlsEnabled)
			{
				UpdateWheels(throttle, steer, brake);
				return;
			}

			float engineTorque = Engine.TorqueForce;
			float brakeForce = Engine.BrakeForce;
			brake = Mathf.Abs(_throttle) < INPUTS_DEATH_ZONE ? Engine.AutoBrakeForce : brakeForce * FinalBrake;

			if (!IsOn)
			{
				UpdateWheels(0, 0, brake);
				return;
			}

			// Speed limit.
			{
				float maxForwardSpeed = Engine.MaxForwardSpeed;
				float maxRearSpeed = Engine.MaxRearSpeed;

				float currentForwardSpeed = Mathf.Clamp(ForwardSpeed, 0, maxForwardSpeed);
				float currentRearSpeed = Mathf.Abs(Mathf.Clamp(ForwardSpeed, -maxRearSpeed, 0));

				float backwardNormalizedSpeed = currentRearSpeed / maxRearSpeed;
				float forwardNormalizedSpeed = currentForwardSpeed / maxForwardSpeed;

				float accelerationMultiplier;
				if (forwardNormalizedSpeed > 0) accelerationMultiplier = Engine.AccelerationCurve.Evaluate(forwardNormalizedSpeed);
				else accelerationMultiplier = Engine.AccelerationCurve.Evaluate(backwardNormalizedSpeed);

				engineTorque *= accelerationMultiplier;
			}

			throttle = engineTorque * FinalThrottle;
			steer = FinalSteer * CurrentSteerVsSpeed;
			UpdateWheels(throttle, steer, brake);
		}

		private void UpdateWheels(float throttle, float steer, float brake)
		{
			for (int i = 0; i < WheelsData.Length; i++)
			{
				WheelsData[i].UpdateData();
				WheelsData[i].ApplyControl(throttle, steer, brake);
				WheelsData[i].SyncWheelTransform();
			}
		}

		private void ProcessInputs()
		{
			float brake = _brake;
			bool forceBrake = false;

			forceBrake |= _throttle > INPUTS_DEATH_ZONE && ForwardSpeed < -0.1f;
			forceBrake |= _throttle < -INPUTS_DEATH_ZONE && ForwardSpeed > 0.1f;

			if (forceBrake)
				brake = 1f;

			FinalSteer = ProcessInput(_steer, FinalSteer, DrivePadSmooth.RiseRateSteer, DrivePadSmooth.FallRateSteer, INPUTS_DEATH_ZONE);
			FinalThrottle = ProcessInput(_throttle, FinalThrottle, DrivePadSmooth.RiseRateThrottle, DrivePadSmooth.FallRateThrotte, INPUTS_DEATH_ZONE);
			FinalBrake = ProcessInput(brake, FinalBrake, DrivePadSmooth.RiseRateBrake, DrivePadSmooth.RiseFallBrake, INPUTS_DEATH_ZONE);
		}

		private float ProcessInput(float targetInput, float currentInput, float riseRate, float fallRate, float deathZone = 0.1f)
		{
			targetInput = Mathf.Clamp(targetInput, -1, 1);

			if (Mathf.Abs(targetInput) <= deathZone)
				targetInput = 0f;

			if (targetInput == 0 && Mathf.Abs(currentInput) <= deathZone)
				return 0;

			if (targetInput != 0)
			{
				float acceleration = riseRate * Time.deltaTime;
				if (currentInput < targetInput) currentInput = Mathf.Min(currentInput + acceleration, targetInput);
				else currentInput = Mathf.Max(currentInput - acceleration, targetInput);
			}
			else
			{
				currentInput -= fallRate * Time.deltaTime * (currentInput > 0 ? 1 : -1);
			}

			currentInput = Mathf.Clamp(currentInput, -1, 1);

			return currentInput;
		}

		/// <summary>
		/// Add an additional force on the vehicle to increase the acceleration.
		/// </summary>
		/// <param name="AccelerationForce">The force magnetude.</param>
		protected void AddForwardAcceleration(float AccelerationForce)
		{
			if (!IsOn || !RigidBody)
				return;

			RigidBody.AddRelativeForce(Vector3.forward * AccelerationForce, ForceMode.Acceleration);
		}

		/// <summary>
		/// Apply anti roll force on vehicle axle suspension. Used to increase the stability on curves avoiding vehicle turn around his Z axis.
		/// </summary>
		/// <param name="antiRollForce">The anti roll force</param>
		/// <param name="leftWheel">The left wheel.</param>
		/// <param name="rightWheel">The right wheel.</param>
		protected void SimulateAntiRollBar(float antiRollForce, WheelCollider leftWheel, WheelCollider rightWheel)
		{
			if (!leftWheel || !rightWheel)
				return;

			antiRollForce *= 100000 * Time.fixedDeltaTime;
			bool leftWheelIsGrounded = leftWheel.GetGroundHit(out WheelHit leftWheelHit);
			bool rightWheelIsGrounded = rightWheel.GetGroundHit(out WheelHit rightWheelHit);

			if (!leftWheelIsGrounded && !rightWheelIsGrounded)
				return;

			float leftAntiRollBarWeight = 1.0f;
			float rightAntiRollBarWeight = 1.0f;
			Transform leftTransform = leftWheel.transform;
			Transform rightTransform = rightWheel.transform;

			if (leftWheelIsGrounded)
				leftAntiRollBarWeight = (-leftTransform.InverseTransformPoint(leftWheelHit.point).y - leftWheel.radius) / leftWheel.suspensionDistance;

			if (rightWheelIsGrounded)
				rightAntiRollBarWeight = (-rightTransform.InverseTransformPoint(rightWheelHit.point).y - rightWheel.radius) / rightWheel.suspensionDistance;


			// Get Final Anti-Roll Force 
			float finalForce = (leftAntiRollBarWeight - rightAntiRollBarWeight) * antiRollForce;

			// Apply Anti-Roll bar Simulation
			if (leftWheelIsGrounded) RigidBody.AddForceAtPosition(leftTransform.up * -finalForce, leftTransform.position);
			if (rightWheelIsGrounded) RigidBody.AddForceAtPosition(rightTransform.up * finalForce, rightTransform.position);
		}

		/// <summary>
		/// Call it after change/set the vehicle wheels to update the vehicle physics control.
		/// </summary>
		public virtual void UpdateWheelsData()
		{
		}

		/// <summary>
		/// Returns an wheel data from the vehicle.
		/// </summary>
		/// <param name="index"></param>
		/// <returns></returns>
		public WheelData GetWheel(int index)
		{
			if (index < 0 || index > WheelsData.Length - 1)
			{
				Debug.LogError($"Invalid wheel index: {index}");
				return null;
			}

			return WheelsData[index];
		}

		/// <summary>
		/// Try align the vehicle on ground normal, works only if the <see cref="IsGrounded"/> is true.
		/// </summary>
		/// <param name="alignmentSpeed">The speed to align the vehicle.</param>
		protected void SimulateGroundAlignment(float alignmentSpeed)
		{
			if (!IsGrounded)
				return;

			AlignVehicleToNormal(WheelsUpDirection, alignmentSpeed);
		}

		/// <summary>
		/// Align the vehicle to a normal direction.
		/// </summary>
		/// <param name="Normal">The normal to align</param>
		/// <param name="alignmentSpeed"></param>
		protected void AlignVehicleToNormal(Vector3 Normal, float alignmentSpeed)
		{
			float speed = Mathf.Clamp01(alignmentSpeed * Time.deltaTime);
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.FromToRotation(transform.up, Normal) * transform.rotation, speed);
		}

		/// <summary>
		/// Call it after change <seealso cref="EngineSettings.CenterOfMass"/>. <para/>
		/// Update the vehicle <see cref="Rigidbody.centerOfMass"/>.
		/// </summary>
		public void UpdateCenterOfMass()
		{
			if (!RigidBody)
				return;

			RigidBody.centerOfMass = Engine.CenterOfMass;
		}

		/// <summary>
		/// Draw debug gizmos.
		/// </summary>
		protected virtual void OnDrawGizmos()
		{
			Gizmos.color = Color.yellow;
			Gizmos.DrawRay(transform.position, WheelsUpDirection);
		}

		[System.Serializable]
		public class VehicleOverturnCheck
		{
			[Header("Overturn Check")]
			public Vector3 CheckboxPosition = new Vector3(0, 1.5f, 0);
			public Vector3 CheckboxScale = new Vector3(0.4f, 0.3f, 1.5f);
			public LayerMask CheckboxLayerMask;

			[Header("Anti-Overturn")]
			public bool EnableAntiOverturn;
			public float AntiOverturnSpeed = 1f;

			[Header("State")]
			public bool IsOverturned;

			private RaycastHit GroundHit;

			public void OverturnCheck(Transform vehicle)
			{
				Vector3 origin = vehicle.position + (vehicle.right * CheckboxPosition.x) + (vehicle.up * CheckboxPosition.y) + (vehicle.forward * CheckboxPosition.z);
				var checkbox = Physics.OverlapBox(origin, CheckboxScale, vehicle.rotation, CheckboxLayerMask);
				IsOverturned = (checkbox.Length != 0);
			}
			public void AntiOverturn(Transform vehicle)
			{
				if (!IsOverturned) return;
				vehicle.rotation = Quaternion.Lerp(vehicle.rotation, Quaternion.FromToRotation(vehicle.up, GroundHit.normal) * vehicle.rotation, AntiOverturnSpeed * Time.deltaTime);
			}
		}

		[System.Serializable]
		public class VehicleOverlapBoxCheck
		{
			[Header("Overturn Check")]
			public Vector3 CheckboxPosition = new Vector3(0, 0, 0);
			public Vector3 CheckboxScale = new Vector3(0.4f, 0.4f, 0.4f);
			public LayerMask CheckboxLayerMask;

			[Header("State")]
			public bool Collided;

			private RaycastHit GroundHit;

			public void Check(Transform vehicle)
			{
				Vector3 origin = vehicle.position + vehicle.right * CheckboxPosition.x + vehicle.up * CheckboxPosition.y + vehicle.forward * CheckboxPosition.z;
				var checkbox = Physics.OverlapBox(origin, CheckboxScale, vehicle.rotation, CheckboxLayerMask);
				Collided = (checkbox.Length != 0);
				if (Collided) Debug.Log("collided");
			}
		}

		[System.Serializable]
		public class VehicleRaycastCheck
		{
			[Header("Raycast Check")]
			public Vector3 OriginPosition = new Vector3(0, 0, 0);
			public float RayMaxDistance = 1;
			public LayerMask RayLayerMask;

			[Header("State")]
			public bool IsCollided;

			public RaycastHit raycastHit;

			public void Check(Transform vehicle, Vector3 direction)
			{
				Vector3 origin = vehicle.position + vehicle.right * OriginPosition.x + vehicle.up * OriginPosition.y + vehicle.forward * OriginPosition.z;
				IsCollided = Physics.Raycast(origin, direction, out raycastHit, RayMaxDistance, RayLayerMask);
			}
		}



		public static class VehicleGizmo
		{
			public static void DrawVector3Position(Vector3 position, Transform Vehicle, string Label = "", Color color = default(Color))
			{
				if (color != Color.clear)
				{
					Gizmos.color = color;
				}

				Vector3 wordlPosition = Vehicle.position + Vehicle.right * position.x + Vehicle.up * position.y + Vehicle.forward * position.z;
				if (Label != "")
				{
#if UNITY_EDITOR
					UnityEditor.Handles.Label(wordlPosition + Vector3.up * 0.1f, Label);
#endif
				}
				Gizmos.DrawSphere(wordlPosition, 0.03f);
			}
			public static void DrawVehicleInclination(Transform RotationParent, Transform RotationChild)
			{
				if (RotationChild == null || RotationParent == null) return;

				//Points
				Vector3 pos1 = RotationParent.position + RotationParent.up;
				Vector3 pos2 = RotationChild.position + RotationChild.up;

				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(pos1, 0.01f);
				Gizmos.color = Color.white;
				Gizmos.DrawWireSphere(pos2, 0.01f);

				//Base
				Gizmos.color = Color.grey;
				Gizmos.DrawRay(RotationParent.position, RotationParent.up);

				//Real rotation
				Gizmos.color = Color.green;
				Gizmos.DrawRay(RotationChild.position, RotationChild.up);
#if UNITY_EDITOR

				//Infos
				UnityEditor.Handles.Label(RotationParent.position + RotationChild.up * 1.2f, Vector3.Angle(RotationChild.up, RotationParent.up).ToString("00.0"));
#endif

				//Base Lines
				Gizmos.color = Color.green;
				Gizmos.DrawLine(RotationParent.position - RotationParent.right, RotationParent.position + RotationParent.right);
#if UNITY_EDITOR

				//Disc
				UnityEditor.Handles.color = Color.green;
				UnityEditor.Handles.DrawWireArc(RotationParent.position, RotationParent.forward, RotationParent.right, 180, 1);
#endif
			}
			public static void DrawRaycastHit(Vehicle.VehicleRaycastCheck rayCheck, Transform vehicle, Vector3 direction)
			{
				Gizmos.color = rayCheck.IsCollided ? Color.green : Color.red;

				Vector3 origin = vehicle.position + vehicle.right * rayCheck.OriginPosition.x + vehicle.up * rayCheck.OriginPosition.y + vehicle.forward * rayCheck.OriginPosition.z;

				Gizmos.DrawLine(origin, rayCheck.IsCollided ? rayCheck.raycastHit.point : origin + direction * rayCheck.RayMaxDistance);
			}
			public static void DrawOverturnCheck(Vehicle.VehicleOverturnCheck OverturnCheck, Transform Vehicle)
			{
				if (OverturnCheck.EnableAntiOverturn)
				{
					Gizmos.matrix = Matrix4x4.TRS(Vehicle.position, Vehicle.rotation, Vehicle.localScale);
					Gizmos.color = OverturnCheck.IsOverturned ? Color.green : Color.red;
					Gizmos.DrawWireCube(Vector3.zero + Vector3.up * OverturnCheck.CheckboxPosition.y + Vector3.right * OverturnCheck.CheckboxPosition.x + Vector3.forward * OverturnCheck.CheckboxPosition.z, OverturnCheck.CheckboxScale);
				}
			}
			public static void DrawOverlapBoxCheck(Vehicle.VehicleOverlapBoxCheck BoxCheck, Transform Vehicle)
			{
				Gizmos.matrix = Matrix4x4.TRS(Vehicle.position, Vehicle.rotation, Vehicle.localScale);
				Gizmos.color = BoxCheck.Collided ? Color.green : Color.red;
				Gizmos.DrawWireCube(Vector3.zero + Vector3.up * BoxCheck.CheckboxPosition.y + Vector3.right * BoxCheck.CheckboxPosition.x + Vector3.forward * BoxCheck.CheckboxPosition.z, BoxCheck.CheckboxScale);
			}
		}
	}
}
