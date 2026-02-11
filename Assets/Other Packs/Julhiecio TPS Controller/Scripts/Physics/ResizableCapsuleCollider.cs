using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPSEditor.JUHeader;

namespace JUTPS.PhysicsScripts
{

    [RequireComponent(typeof(Animator), typeof(CapsuleCollider))]
    [AddComponentMenu("JU TPS/Physics/Resizable Capsule Collider")]
    public class ResizableCapsuleCollider : JUTPSActions.JUTPSAction
    {
        [JUHeader("Capsule Resize")]
        public CapsuleCollider CapsuleToResize;
        public float HeightOffset = 0.73f;
        public float ProneAndRollCenterY = 0.38f; 
        public Vector3 CenterOffset;

        private float StartHeight;
        private Vector3 StartCenter;

        private Transform rightFoot, leftFoot, head;
        void Start()
        {
            if (CapsuleToResize == null) CapsuleToResize = GetComponent<CapsuleCollider>();

            StartHeight = CapsuleToResize.height;
            StartCenter = CapsuleToResize.center;

            Invoke(nameof(GetFeetReferences), 0.01f);
        }

        // Update is called once per frame
        void Update()
        {
            if (CapsuleToResize == null || head == null || rightFoot == null || leftFoot == null) return;

       
            if (TPSCharacter.IsRolling || TPSCharacter.IsProne)
            {
                CapsuleToResize.direction = 2;
                CapsuleToResize.height = HeadFeetDistance() * StartHeight * HeightOffset;
                CapsuleToResize.center = new Vector3(0, ProneAndRollCenterY, 0);
            }
            else
            {
                if (TPSCharacter.IsGrounded)
                {
                    CapsuleToResize.height = HeadFeetDistance() * StartHeight * HeightOffset;
                    CapsuleToResize.center = CenterOffset + new Vector3(0, CapsuleToResize.height / 2, 0);
                }
                else
                {
                    CapsuleToResize.height = HeadFeetDistance() * StartHeight * HeightOffset;
                    CapsuleToResize.center = CenterOffset + transform.InverseTransformPoint(GetBodyCenter());
                    CapsuleToResize.center = new Vector3(0, CapsuleToResize.center.y, 0);
                }


                CapsuleToResize.direction = 1;
            }

        }
        public float HeadFeetDistance() { return Vector3.Distance(head.position, GetMiddlePointBetweenFeets()); }
        private void GetFeetReferences()
        {
            if (anim == null) { anim = GetComponent<Animator>(); }

            head = anim.GetBoneTransform(HumanBodyBones.Head);
            rightFoot = anim.GetBoneTransform(HumanBodyBones.RightFoot);
            leftFoot = anim.GetBoneTransform(HumanBodyBones.LeftFoot);
        }
        public Vector3 GetMiddlePointBetweenFeets()
        {
            if (rightFoot == null || leftFoot == null)
            {
                return Vector3.zero;
            }

            Vector3 middlePoint = Vector3.Lerp(rightFoot.position, leftFoot.position, 0.5f);
            return middlePoint;
        }
        public Vector3 GetBodyCenter()
        {
            Vector3 middlePoint = Vector3.Lerp(head.position, GetMiddlePointBetweenFeets(), 0.5f);
            return middlePoint;
        }
    }
}