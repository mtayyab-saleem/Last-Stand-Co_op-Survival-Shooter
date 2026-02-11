using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.Utilities
{
    [AddComponentMenu("JU TPS/Tools/JU Character Settings Drawer")]
    public class JUCharacterSettingsDrawer : MonoBehaviour
    {
#if UNITY_EDITOR
        public bool DestroyOnStartGame;
        public JUCharacterController PlayerController;
        public GizmosSettings VisualizationSettings;
        public bool StepCorrection, GroundCheck;
        public bool WallCheck = true;
        void Start()
        {
            if (DestroyOnStartGame)
                Destroy(this);
        }
        private void LoadVisualizationAssets()
        {
            PlayerController = GetComponent<JUCharacterController>();

            VisualizationSettings.StepVisualizerMesh = JUGizmoDrawer.GetJUGizmoDefaultMesh(JUGizmoDrawer.DrawMesh.Steps);
        }
        private void OnDrawGizmos()
        {
            if (VisualizationSettings.StepVisualizerMesh == null)
                LoadVisualizationAssets();

            //Step Visualizer
            float gizmoscale = 1f;
            if (PlayerController == null) return;

            if (PlayerController.WallAHead)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawLine(transform.position + transform.up * PlayerController.WallRayHeight, transform.position + transform.up * PlayerController.WallRayHeight + transform.forward * PlayerController.WallRayDistance);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position + transform.up * PlayerController.WallRayHeight, transform.position + transform.up * PlayerController.WallRayHeight + transform.forward * PlayerController.WallRayDistance);
            }


            if (GroundCheck)
            {
                //Ground Check Debug
                if (PlayerController.IsGrounded == false)
                {
                    Gizmos.color = Color.black;
                    Gizmos.DrawWireCube(transform.position + transform.up * PlayerController.GroundCheckHeighOfsset, new Vector3(PlayerController.GroundCheckRadius, PlayerController.GroundCheckSize, PlayerController.GroundCheckRadius) * 2);
                }
                else
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawWireCube(transform.position + transform.up * PlayerController.GroundCheckHeighOfsset, new Vector3(PlayerController.GroundCheckRadius, PlayerController.GroundCheckSize, PlayerController.GroundCheckRadius) * 2);
                }
            }

            //Step Correction Settings
            if (StepCorrection)
            {
                if (PlayerController.DirectionTransform != null)
                {
                    var step_pos = PlayerController.transform.position + PlayerController.DirectionTransform.transform.forward * PlayerController.ForwardStepOffset + transform.up * PlayerController.StepHeight;
                    var step_pos_height = PlayerController.transform.position + PlayerController.DirectionTransform.transform.forward * PlayerController.ForwardStepOffset + transform.up * PlayerController.FootstepHeight;
                    if (PlayerController.Step_Hit.point != Vector3.zero
                    && PlayerController.Step_Hit.point.y > PlayerController.transform.position.y + PlayerController.StepHeight)
                    {
                        step_pos = PlayerController.Step_Hit.point;
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(PlayerController.Step_Hit.point, 0.05f);
                    }
                    Gizmos.color = VisualizationSettings.GizmosColor;
                    Gizmos.DrawMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos, PlayerController.DirectionTransform.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.color = VisualizationSettings.WireGizmosColor;
                    Gizmos.DrawWireMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos, PlayerController.DirectionTransform.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.color = VisualizationSettings.WireGizmosColor;
                    Gizmos.DrawWireMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos_height, PlayerController.DirectionTransform.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.DrawLine(step_pos, step_pos_height);
                }
                else
                {
                    var step_pos = PlayerController.transform.position + PlayerController.transform.forward * PlayerController.ForwardStepOffset + transform.up * PlayerController.StepHeight;
                    var step_pos_height = PlayerController.transform.position + PlayerController.transform.forward * PlayerController.ForwardStepOffset + transform.up * PlayerController.FootstepHeight;
                    if (PlayerController.Step_Hit.point != Vector3.zero
                    && PlayerController.Step_Hit.point.y > PlayerController.transform.position.y + PlayerController.StepHeight)
                    {
                        step_pos = PlayerController.Step_Hit.point;
                        Gizmos.color = Color.white;
                        Gizmos.DrawWireSphere(PlayerController.Step_Hit.point, 0.05f);
                    }
                    Gizmos.color = VisualizationSettings.GizmosColor;
                    Gizmos.DrawMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos, PlayerController.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.color = VisualizationSettings.WireGizmosColor;
                    Gizmos.DrawWireMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos, PlayerController.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.color = VisualizationSettings.WireGizmosColor;
                    Gizmos.DrawWireMesh(VisualizationSettings.StepVisualizerMesh, 0, step_pos_height, PlayerController.transform.rotation, new Vector3(gizmoscale, gizmoscale, gizmoscale));

                    Gizmos.DrawLine(step_pos, step_pos_height);
                }
            }
        }

#endif

    }

    [System.Serializable]
    public class GizmosSettings
    {
        [HideInInspector] public string ResourcesPath;
        public Color GizmosColor = new Color(0, 0, 0, .5F);
        public Color WireGizmosColor = new Color(0.9F, 0.4F, 0.2F, .5F);

        public Mesh StepVisualizerMesh;
    }
}