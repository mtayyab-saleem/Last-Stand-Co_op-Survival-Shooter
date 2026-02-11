using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.VehicleSystem
{

    public class BikePedal : MonoBehaviour
    {
        [Header("Pedal Rotation")]
        public Vehicle Bike;
        public WheelCollider BackWheel;
        public float PedalRotateSpeed = 0.2f;
        public Transform RightPedal, LeftPedal;
        [Header("IK Targets")]
        public Transform FootUpOrientator;
        public Transform RightFootTarget;
        public Transform LeftFootTarget;

        void Start()
        {
            Bike = GetComponentInParent<Vehicle>();
            if (FootUpOrientator == null && Bike != null) FootUpOrientator = Bike.transform;
        }
        void Update()
        {
            if (BackWheel == null || FootUpOrientator == null || Bike == null || LeftFootTarget == null || RightFootTarget == null || !Bike.IsGrounded) return;
            transform.Rotate(BackWheel.motorTorque * (PedalRotateSpeed * Mathf.Abs(Bike.ForwardSpeed) / Bike.Engine.MaxForwardSpeed) * Time.deltaTime, 0, 0);

            Quaternion rightRotation = Quaternion.FromToRotation(RightPedal.up, FootUpOrientator.up) * RightPedal.rotation;
            RightPedal.rotation = rightRotation;

            Quaternion leftRotation = Quaternion.FromToRotation(LeftPedal.up, FootUpOrientator.up) * LeftPedal.rotation;
            LeftPedal.rotation = leftRotation;
        }
    }

}