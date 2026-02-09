using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.VehicleSystem
{
    /// <summary>
    /// Create a wheel seer animation for vehicles.
    /// </summary>
    public class JUVehicleAnimatedSteerWheel : MonoBehaviour
    {
        /// <summary>
        /// The wheel steer rotation multiplier.
        /// </summary>
        public float RotationMultiplier;

        /// <summary>
        /// The wheel steer pivot.
        /// </summary>
        public Transform SteeringWheel;

        /// <summary>
        /// The wheel collider to access the steer.
        /// </summary>
        public WheelCollider ReferenceWheel;

        /// <summary>
        /// Create a <see cref="JUVehicleAnimatedSteerWheel"/> component.
        /// </summary>
        public JUVehicleAnimatedSteerWheel()
        {
            RotationMultiplier = 1;
        }

        void Start()
        {
            CreateSteeringWheelRotationPivot(SteeringWheel);
        }

        void Update()
        {
            SteeringWheel.transform.localEulerAngles = SteeringWheelRotation(SteeringWheel, ReferenceWheel).eulerAngles;
        }

        private void CreateSteeringWheelRotationPivot(Transform SteeringWheel)
        {
            //Create a pivot and setup transformations
            GameObject SteeringWheelRotationAxisFix = new GameObject("Steering Wheel");
            SteeringWheelRotationAxisFix.transform.position = SteeringWheel.position;
            SteeringWheelRotationAxisFix.transform.rotation = SteeringWheel.rotation;
            SteeringWheelRotationAxisFix.transform.parent = SteeringWheel.transform.parent;

            //Steering wheel parenting with the pivot
            SteeringWheel.transform.parent = SteeringWheelRotationAxisFix.transform;
        }

        private Quaternion SteeringWheelRotation(Transform SteeringWheel, WheelCollider WheelToGetSteerAngle, float MultiplySteeringWheelRotation = 1)
        {
            var SteeringWheelRotation = MultiplySteeringWheelRotation * WheelToGetSteerAngle.steerAngle * RotationMultiplier;
            Vector3 RotationEuler = new Vector3(SteeringWheel.localEulerAngles.x, SteeringWheelRotation, SteeringWheel.transform.localEulerAngles.x);
            return Quaternion.Euler(RotationEuler);
        }
    }
}