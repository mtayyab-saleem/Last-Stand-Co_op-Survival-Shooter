using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS
{
    [AddComponentMenu("JU TPS/Tools/JU Gizmo Drawer")]
    public class JUGizmoDrawer : MonoBehaviour
    {
        public bool Draw = true;
        public DrawMesh ModelToDraw = DrawMesh.Humanoid;
        public DrawType DrawMode = DrawType.Both;
        public Color GizmoColor = new Color(0.2F, 0.2F, 0.2F, .5F);
        public Color WireframeColor = new Color(1F, 1F, 1F, .1F);
        public bool MirrorX;

        public enum DrawType { Solid, Wireframe, Both }
        public enum DrawMesh { Hand, ClosedHand, ArmedHand, Foot, Steps, Humanoid, Point }

        private static Mesh Hand, ClosedHand, ArmedHand, Foot, Steps, Humanoid;

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (Humanoid == null)
            {
                LoadMeshes();
            }
            else
            {
                var t = transform;
                t.localScale = new Vector3(MirrorX ? -t.localScale.z : t.localScale.z, t.localScale.y, t.localScale.z);
                var matrixx = t.localToWorldMatrix;

                Gizmos.matrix = matrixx;
                Gizmos.color = GizmoColor;
                switch (ModelToDraw)
                {
                    case DrawMesh.Hand:
                        DrawGizmoMesh(Hand, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.ClosedHand:
                        DrawGizmoMesh(ClosedHand, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.ArmedHand:
                        DrawGizmoMesh(ArmedHand, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.Foot:
                        DrawGizmoMesh(Foot, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.Steps:
                        DrawGizmoMesh(Steps, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.Humanoid:
                        DrawGizmoMesh(Humanoid, DrawMode, wireframeColor: WireframeColor);
                        break;
                    case DrawMesh.Point:
                        Gizmos.DrawSphere(Vector3.zero, 0.07f);
                        Gizmos.color = WireframeColor;
                        Gizmos.DrawWireSphere(Vector3.zero, 0.07f);
                        break;
                }
            }
        }
#endif



        private static void LoadMeshes()
        {
            Hand = GetEditorResourceModel("Hand Visualizer Model");
            ClosedHand = GetEditorResourceModel("Hand Closed Visualizer Model");
            ArmedHand = GetEditorResourceModel("Hand Armed Visualizer Model");
            Foot = GetEditorResourceModel("Foot Visualizer Model");
            Steps = GetEditorResourceModel("Step Visualizer Model");

            Humanoid = GetEditorResourceModel();


            //print("Loaded Visualization Meshes");
        }

        /// <summary>
        /// Return a mesh in the path Editor/Editor Resources/GizmosModels/
        /// </summary>
        public static Mesh GetEditorResourceModel(string ModelName = "Humanoid Visualizer Model")
        {
#if UNITY_EDITOR

            if (Resources.Load("Editor Resources/Models/" + ModelName) == null)
            {
                Debug.Log("Unable to load model, check model name and directory path.");
                return null;
            }

            var LoadedMesh = Resources.Load("Editor Resources/Models/" + ModelName) as GameObject;
            return LoadedMesh.GetComponent<MeshFilter>().sharedMesh;
#else
        Debug.Log("Unable to load editor models without being in editor");
        return null;
#endif
        }


        public static Mesh GetJUGizmoDefaultMesh(DrawMesh mesh)
        {
            if (Humanoid == null)
            {
                LoadMeshes();
            }

            switch (mesh)
            {
                case DrawMesh.Hand:
                    return Hand;
                case DrawMesh.ClosedHand:
                    return ClosedHand;
                case DrawMesh.ArmedHand:
                    return ArmedHand;
                case DrawMesh.Foot:
                    return Foot;
                case DrawMesh.Steps:
                    return Steps;
                case DrawMesh.Humanoid:
                    return Humanoid;
                case DrawMesh.Point:
                    return null;
                default:
                    return null;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Call this on the void OnDrawGizmo()
        /// </summary>
        public static void DrawGizmoMesh(Mesh MeshToDraw, DrawType RenderGizmosMode = default, Vector3 GizmoPosition = default, Vector3 GizmoEulerRotation = default, Color wireframeColor = default)
        {

            if (MeshToDraw == null)
            {
                Debug.LogWarning("Mesh to draw are null");
                return;
            }
            switch (RenderGizmosMode)
            {
                case DrawType.Both:
                    Gizmos.DrawMesh(MeshToDraw, 0, GizmoPosition, Quaternion.Euler(GizmoEulerRotation));
                    Gizmos.color = wireframeColor;
                    Gizmos.DrawWireMesh(MeshToDraw, 0, GizmoPosition, Quaternion.Euler(GizmoEulerRotation));
                    break;
                case DrawType.Solid:
                    Gizmos.DrawMesh(MeshToDraw, 0, GizmoPosition, Quaternion.Euler(GizmoEulerRotation));
                    break;
                case DrawType.Wireframe:
                    Gizmos.color = wireframeColor;
                    Gizmos.DrawWireMesh(MeshToDraw, 0, GizmoPosition, Quaternion.Euler(GizmoEulerRotation));
                    break;
            }
        }

#endif

        public static JUGizmoDrawer CreateNewJUGizmo(string Name = "JUGizmo", Vector3 Position = default, Quaternion Rotation = default, DrawMesh ModelToDraw = DrawMesh.Humanoid, Color Color = default, Color WireframeColor = default, bool Mirror = default, DrawType DrawMode = DrawType.Both)
        {
            JUGizmoDrawer new_gizmo = new GameObject(Name).AddComponent<JUGizmoDrawer>();
            new_gizmo.transform.position = Position;
            new_gizmo.transform.rotation = Rotation;
            new_gizmo.ModelToDraw = ModelToDraw;
            new_gizmo.GizmoColor = Color;
            new_gizmo.WireframeColor = WireframeColor;
            new_gizmo.MirrorX = Mirror;
            new_gizmo.DrawMode = DrawMode;

            return new_gizmo;
        }


        public static GameObject CreateLeftHandGizmo(Vector3 Position = default, Quaternion Rotation = default, bool Closed = default)
        {
            Color color = new Color(0.3F, 1F, 0.3F, 0.7F);
            Color wcolor = new Color(0.7F, 1F, 0.7F, 0.1F);

            GameObject gizmo = CreateNewJUGizmo("Left Hand Point", Position, Rotation,
                Closed ? DrawMesh.ClosedHand : DrawMesh.Hand, color, wcolor, false, DrawType.Both).gameObject;

            return gizmo;
        }
        public static GameObject CreateRightHandGizmo(Vector3 Position = default, Quaternion Rotation = default, bool Closed = default)
        {
            Color color = new Color(0.3F, 0.5F, 1F, 0.7F);
            Color wcolor = new Color(0.5F, 0.8F, 1F, 0.1F);

            GameObject gizmo = CreateNewJUGizmo("Right Hand Point", Position, Rotation,
                 Closed ? DrawMesh.ClosedHand : DrawMesh.Hand, color, wcolor, true, DrawType.Both).gameObject;

            return gizmo;
        }


        public static GameObject CreateLeftFootGizmo(Vector3 Position = default, Quaternion Rotation = default)
        {
            Color color = new Color(0.3F, 1F, 0.3F, 0.7F);
            Color wcolor = new Color(0.7F, 1F, 0.7F, 0.1F);

            GameObject gizmo = CreateNewJUGizmo("Left Foot Point", Position, Rotation,
                DrawMesh.Foot, color, wcolor, false, DrawType.Both).gameObject;

            return gizmo;
        }
        public static GameObject CreateRightFootGizmo(Vector3 Position = default, Quaternion Rotation = default)
        {
            Color color = new Color(0.3F, 0.5F, 1F, 0.7F);
            Color wcolor = new Color(0.5F, 0.8F, 1F, 0.1F);

            GameObject gizmo = CreateNewJUGizmo("Right Foot Point", Position, Rotation,
                 DrawMesh.Foot, color, wcolor, true, DrawType.Both).gameObject;

            return gizmo;
        }


        public static GameObject CreateRedPointGizmo(Vector3 Position = default, Quaternion Rotation = default)
        {
            Color color = new Color(1F, 0.2F, 0.2F, 0.5F);
            Color wcolor = new Color(1F, 0.7F, 0.7F, 0.7F);

            GameObject gizmo = CreateNewJUGizmo("Red Point", Position, Rotation,
                 DrawMesh.Point, color, wcolor, true, DrawType.Both).gameObject;

            return gizmo;
        }

    }

}