using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.GravitySwitchSystem
{
    [AddComponentMenu("JU TPS/Third Person System/Gravity Switcher/Gravity Box")]
    public class GravityBox : MonoBehaviour
    {
        [Header("Settings")]
        public float GravityForce = -35;
        public string[] TagsToIgnore;

        [Header("Alignment")]
        public bool AlignRigidbodies;
        public bool AlignCharacters;
        public float AlignmentForce = -35;
        public float DistanceToStopAligment;

        void Update()
        {
            Collider[] colliders;
            JUGravity.SimulateGravityBox(transform.position, transform.lossyScale, transform.rotation, -transform.up, GravityForce, AlignRigidbodies, AlignmentForce, DistanceToStopAligment, out colliders, TagsToIgnore);
            if (AlignCharacters) JUGravity.AlignJUTPSCharacterUpOrientation(colliders, transform.up);
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.localScale);
            Gizmos.matrix = rotationMatrix;

            Gizmos.color = new Color(0, 1, 0, 0.1f);
            Gizmos.DrawCube(Vector3.zero, transform.localScale);

            Gizmos.color = new Color(1, 1, 1, 0.2f);
            Gizmos.DrawWireCube(Vector3.zero, transform.localScale);

            UnityEditor.Handles.ArrowHandleCap(0, transform.position + transform.up * 0.5f, Quaternion.LookRotation(-transform.up), 1, EventType.Repaint);
        }
#endif
    }

}