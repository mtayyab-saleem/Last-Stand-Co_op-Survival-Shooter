using UnityEngine;
using JUTPS.JUInputSystem;
using System.Security.Permissions;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// Ju car controller.
    /// </summary>
    [AddComponentMenu("JU TPS/Vehicle System/Car Controller")]
    public class CarController : Vehicle
    {
        /// <summary>
        /// Stores a <see cref="WheelCollider"/> from each side of the vehicle axle and properties of wheels behavior.
        /// </summary>
        [System.Serializable]
        public struct WheelAxle
        {
            /// <summary>
            /// The acceleration influence on this axle, a value between 0 to 1.
            /// </summary>
            [Header("Drive")]
            [Range(0, 1)] public float ThrottleInfluence;

            /// <summary>
            /// The brake influence on this axle, a value between 0 to 1.
            /// </summary>
            [Range(0, 1)] public float BrakeInfluence;

            /// <summary>
            /// The max steer steer angle on this axle, a value between -180 to 180.
            /// </summary>
            [Range(-180, 180)] public float MaxSteerAngle;

            /// <summary>
            /// The left <see cref="WheelCollider"/> of the axle.
            /// </summary>
            [Header("Left")]
            public WheelCollider LeftWheelCollider;

            /// <summary>
            /// The left wheel model that will follow the <see cref="LeftWheelCollider"/> position and rotation.
            /// </summary>
            public Transform LeftWheelMesh;

            /// <summary>
            /// The right <see cref="WheelCollider"/> of the axle.
            /// </summary>
            [Header("Right")]
            public WheelCollider RightWheelCollider;

            /// <summary>
            /// The right wheel model that will follow the <see cref="RightWheelCollider"/> position and rotation.
            /// </summary>
            public Transform RightWheelMesh;
        }

        [SerializeField] private WheelAxle[] _wheelAxles;

        /// <summary>
        /// Align vehicle on ground normal when grounded.
        /// </summary>
        [Header("Anti Overturn")]
        public VehicleOverturnCheck OverturnCheck;

        /// <summary>
        /// Gets or set the vehicle wheels axles, don't forgot call <see cref="UpdateWheelsData"/> after change the wheels.
        /// </summary>
        public WheelAxle[] WheelAxles
        {
            get => _wheelAxles;
            set
            {
                _wheelAxles = value;
                UpdateWheelsData();
            }
        }

        /// <summary>
        /// Create a ju car vehicle controller component instance.
        /// </summary>
        public CarController() : base()
        {
            _wheelAxles = new WheelAxle[2]
            {
                new WheelAxle
                {
                    ThrottleInfluence = 1f,
                    BrakeInfluence = 1f,
                    MaxSteerAngle = 35,
                    LeftWheelCollider = null,
                    RightWheelCollider = null,
                    LeftWheelMesh = null,
                    RightWheelMesh = null
                },
                new WheelAxle
                {
                    ThrottleInfluence = 1f,
                    BrakeInfluence = 1f,
                    MaxSteerAngle = 0,
                    LeftWheelCollider = null,
                    RightWheelCollider = null,
                    LeftWheelMesh = null,
                    RightWheelMesh = null
                }
            };
        }

#if UNITY_EDITOR

        /// <inheritdoc/>
        protected override void OnValidate()
        {
            base.OnValidate();

            // Update the wheels when set in runtime on the editor.
            UpdateWheelsData();
        }
#endif

        /// <inheritdoc/>
        protected override void Start()
        {
            base.Start();
        }

        /// <inheritdoc/>
        protected override void Update()
        {
            base.Update();

            if (!IsOn)
                return;

            if (UseDefaultInputs)
                UpdatePlayerControls();

            OverturnCheck.OverturnCheck(transform);
            OverturnCheck.AntiOverturn(transform);
        }

        /// <inheritdoc/>
        protected override void OnDrawGizmos()
        {
            base.OnDrawGizmos();
            VehicleGizmo.DrawOverturnCheck(OverturnCheck, transform);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(Engine.CenterOfMass, 0.2f);
        }

        private void UpdatePlayerControls()
        {
            Steer = JUInput.GetAxis(JUInput.Axis.MoveHorizontal);
            Throttle = JUInput.GetAxis(JUInput.Axis.MoveVertical);
            Brake = JUInput.GetButton(JUInput.Buttons.JumpButton) ? 1 : 0;
        }

        /// <inheritdoc/>
        public override void UpdateWheelsData()
        {
            base.UpdateWheelsData();

            WheelsData = new WheelData[WheelAxles.Length * 2];

            for (int i = 0; i < WheelAxles.Length; i++)
            {
                int leftWhel = i * 2;
                int rightWheel = (i * 2) + 1;

                WheelAxle axle = WheelAxles[i];
                WheelsData[leftWhel] = new WheelData(axle.LeftWheelCollider, axle.LeftWheelMesh, false, axle.ThrottleInfluence, axle.BrakeInfluence, axle.MaxSteerAngle);
                WheelsData[rightWheel] = new WheelData(axle.RightWheelCollider, axle.RightWheelMesh, false, axle.ThrottleInfluence, axle.BrakeInfluence, axle.MaxSteerAngle);
            }
        }
    }
}
