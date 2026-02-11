// Extends the Unity IK System with useful functions in the animator.

namespace JUTPS.ExtendedInverseKinematics
{
    using UnityEngine;

    public static class JUInverseKinematicExtension
    {
        /// <summary>
        /// Easy way to set left hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionLeftHand"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        public static void SetLeftHandOn(this Animator anim, Transform IKPositionLeftHand, float IKWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.LeftHand, IKPositionLeftHand.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, IKPositionLeftHand.rotation);
        }

        /// <summary>
        /// Easy way to set right hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionRightHand"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        public static void SetRightHandOn(this Animator anim, Transform IKPositionRightHand, float IKWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.RightHand, IKPositionRightHand.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, IKPositionRightHand.rotation);
        }

        /// <summary>
        /// Easy way to set left hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionLeftHand"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetLeftHandOn(this Animator anim, Transform IKPositionLeftHand, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.LeftHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftHand, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.LeftHand, IKPositionLeftHand.position);
            anim.SetIKRotation(AvatarIKGoal.LeftHand, IKPositionLeftHand.rotation);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }

        /// <summary>
        /// Easy way to set right hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionRightHand"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetRightHandOn(this Animator anim, Transform IKPositionRightHand, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.RightHand, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightHand, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.RightHand, IKPositionRightHand.position);
            anim.SetIKRotation(AvatarIKGoal.RightHand, IKPositionRightHand.rotation);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.RightElbow, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.RightElbow, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }



        /// <summary>
        /// Easy way to set left hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionLeftFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        public static void SetLeftFootOn(this Animator anim, Transform IKPositionLeftFoot, float IKWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, IKPositionLeftFoot.position);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, IKPositionLeftFoot.rotation);
        }

        /// <summary>
        /// Easy way to set right hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionRightFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        public static void SetRightFootOn(this Animator anim, Transform IKPositionRightFoot, float IKWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.RightFoot, IKPositionRightFoot.position);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, IKPositionRightFoot.rotation);
        }

        /// <summary>
        /// Easy way to set left hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionLeftFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetLeftFootOn(this Animator anim, Transform IKPositionLeftFoot, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, IKPositionLeftFoot.position);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, IKPositionLeftFoot.rotation);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.LeftKnee, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }

        /// <summary>
        /// Easy way to set right hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionRightFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetRightFootOn(this Animator anim, Transform IKPositionRightFoot, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.RightFoot, IKPositionRightFoot.position);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, IKPositionRightFoot.rotation);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.RightKnee, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }


        /// <summary>
        /// Easy way to set left hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionLeftFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetLeftFootOn(this Animator anim, Vector3 IKPositionLeftFoot, Quaternion IKRotationLeftFoot, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.LeftFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.LeftFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.LeftFoot, IKPositionLeftFoot);
            anim.SetIKRotation(AvatarIKGoal.LeftFoot, IKRotationLeftFoot);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.LeftKnee, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.LeftKnee, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }

        /// <summary>
        /// Easy way to set right hand ik position using a transform reference
        /// </summary>
        /// <param name="IKPositionRightFoot"> transform to get position and rotation</param>
        /// <param name="IKWeight">IK Weight</param>
        /// <param name="HintAjust">Offset vector for hint adjust</param>
        /// <param name="HintWeight">Hint IK Weight</param>
        public static void SetRightFootOn(this Animator anim, Vector3 IKPositionRightFoot, Quaternion IKRotationRightFoot, float IKWeight, Vector3 HintAjust, float HintWeight)
        {
            //Set Weight
            anim.SetIKRotationWeight(AvatarIKGoal.RightFoot, IKWeight);
            anim.SetIKPositionWeight(AvatarIKGoal.RightFoot, IKWeight);
            //Set Position
            anim.SetIKPosition(AvatarIKGoal.RightFoot, IKPositionRightFoot);
            anim.SetIKRotation(AvatarIKGoal.RightFoot, IKRotationRightFoot);
            //Set Hint Weight
            anim.SetIKHintPositionWeight(AvatarIKHint.RightKnee, HintWeight);
            //Set Hint Position
            anim.SetIKHintPosition(AvatarIKHint.RightKnee, anim.transform.position + anim.transform.right * HintAjust.x + anim.transform.up * HintAjust.y + anim.transform.forward * HintAjust.z);
        }



        /// <summary>
        /// Easy way to make a character spine lean, quite useful.
        /// </summary>
        /// <param name="LeanVector">Vector with values to add lean</param>
        /// <param name="Weight">IK weight</param>
        public static void SpineInclination(this Animator anim, Vector3 LeanVector, float Weight = 0)
        {
            //Weight Lean
            LeanVector = Vector3.Lerp(Vector3.zero, LeanVector, Weight);

            //Get transform reference
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);

            //Get local euler
            Vector3 localeulerangles = spine.localEulerAngles;

            //Create a new euler
            Vector3 InclinationEuler = localeulerangles;

            //Modify euler with lean vector
            InclinationEuler.x = localeulerangles.x + LeanVector.x;
            InclinationEuler.y = localeulerangles.y + LeanVector.y;
            InclinationEuler.z = localeulerangles.z + LeanVector.z;

            //Convert euler to quaternion
            Quaternion localrotation = Quaternion.Euler(InclinationEuler);

            //Apply spine local quaterion rotation
            anim.SetBoneLocalRotation(HumanBodyBones.Spine, localrotation);
        }

        /// <summary>
        /// Easy way to make a character spine lean, quite useful.
        /// </summary>
        /// <param name="Direction">Vector with direction value to lean (example: Vector.forward)</param>
        /// <param name="LeanIntensity">Intensity of inclination</param>
        /// <param name="Weight">IK weight</param>
        public static void SpineInclination(this Animator anim, Vector3 Direction, float LeanIntensity, float Weight = 1)
        {
            //Weight Lean
            Direction = Vector3.Lerp(Vector3.zero, Direction, Weight);

            //Get transform reference
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);

            //Get Global euler
            Vector3 GlobalEuler = spine.eulerAngles;

            //Convert euler to quaternion
            Quaternion GlobalRotation = Quaternion.Euler(GlobalEuler + Direction * LeanIntensity);

            //Set Global Rotation
            spine.rotation = GlobalRotation;

            //Apply spine local quaterion rotation
            anim.SetBoneLocalRotation(HumanBodyBones.Spine, spine.localRotation);
        }



        /// <summary>
        /// Easy way to make a spine of character align direction to a position
        /// </summary>
        /// <param name="position">position to look at</param>
        /// <param name="Weight">IK weight</param>
        public static void SpineLookAtUnclamped(this Animator anim, Vector3 position = default(Vector3), float Weight = 1)
        {
            //Get transform reference
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);

            //Apply look at global rotation
            spine.rotation = Quaternion.LookRotation(position - spine.position);
            spine.parent.rotation = Quaternion.Lerp(spine.parent.rotation, spine.rotation, 0.5f * Weight);

            //Apply local rotation to IK
            anim.SetBoneLocalRotation(HumanBodyBones.Spine, spine.localRotation);
        }

        /// <summary>
        /// Easy way to make a character look at a position, maybe you need a exorcist
        /// </summary>
        /// <param name="position">position to look at</param>
        /// <param name="Weight">IK weight</param>
        public static void HeadLookAtUnclamped(this Animator anim, Vector3 position = default(Vector3), float Weight = 1)
        {
            //Get transform reference
            Transform head = anim.GetBoneTransform(HumanBodyBones.Head);

            //Apply look at global rotation
            head.rotation = Quaternion.LookRotation(position - head.position);
            head.parent.rotation = Quaternion.Lerp(head.parent.rotation, head.rotation, 0.5f * Weight);

            //Apply local rotation to IK
            anim.SetBoneLocalRotation(HumanBodyBones.Head, head.localRotation);
        }

        /// <summary>
        /// Easy way to make a character look at a position
        /// </summary>
        /// <param name="position">position to look at</param>
        /// <param name="Weight">IK weight</param>
        public static void NormalLookAt(this Animator anim, Vector3 position = default(Vector3), float Weight = 1, float BodyWeight = 0, float GlobalWeight = 1)
        {
            anim.SetLookAtWeight(GlobalWeight, BodyWeight, Weight);
            anim.SetLookAtPosition(position);
        }
        
        /// <summary>
        /// Easy way to get last spine bone (Upperchest Bone)
        /// </summary>
        /// <param name="anim"></param>
        /// <returns>Last spine bone</returns>
        public static Transform GetLastSpineBone(this Animator anim)
        {
            Transform spine = anim.GetBoneTransform(HumanBodyBones.Head).parent.parent;
            return spine;
        }
    }
}
