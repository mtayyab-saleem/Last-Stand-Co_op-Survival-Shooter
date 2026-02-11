using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.VehicleSystem
{
    public class JUVehicleCharacterIK : MonoBehaviour
    {
        /// <summary>
        /// Stores the pilot character IK positions on the vehicle.
        /// </summary>
        [System.Serializable]
        public class IKTargetPositions
        {
            /// <summary>
            /// The character position inside the vehicle.
            /// </summary>
            public Transform CharacterPosition;

            /// <summary>
            /// The character left hand position inside the vehicle.
            /// </summary>
            public Transform LeftHandPositionIK;

            /// <summary>
            /// The character right hand position inside the vehicle.
            /// </summary>
            public Transform RightHandPositionIK;

            /// <summary>
            /// The character left foot position inside the vehicle.
            /// </summary>
            public Transform LeftFootPositionIK;

            /// <summary>
            /// The character right foot position inside the vehicle.
            /// </summary>
            public Transform RightFootPositionIK;
        }

        /// <summary>
        /// Stores drive character humanoid animation settings.
        /// </summary>
        [System.Serializable]
        public class DrivingProceduralAnimationWeights
        {
            /// <summary>
            /// The spine inclination on high speeds.
            /// </summary>
            [Min(0)] public float FrontalLeanWeight;

            /// <summary>
            /// The spine inclination on curves.
            /// </summary>
            [Min(0)] public float SideLeanWeight;

            /// <summary>
            /// The influence to head look direction to the vehicle steering direction. 
            /// </summary>
            [Min(0)] public float LookAtDirectionWeight;

            /// <summary>
            /// The hint movement influence .
            /// </summary>
            [Min(0)] public float HintMovementWeight;

            /// <summary>
            /// Use foot placement.
            /// </summary>
            public bool FootPlacement;

            /// <summary>
            /// Create a class that contains all character IK settings to drive a <see cref="Vehicle"/>.
            /// </summary>
            public DrivingProceduralAnimationWeights()
            {
                FootPlacement = false;
                FrontalLeanWeight = 1;
                SideLeanWeight = 1;
                LookAtDirectionWeight = 1;
                HintMovementWeight = 1;
            }
        }

        /// <summary>
        /// If true, any character with <see cref="JUTPS.ActionScripts.DriveVehicles"/> can get this vehicle and drive if the <see cref="Vehicle"/> controls are enabled.
        /// </summary>
        public bool CharactersCanGetVehicle;

        /// <summary>
        /// The position to exit from the vehicle.
        /// </summary>
        [Header("Vehicle Locomotion Settings")]
        public Vector3 CharacterExitingPosition;

        /// <summary>
        /// The IK position to the humanoid character that will control the vehicle.
        /// </summary>
        [Header("Driver Inverse Kinematics")]
        public IKTargetPositions InverseKinematicTargetPositions;

        /// <summary>
        /// The humanoid pilot character animations.
        /// </summary>
        [Header("Driver Procedural Animation Weights")]
        public DrivingProceduralAnimationWeights AnimationWeights;

        /// <summary>
        /// Create an instance of the <seealso cref="JUVehicleCharacterIK"/> component.
        /// </summary>
        public JUVehicleCharacterIK()
        {
            CharacterExitingPosition = Vector3.left;
            CharactersCanGetVehicle = true;

            InverseKinematicTargetPositions = new IKTargetPositions();
            AnimationWeights = new DrivingProceduralAnimationWeights();
        }

        private void OnDrawGizmos()
        {
            Vehicle.VehicleGizmo.DrawVector3Position(CharacterExitingPosition, transform, "Exit Position", Color.green);
        }

        /// <summary>
        /// Return the character exit position from the <see cref="Vehicle"/>.
        /// </summary>
        /// <param name="groundLayer"></param>
        /// <returns></returns>
        public Vector3 GetExitPosition(LayerMask groundLayer)
        {
            Vector3 exitPosition = CharacterExitingPosition;
            Vector3 oppositeExitPosition = CharacterExitingPosition;
            oppositeExitPosition.x = -CharacterExitingPosition.x;

            Vector3 raycastOriginPosition = transform.position + transform.forward * CharacterExitingPosition.z + transform.up * CharacterExitingPosition.y;

            bool exitPositionAvaliable = !Physics.Raycast(raycastOriginPosition, -transform.right, Mathf.Abs(CharacterExitingPosition.x), groundLayer);
            bool oppositeExitPositionAvaliable = !Physics.Raycast(raycastOriginPosition, transform.right, Mathf.Abs(CharacterExitingPosition.x), groundLayer);

            Vector3 finalExitPosition = Vector3.zero;

            if (exitPositionAvaliable) finalExitPosition = exitPosition;
            if (oppositeExitPositionAvaliable && !exitPositionAvaliable) finalExitPosition = oppositeExitPosition;
            if (!oppositeExitPositionAvaliable && !exitPositionAvaliable) finalExitPosition = Vector3.zero;

            if (finalExitPosition != Vector3.zero)
                return transform.TransformPoint(finalExitPosition);

            return Vector3.zero;
        }
    }
}