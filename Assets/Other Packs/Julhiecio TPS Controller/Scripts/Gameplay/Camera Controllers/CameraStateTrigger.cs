using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace JUTPS.CameraSystems
{

    [AddComponentMenu("JU TPS/Third Person System/Cameras/Camera State Trigger")]
    public class CameraStateTrigger : MonoBehaviour
    {
        public float TransitionSpeed = 8;
        public string CustomStateName = "";
        public CameraState CameraState = new CameraState("Camera State");

        //private CameraState OverridedCurrentCameraState;
        private bool IsTransitioning;

        private JUCameraController mCameraController;
        void Awake()
        {
            mCameraController = FindObjectOfType<JUCameraController>();
        }

        // Update is called once per frame
        void Update()
        {
            if (IsCameraInsideBounds(mCameraController.transform.position))
            {
                //Debug.Log("Is inside");
                if (CameraState == null || CustomStateName != "")
                {
                    mCameraController.SetCustomCameraStateTransition(mCameraController.GetCurrentCameraState, CustomStateName, TransitionSpeed);
                }
                else
                {
                    mCameraController.IsTransitioningToCustomState = true;
                    mCameraController.SetCameraStateTransition(mCameraController.GetCurrentCameraState, CameraState, TransitionSpeed);
                }
                IsTransitioning = true;
            }
            else if (IsTransitioning == true)
            {
                mCameraController.DisableCustomStateTransitioningState();
            }
        }
        public bool IsCameraInsideBounds(Vector3 CameraPosition)
        {
            var bounds = new Bounds(transform.position, transform.localScale);
            return bounds.Contains(CameraPosition);
        }


        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Gizmos.color = Color.white;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        }
        private void OnDrawGizmos()
        {
            Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(Vector3.zero, Vector3.one);

            Color YellowTransparent = Color.yellow;
            YellowTransparent.a = 0.2f;
            Gizmos.color = YellowTransparent;

            Gizmos.DrawCube(Vector3.zero, Vector3.one);
        }
    }

}