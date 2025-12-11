using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.VehicleSystem
{
    public class VehicleAirRotation : MonoBehaviour
    {
        private Vehicle vehicle;
        public bool UseDefaultInputs = true;
        public float Force = 200;
        public bool X = true;
        public bool Y = true;
        public bool Z = true;
        void Start()
        {
            vehicle = GetComponent<Vehicle>();
        }
        void Update()
        {
            if (!UseDefaultInputs) return;

            float VerticalForce = JUInputSystem.JUInput.GetAxis(JUInputSystem.JUInput.Axis.MoveVertical);
            float HorizontalForce = JUInputSystem.JUInput.GetAxis(JUInputSystem.JUInput.Axis.MoveHorizontal);

            RotateVehicle(new Vector3(VerticalForce, HorizontalForce, 0), vehicle.IsGrounded);
        }
        public void RotateVehicle(Vector3 Torque, bool IsGrounded)
        {
            if (IsGrounded == true || vehicle.IsOn == false) return;

            Vector3 modifiedToque = Torque;
            if (!X) modifiedToque.x = 0;
            if (!Y) modifiedToque.y = 0;
            if (!Z) modifiedToque.z = 0;

            vehicle.RigidBody.AddRelativeTorque(modifiedToque * Force, ForceMode.Acceleration);
        }
    }

}
