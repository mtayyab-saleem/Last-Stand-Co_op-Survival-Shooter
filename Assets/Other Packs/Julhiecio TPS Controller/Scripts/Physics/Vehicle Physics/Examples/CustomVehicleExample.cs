using UnityEngine;
using JUTPS.JUInputSystem;

namespace JUTPS.VehicleSystem
{
    // >>> Inherit the Vehicle class to use functions and override methods
    public class CustomVehicleExample : Vehicle
    {
        [System.Serializable]
        public struct Wheel
        {
            public WheelCollider Collider;
            public Transform Mesh;
            [Range(-180, 180)] public float SteerAngle;
            [Range(0, 1)] public float ThrottleIntensity;
            [Range(0, 1)] public float BrakeIntensity;
        }

        public Wheel[] Wheels;

        protected override void Update()
        {
            base.Update();

            if (!IsOn)
                return;

            //Set default inputs
            if (UseDefaultInputs)
            {
                Steer = JUInput.GetAxis(JUInput.Axis.MoveHorizontal);
                Throttle = JUInput.GetAxis(JUInput.Axis.MoveVertical);
                Brake = JUInput.GetButton(JUInput.Buttons.JumpButton) ? 1 : 0;
            }
        }

        public override void UpdateWheelsData()
        {
            base.UpdateWheelsData();

            WheelsData = new WheelData[Wheels.Length];
            for (int i = 0; i < Wheels.Length; i++)
            {
                Wheel w = Wheels[i];
                WheelsData[i] = new WheelData(w.Collider, w.Mesh, false, w.ThrottleIntensity, w.BrakeIntensity, w.SteerAngle);
            }
        }
    }
}