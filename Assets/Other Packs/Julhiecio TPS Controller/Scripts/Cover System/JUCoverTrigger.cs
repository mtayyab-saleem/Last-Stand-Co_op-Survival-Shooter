using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JUTPS.CoverSystem
{
    [RequireComponent(typeof(BoxCollider))]
    public class JUCoverTrigger : MonoBehaviour
    {
        private BoxCollider boxColliderTrigger;

        public bool IsCrouchingCover = false;
        void Start() { if (boxColliderTrigger == null) boxColliderTrigger = GetComponent<BoxCollider>(); }

        public Vector3 LeftEndPoint() { return transform.position - transform.right * (transform.lossyScale.x * boxColliderTrigger.size.x) / 2; }
        public Vector3 RightEndPoint() { return transform.position + transform.right * (transform.lossyScale.x * boxColliderTrigger.size.x) / 2; }
        public float CoverMovementLineLenght() { return Vector3.Distance(LeftEndPoint(), RightEndPoint()); }
        public Vector3 GetCoverWallClosestPoint(Vector3 characterPosition) { return GetClosestPointOnFiniteLine(characterPosition, LeftEndPoint(), RightEndPoint()); }
        public static Vector3 GetClosestPointOnFiniteLine(Vector3 point, Vector3 lineStart, Vector3 lineEnd)
        {
            Vector3 lineDirection = lineEnd - lineStart;
            float lineLength = lineDirection.magnitude;
            lineDirection.Normalize();
            float projectLength = Mathf.Clamp(Vector3.Dot(point - lineStart, lineDirection), 0f, lineLength);
            return lineStart + lineDirection * projectLength;
        }
        public float LerpValue(Vector3 characterPosition)
        {
            Vector3 LineDirection = RightEndPoint() - LeftEndPoint();
            Vector3 AV = characterPosition - LeftEndPoint();
            return Vector3.Dot(AV, LineDirection) / Vector3.Dot(LineDirection, LineDirection);

        }

        // > Variable to test functions
        //public Transform playerTest;
        //public float LerpValueTest;

        private void OnDrawGizmos()
        {
            if (boxColliderTrigger == null)
            {
                boxColliderTrigger = GetComponent<BoxCollider>();
                return;
            }

            // > View test
            /*if (playerTest != null)
            {
                Gizmos.DrawWireCube(GetClosestPointOnFiniteLine(playerTest.position, LeftEndPoint(), RightEndPoint()), Vector3.one);
                LerpValueTest = LerpValue(playerTest.position);
            }*/

            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
            Gizmos.color = new Color(0.2f, 0.2f, 0.7f, 0.3f);

            Gizmos.DrawCube(boxColliderTrigger.center, boxColliderTrigger.size);
            Gizmos.color = new Color(1, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireCube(boxColliderTrigger.center, boxColliderTrigger.size);

#if UNITY_EDITOR
            UnityEditor.Handles.color = Color.yellow;
            UnityEditor.Handles.ArrowHandleCap(0, transform.position, transform.rotation, -0.3f, EventType.Repaint);
            UnityEditor.Handles.ArrowHandleCap(0, LeftEndPoint(), transform.rotation, -0.6f, EventType.Repaint);
            UnityEditor.Handles.ArrowHandleCap(0, RightEndPoint(), transform.rotation, -0.6f, EventType.Repaint);

#endif
        }
    }
}