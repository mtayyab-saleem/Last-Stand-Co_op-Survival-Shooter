using UnityEngine;
using JUTPS.ExtendedInverseKinematics;
using JUTPS.VehicleSystem;
using JUTPS.ActionScripts;

namespace JUTPS.FX
{
    [AddComponentMenu("JU TPS/FX/Driver Procedural Animation")]
    [RequireComponent(typeof(DriveVehicles))]
    public class ProceduralDrivingAnimation : JUTPSActions.JUTPSAction
    {
        private DriveVehicles DriveAbility;

        [Header("Settings")]
        public bool Enabled = true;
        public bool FootPlacer;
        private Transform LeftFootTargetPosition, RightFootTargetPosition;

        public LayerMask GroundLayer;
        [Header("Spine Lean")]
        [SerializeField] private bool SpineLean = true;
        [Range(0, 1)]
        [SerializeField] private float LeanDirection = 0.2f;
        [SerializeField] private BodyLeanInert.Axis ForwardLeanAxis = BodyLeanInert.Axis.X;
        [SerializeField] private BodyLeanInert.Axis SidesLeanAxis = BodyLeanInert.Axis.Z;
        public bool InvertForwardLean;
        public bool InvertSideLean;

        private void Start()
        {
            DriveAbility = GetComponent<DriveVehicles>();

            LeftFootTargetPosition = new GameObject("LeftFootTargetPosition").transform;
            RightFootTargetPosition = new GameObject("RightFootTargetPosition").transform;

            LeftFootTargetPosition.hideFlags = HideFlags.HideInHierarchy;
            RightFootTargetPosition.hideFlags = HideFlags.HideInHierarchy;

            LeftFootTargetPosition.position = transform.position;
            RightFootTargetPosition.position = transform.position;

            LeftFootTargetPosition.parent = transform;
            RightFootTargetPosition.parent = transform;

        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!Enabled || !DriveAbility || DriveAbility.DisableCharacterOnEnter || !DriveAbility.IsDriving)
                return;

            DoProceduralDrivingAnimation(DriveAbility.CurrentVehicle, DriveAbility.CurrentVehicleCharacterIK);
        }

        protected virtual void DoProceduralDrivingAnimation(Vehicle vehicle, JUVehicleCharacterIK vehicleCharacterIK)
        {
            if (!vehicle || !vehicleCharacterIK || TPSCharacter.IsRagdolled)
                return;

            HeadLookAtAnimation(vehicle, vehicleCharacterIK);

            var vehicleSpeed = Mathf.Clamp(Mathf.Abs(vehicle.ForwardSpeed), 0, 15);

            // Hand IK
            if (vehicleCharacterIK.InverseKinematicTargetPositions.RightHandPositionIK &&
                vehicleCharacterIK.InverseKinematicTargetPositions.LeftHandPositionIK)
            {
                anim.SetLeftHandOn(vehicleCharacterIK.InverseKinematicTargetPositions.LeftHandPositionIK, 1);
                anim.SetRightHandOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightHandPositionIK, 1);
            }

