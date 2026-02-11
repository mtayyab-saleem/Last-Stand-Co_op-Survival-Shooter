using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// Ju motorcycle vehicle controller.
    /// </summary>
    [AddComponentMenu("JU TPS/Vehicle System/Motorcycle Controller")]
    public class MotorcycleController : Vehicle
    {
        /// <summary>
        /// Stores vehicle <see cref="WheelCollider"/> and wheel behavior.
        /// </summary>
        [System.Serializable]
        public struct Wheel
        {
            /// <summary>
            /// The max wheel steer angle, a value between -180 to 180.
            /// </summary>
            [Range(-180, 180)] public float MaxSteerAngle;

            /// <summary>
            /// The wheel throttle intensity, a value between 0 and 1 where 0 has not acceleration and 1 has acceleration.
            /// </summary>
            [Range(0, 1)] public float ThrottleIntensity;

            /// <summary>
            /// The wheel brake intensity, a value between 0 and 1 where 0 has not brake and 1 has brake force.
            /// </summary>
            [Range(0, 1)] public float BrakeIntensity;

            /// <summary>
            /// The wheel collider.
            /// </summary>
            public WheelCollider WheelCollider;

            /// <summary>
            /// The wheel mesh transform that will follow the <see cref="WheelCollider"/>
            /// </summary>
            public Transform WheelMesh;
        }

        /// <summary>
        /// Stores properties related with motorcycle inclination on curves in high speed.
        /// </summary>
        [System.Serializable]
        public struct InclinationSettings
        {
            /// <summary>
            /// The inclination sensitive in high speeds.
            /// </summary>
            [Min(0)] public float Sensitive;

            /// <summary>
            /// The inclination speed.
            /// </summary>
            [Min(0.1f)] public float Speed;

            /// <summary>
            /// Max motorcycle inclination on curves.
            /// </summary>
            [Range(0, 60)] public float MaxAngle;

            /// <summary>
            /// The normal vehicle inclination when stoped, very useful to simulate the character foot on ground when the motorcycle is stoped.
            /// </summary>
            [Range(-45, 45)] public float StopedInclination;

            /// <summary>
            /// The vehicle aero-dynamic drag when grounded.
            /// </summary>
            [Min(0)] public float OnGroundDrag;

            /// <summary>
            /// The vehicle aero-dynamic drag when grounded.
            /// </summary>
            [Min(0)] public float OffGroundDrag;
        }

        private Transform _rotationPivotParent;
        private Transform _rotationPivotChild;

        /// <summary>
        /// The front wheel of the motorcycle.
        /// </summary>
        [Header("Wheels")]
        public Wheel FrontWheel;

        /// <summary>
        /// The back wheel of the motorcycle.
        /// </summary>
        public Wheel BackWheel;

        /// <summary>
        /// Align vehicle on ground normal when grounded.
        /// </summary>
        public VehicleOverturnCheck OverturnCheck;

        /// <summary>
        /// Stores properties related with motorcycle inclination on curves in high speed.
        /// </summary>
        public InclinationSettings Inclination;

        /// <summary>
        /// If true, the vehicle will align the up with the ground if the ground collider tag is <seealso cref="LoopTag"/>.
        /// </summary>
        [Header("Looping")]
        public bool EnableLooping;

        /// <summary>
        /// The loop collider tag, used to align the vehicle in a specific surface if <seealso cref="EnableLooping"/> is true.
        /// </summary>
        public string LoopTag;

        /// <summary>
        /// The velocity to rotate to the ground normal direction if the it's have the <seealso cref="LoopTag"/> tag.
        /// </summary>
        [Min(0.1f)] public float AlignWithLoopSpeed;

        /// <summary>
        /// Current motorcycle inclination.
        /// </summary>
        public float CurrentInclination { get; private set; }

        /// <summary>
        /// Return true if the <seealso cref="Vehicle.IsGrounded"/> is true and the ground collider surface have a tag <seealso cref="LoopTag"/>. <para/>
        /// When is looping, he vehicle will align the normal with the ground direction.
        /// </summary>
        public bool IsLooping { get; private set; }

        /// <summary>
        /// Create a <see cref="MotorcycleController"/> gameObject component instance.
        /// </summary>
        public MotorcycleController()
        {
            FrontWheel = new Wheel
            {
                MaxSteerAngle = 35,
                WheelCollider = null,
                WheelMesh = null,
                BrakeIntensity = 1,
                ThrottleIntensity = 0
            };

            BackWheel = new Wheel()
            {
                MaxSteerAngle = 0,
                WheelCollider = null,
                WheelMesh = null,
                ThrottleIntensity = 1,
                BrakeIntensity = 1
            };

            Inclination = new InclinationSettings()
            {
                Sensitive = 3,
                Speed = 1,
                MaxAngle = 45,
                StopedInclination = 20,
                OnGroundDrag = 5,
                OffGroundDrag = 1
            };

            LoopTag = "Loop";
            AlignWithLoopSpeed = 8;
        }

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();

            // Create transforms to manage motorcycle inclination.
            _rotationPivotParent = new GameObject("Motorcycle Lean Angle Pivot").transform;
            _rotationPivotChild = new GameObject("Motorcycle Lean Angle Z").transform;

            _rotationPivotChild.SetParent(_rotationPivotParent);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.hideFlags = HideFlags.HideInHierarchy;

            _rotationPivotChild.SetParent(_rotationPivotChild);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.hideFlags = HideFlags.HideInHierarchy;
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            if (!IsOn)
                return;

            // Set default inputs
            if (UseDefaultInputs)
            {
                Steer = JUInput.GetAxis(JUInput.Axis.MoveHorizontal);
                Throttle = JUInput.GetAxis(JUInput.Axis.MoveVertical);
                Brake = JUInput.GetButton(JUInput.Buttons.JumpButton) ? 1 : 0;
            }

            // Anti Overturn
            OverturnCheck.OverturnCheck(transform);
            OverturnCheck.AntiOverturn(transform);
        }

        /// <inheritdoc/>
        protected override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!IsOn)
                return;

            // Inclination
            if (!IsLooping)
                MotorcycleLeanSystem();

            if (EnableLooping)
                LoopSystem();

            CanTurnToUpInAir = !IsLooping;
        }

        protected virtual void MotorcycleLeanSystem()
        {
            // Do not execute the inclination if the parent is not correct
            if (_rotationPivotChild.parent != _rotationPivotParent)
            {
                Debug.LogError($"The parent of the {nameof(_rotationPivotChild)} variable is not the same {typeof(GameObject)} as the {nameof(_rotationPivotParent)}");
                return;
            }

            if (!FrontWheel.WheelCollider || !FrontWheel.WheelMesh || !BackWheel.WheelCollider || !BackWheel.WheelMesh)
                return;

            if (!FrontWheel.WheelCollider.GetGroundHit(out WheelHit frontHit) || !BackWheel.WheelCollider.GetGroundHit(out WheelHit rearHit))
                return;

            Vector3 groundNormal = (frontHit.normal + rearHit.normal).normalized;
            SimulateVehicleInclination(groundNormal);
        }

        protected virtual void LoopSystem()
        {
            IsLooping = false;

            if (!IsGrounded)
                return;

            if (!FrontWheel.WheelCollider.GetGroundHit(out WheelHit frontHit) || !BackWheel.WheelCollider.GetGroundHit(out WheelHit rearHit))
                return;

            IsLooping = frontHit.collider.tag.Equals(LoopTag);

            if (IsLooping)
            {
                // Align the motorcycle with the loop face.
                Vector3 loopNormal = (frontHit.normal + rearHit.normal).normalized;
                AlignVehicleToNormal(loopNormal, AlignWithLoopSpeed);
            }
        }

        private void SimulateVehicleInclination(Vector3 groundAligment)
        {
            if (!IsGrounded)
                return;

            // Inclination Calculation
            if (Mathf.Abs(ForwardSpeed) > 1) CurrentInclination = Steer * Mathf.Abs(ForwardSpeed) * Inclination.Sensitive;
            else CurrentInclination = Mathf.Lerp(CurrentInclination, Inclination.StopedInclination, Time.deltaTime);
            CurrentInclination = Mathf.Clamp(CurrentInclination, -Inclination.MaxAngle, Inclination.MaxAngle);

            float inclinationSpeed = Mathf.Clamp01(Inclination.Speed * DrivePadSmooth.RiseRateSteer * CurrentSteerVsSpeed * Time.deltaTime);

            // Vehicle rotation.
            Quaternion pivotTargetRotation = Quaternion.FromToRotation(_rotationPivotParent.up, groundAligment) * _rotationPivotParent.rotation;
            _rotationPivotChild.localEulerAngles = new Vector3(0, 0, -CurrentInclination);
            _rotationPivotParent.position = transform.position;
            _rotationPivotParent.rotation = Quaternion.Slerp(_rotationPivotParent.rotation, pivotTargetRotation, inclinationSpeed);
            _rotationPivotParent.localEulerAngles = new Vector3(_rotationPivotParent.localEulerAngles.x, transform.localEulerAngles.y, _rotationPivotParent.localEulerAngles.z);

            // Apply the inclination.
            transform.rotation = Quaternion.Lerp(transform.rotation, _rotationPivotChild.rotation, inclinationSpeed);

            //Freeze rigidbody rotation.
            if (IsGrounded)
            {
                RigidBody.angularDamping = Inclination.OnGroundDrag;
                RigidBody.constraints = RigidbodyConstraints.FreezeRotationZ;
            }
            else
            {
                RigidBody.angularDamping = Inclination.OffGroundDrag;
                RigidBody.constraints = RigidbodyConstraints.None;
            }
        }

        /// <inheritdoc/>
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            VehicleGizmo.DrawVehicleInclination(_rotationPivotParent, _rotationPivotChild);
            VehicleGizmo.DrawOverturnCheck(OverturnCheck, transform);
        }

        /// <inheritdoc/>
        public override void UpdateWheelsData()
        {
            base.UpdateWheelsData();

            WheelsData = new WheelData[2];
            WheelsData[0] = new WheelData(FrontWheel.WheelCollider, FrontWheel.WheelMesh, true, FrontWheel.ThrottleIntensity, FrontWheel.BrakeIntensity, FrontWheel.MaxSteerAngle);
            WheelsData[1] = new WheelData(BackWheel.WheelCollider, BackWheel.WheelMesh, true, BackWheel.ThrottleIntensity, BackWheel.BrakeIntensity, BackWheel.MaxSteerAngle);
        }
    }

}