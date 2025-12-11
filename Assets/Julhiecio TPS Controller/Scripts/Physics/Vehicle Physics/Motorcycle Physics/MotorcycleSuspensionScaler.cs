using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using JUTPSEditor.JUHeader;

namespace JUTPS.VehicleSystem
{

    [ExecuteInEditMode]
    [AddComponentMenu("JU TPS/Vehicle System/Motorcycle Suspension")]
    public class MotorcycleSuspensionScaler : MonoBehaviour
    {
        [JUHeader("Target")]
        public Transform WheelTarget;

        [JUHeader("Scaling/Streching Suspension")]
        [Space(10)]
        public bool Scale = true;
        public float LenghtOffset;
        public float MaxDistance;

        [JUHeader("Suspension Direction Options")]
        [Space(10)]
        public bool LookAt = true;
        public bool InvertLookAt;
        public float HeightOffset;

        [JUHeader("Fix Wheel VISUAL Position")]
        [Space(10)]
        public WheelCollider WheelColliderTarget;
        public Transform HandleBarForwardDirection;
        public float Offset = 0.1f;
        public bool MoveSuspension;
        public float SuspensionOfsset;
        public float Lenght;

        //public bool ReplaceZPosition;
        //public WheelCollider WheelToReplaceZPosition;
        // public Transform TransformToGetZPosition;
        void LateUpdate()
        {
            if (WheelTarget != null)
            {
                if (LookAt)
                {
                    if (InvertLookAt)
                    {
                        transform.rotation = Quaternion.LookRotation(WheelTarget.position - transform.position + transform.up * HeightOffset, transform.parent.up);
                    }
                    else
                    {
                        transform.rotation = Quaternion.LookRotation(transform.position - WheelTarget.position - transform.up * HeightOffset, transform.parent.up);
                    }
                    var rot = transform.localEulerAngles;
                    rot.y = 0;
                    transform.localEulerAngles = rot;
                }
                if (Scale)
                {
                    float dist = Vector3.Distance(transform.position, WheelTarget.position);
                    if (MaxDistance == 0 || dist * LenghtOffset < MaxDistance)
                    {
                        transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y, dist * LenghtOffset);
                    }
                }

                if (WheelColliderTarget != null && HandleBarForwardDirection != null)
                {
                    float dist = Vector3.Distance(HandleBarForwardDirection.position, WheelTarget.position);

                    Vector3 pos;
                    Quaternion rot;
                    WheelColliderTarget.GetWorldPose(out pos, out rot);

                    if (MoveSuspension)
                    {
                        WheelTarget.transform.position = pos - HandleBarForwardDirection.forward * (dist * Lenght) + HandleBarForwardDirection.forward * Offset;
                        transform.position = WheelTarget.transform.position + transform.forward * SuspensionOfsset;
                    }
                    else
                    {
                        WheelTarget.transform.position = pos - HandleBarForwardDirection.forward * Offset * transform.localScale.z + HandleBarForwardDirection.forward * Offset;
                    }
                }

                //if (ReplaceZPosition && TransformToGetZPosition != null && WheelToReplaceZPosition != null)
                // WheelToReplaceZPosition.transform.position = new Vector3 (WheelToReplaceZPosition.transform.position.x, WheelToReplaceZPosition.transform.position.y, TransformToGetZPosition.position.z);
            }
        }
        private void OnDrawGizmos()
        {
            if (WheelColliderTarget != null && HandleBarForwardDirection != null)
            {
                Gizmos.DrawLine(WheelTarget.transform.position, WheelTarget.transform.position + HandleBarForwardDirection.forward * Offset * transform.localScale.z);
                Gizmos.DrawSphere(WheelTarget.transform.position, 0.02f);
                Gizmos.DrawSphere(WheelTarget.transform.position + HandleBarForwardDirection.forward * Offset * transform.localScale.z, 0.02f);
            }
        }
    }

}