            if (vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK &&
                vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK)
            {
                //Set procedural hint movements values
                float leftHint = 6 * Mathf.Clamp(vehicle.FinalSteer, -1, 0) * vehicleSpeed / 20;
                float rightHint = 6 * Mathf.Clamp(vehicle.FinalSteer, 0, 1) * vehicleSpeed / 20;

                //Create procedural hint movement
                float HintSpace = 3 * vehicleCharacterIK.AnimationWeights.HintMovementWeight;
                Vector3 LeftHintLocalPosition = Vector3.zero - Vector3.right * (HintSpace - leftHint) + Vector3.forward * 10;
                Vector3 RightHintLocalPosition = Vector3.zero + Vector3.right * (HintSpace + rightHint) + Vector3.forward * 10;

                if (FootPlacer && vehicleCharacterIK.AnimationWeights.FootPlacement)
                {
                    Vector3 RightFootOriginalPosition = vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.position;
                    Vector3 LeftFootOriginalPosition = vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK.position;

                    //Set Raycasts
                    RaycastHit LeftGroundHit;
                    Physics.Raycast(LeftFootOriginalPosition + vehicle.transform.forward * vehicleSpeed / 5 - vehicle.transform.right * 0.2f, -vehicle.transform.up, out LeftGroundHit, 0.8f, GroundLayer);

                    RaycastHit RightGroundHit;
                    Physics.Raycast(RightFootOriginalPosition + vehicle.transform.forward * vehicleSpeed / 5 + vehicle.transform.right * 0.2f, -vehicle.transform.up, out RightGroundHit, 0.8f, GroundLayer);

                    //Set ground position
                    Vector3 LeftFootOnGroundPosition = LeftGroundHit.collider ? LeftGroundHit.point + LeftGroundHit.normal * 0.15f : LeftFootOriginalPosition;
                    Vector3 RightFootOnGroundPosition = RightGroundHit.collider ? RightGroundHit.point + RightGroundHit.normal * 0.15f : RightFootOriginalPosition;

                    Vector3 NewLeftFootPosition = Vector3.Lerp(LeftFootOnGroundPosition, LeftFootOriginalPosition, vehicleSpeed / 5);
                    Vector3 NewRightFootPosition = Vector3.Lerp(RightFootOnGroundPosition, RightFootOriginalPosition, vehicleSpeed / 5);

                    Quaternion NewLeftFootRotation = Quaternion.Lerp(Quaternion.FromToRotation(LeftFootTargetPosition.up, LeftGroundHit.normal) * LeftFootTargetPosition.rotation, vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK.rotation, vehicleSpeed / 5);
                    Quaternion NewRightFootRotation = Quaternion.Lerp(Quaternion.FromToRotation(RightFootTargetPosition.up, RightGroundHit.normal) * RightFootTargetPosition.rotation, vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.rotation, vehicleSpeed / 5);

                    LeftFootTargetPosition.position = NewLeftFootPosition; LeftFootTargetPosition.rotation = NewLeftFootRotation;
                    RightFootTargetPosition.position = NewRightFootPosition; RightFootTargetPosition.rotation = NewRightFootRotation;

                    //Set foot on targets and apply hint movement
                    anim.SetLeftFootOn(LeftFootTargetPosition.position, LeftFootTargetPosition.rotation, 1, LeftHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                    anim.SetRightFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK.position, RightFootTargetPosition.rotation, 1, RightHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                }
                else
                {
                    anim.SetLeftFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.LeftFootPositionIK, 1, LeftHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                    anim.SetRightFootOn(vehicleCharacterIK.InverseKinematicTargetPositions.RightFootPositionIK, 1, RightHintLocalPosition, vehicleCharacterIK.AnimationWeights.HintMovementWeight);
                }
            }

            //Spine Lean
            if (!SpineLean) return;

            //Get Lean Values
            float SidewayLeanWeight = -vehicle.FinalSteer * (vehicleSpeed / 5);
            float ForwardLeanWeight = vehicle.FinalThrottle * (vehicleSpeed / 4f);

            Vector3 ForwardAxis = new Vector3(0, 0, 0);
            switch (ForwardLeanAxis)
            {
                case BodyLeanInert.Axis.X:
                    ForwardAxis = InvertForwardLean ? Vector3.left : Vector3.right;
                    break;
                case BodyLeanInert.Axis.Y:
                    ForwardAxis = InvertForwardLean ? Vector3.down : Vector3.up;
                    break;
                case BodyLeanInert.Axis.Z:
                    ForwardAxis = InvertForwardLean ? Vector3.back : Vector3.forward;
                    break;
            }

            Vector3 SideAxis = new Vector3(0, 0, 0);
            switch (SidesLeanAxis)
            {
                case BodyLeanInert.Axis.X:
                    SideAxis = InvertSideLean ? Vector3.left : Vector3.right;
                    break;
                case BodyLeanInert.Axis.Y:
                    SideAxis = InvertSideLean ? Vector3.down : Vector3.up;
                    break;
                case BodyLeanInert.Axis.Z:
                    SideAxis = InvertSideLean ? Vector3.back : Vector3.forward;
                    break;
            }

            //Apply Forward Lean
            anim.SpineInclination(ForwardAxis, ForwardLeanWeight, vehicleCharacterIK.AnimationWeights.FrontalLeanWeight);
            //Apply Sideways Lean
            anim.SpineInclination(Vector3.Lerp(Vector3.up, SideAxis, LeanDirection), SidewayLeanWeight, vehicleCharacterIK.AnimationWeights.SideLeanWeight);
        }

        private void HeadLookAtAnimation(Vehicle vehicle, JUVehicleCharacterIK vehicleCharacterIK)
        {
            if (!vehicle || !vehicleCharacterIK)
                return;

            // Head look at move direction
            Vector3 LookVehicleDirection = transform.position + vehicle.transform.forward * 10 + vehicle.transform.up * 0.6f + vehicle.transform.right * vehicle.FinalSteer * 8;
            anim.NormalLookAt(LookVehicleDirection, vehicleCharacterIK.AnimationWeights.LookAtDirectionWeight, 0, 1);
        }
    }
}
