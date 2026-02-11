#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

using System.Linq;
using JUTPSEditor.JUHeader;
using JUTPS.Utilities;

namespace JUTPS.Utilities
{

    [AddComponentMenu("JU TPS/Tools/Icon Generator")]
    public class JUIconGenerator : MonoBehaviour
    {
        [JUHeader("Render Options")]
        public Vector2 Resolution = new Vector3(256, 256);
        public RenderModes RenderType;
        public Color VectorColor = Color.white;
        public enum RenderModes { NormalGraphic, VertexLit, ColorVector };
        private static Material SolidColorMaterial;
        [HideInInspector] public Camera cam;
        [HideInInspector] public RenderTexture currentrendertexture;

        [Range(0, 30)]
        [JUHeader("Position Settings")]
        public float Distance = 1.2f;
        public Vector3 CenterOffset;
        [HideInInspector] public Quaternion CameraRotation;
        public bool LockZAxisRotation = true;

        //Camera settings
        [JUHeader("Camera Render Settings")]
        public LayerMask ViewLayer;
        public float FieldOfView = 40;
        public float RenderDistance = 1.5f;
        public CameraViewMode Projection = CameraViewMode.Perspective;
        public enum CameraViewMode { Perspective, Orthographic }
        public string PathToSave = "/Julhiecio TPS Controller/Textures/Generated Icons/";

        [HideInInspector] public Quaternion Angles;

        //OBJECT MESHES, MATERIALS AND ETC...
        [HideInInspector] public Renderer[] MesheRenderers;
        private List<MesheMaterials> PreviousMeshMaterials = new List<MesheMaterials>();


        private class MesheMaterials
        {
            public List<Material> MaterialsListFromMesh = new List<Material>();
        }

        public void StartCameraRenderingModes()
        {
            //Load Solid Color Material
            LoadSolidColorMaterial();

            if (MesheRenderers == null || PreviousMeshMaterials == null)
            {
                //Get all renderers
                MesheRenderers = GetComponentsInChildren<Renderer>();
                ///Debug.Log("RENDERS COUNT: " + MesheRenderers.Length);


                //Save all started materials
                for (int Mesh_ID = 0; Mesh_ID < MesheRenderers.Length; Mesh_ID++)
                {
                    var m = new MesheMaterials();
                    for (int Material_ID = 0; Material_ID < MesheRenderers[Mesh_ID].sharedMaterials.Length; Material_ID++)
                    {
                        m.MaterialsListFromMesh.Add(MesheRenderers[Mesh_ID].sharedMaterials[Material_ID]);
                    }
                    PreviousMeshMaterials.Add(m);
                }
            }
            ///Debug.Log("START MATERIAL:" + PreviousMeshMaterials.Count);
        }
        public void SetRenderersMaterialsToSolidColor()
        {
            if (SolidColorMaterial == null) LoadSolidColorMaterial();

            if (SolidColorMaterial == null)
            {
                Debug.LogError("Cannot set SolidColor material because it could not be loaded");
                return;
            }
            for (int Mesh_ID = 0; Mesh_ID < MesheRenderers.Length; Mesh_ID++)
            {
                var NewSolidColorMaterials = new Material[MesheRenderers[Mesh_ID].sharedMaterials.Length];

                for (int Material_ID = 0; Material_ID < MesheRenderers[Mesh_ID].sharedMaterials.Length; Material_ID++)
                {
                    NewSolidColorMaterials[Material_ID] = SolidColorMaterial;
                }
                MesheRenderers[Mesh_ID].sharedMaterials = NewSolidColorMaterials;
            }
        }
        public void SetRenderersMaterialsToOriginalColors()
        {
            if (MesheRenderers[0].sharedMaterials[0] == PreviousMeshMaterials[0].MaterialsListFromMesh[0]) return;


            for (int Mesh_ID = 0; Mesh_ID < MesheRenderers.Length; Mesh_ID++)
            {
                var NewSolidColorMaterials = new Material[MesheRenderers[Mesh_ID].sharedMaterials.Length];

                for (int Material_ID = 0; Material_ID < MesheRenderers[Mesh_ID].sharedMaterials.Length; Material_ID++)
                {
                    NewSolidColorMaterials[Material_ID] = PreviousMeshMaterials[Mesh_ID].MaterialsListFromMesh[Material_ID];
                }
                MesheRenderers[Mesh_ID].sharedMaterials = NewSolidColorMaterials;
            }
        }
        public void SetSolidColor(Color color)
        {
            SolidColorMaterial.color = color;
        }
        public Color CurrentFirstMaterialColor()
        {
            if (MesheRenderers != null)
            {
                Color color = MesheRenderers[0].sharedMaterial.color;
                return color;
            }
            else
            {
                return Color.magenta;
            }
        }
        public void ClearAllTemporaryReferences()
        {
            ///MesheRenderers = null;
            ///PreviousMeshMaterials = null;
            DestroyImmediate(currentrendertexture);
            if (cam != null)
            {
                DestroyImmediate(cam.gameObject);
            }
            //Debug.Log("Destroyed All temporary references");
        }
        public bool IsRenderingSolidColor()
        {
            if (MesheRenderers == null)
            {
                return false;
            }
            else
            {
                if (MesheRenderers[0].sharedMaterial == null) return false;
                if (MesheRenderers[0].sharedMaterial.color == SolidColorMaterial.color)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }
        private void LoadSolidColorMaterial()
        {
            Material m = Resources.Load("SolidColor") as Material;
            SolidColorMaterial = m;
        }
#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (cam != null)
            {
                Gizmos.DrawLine(cam.transform.position, transform.position + CenterOffset);
                Handles.color = Color.red;
                Handles.ArrowHandleCap(0, cam.transform.position, cam.transform.rotation, 0.3f, EventType.Repaint);
            }
        }
#endif
        public void SaveTexture()
        {
            byte[] bytes = ConvertRenderTextureTo2DTexture(currentrendertexture).EncodeToPNG();
            string path = PathToSave + this.gameObject.name + Random.Range(0000, 9000) + ".png";
            if (System.IO.Directory.Exists(Application.dataPath + PathToSave) == false)
            {
                System.IO.Directory.CreateDirectory(Application.dataPath + PathToSave);
                Debug.Log("Icon Generator created the directory: " + Application.dataPath + PathToSave);
            }
            System.IO.File.WriteAllBytes(Application.dataPath + path, bytes);
            Debug.Log("Your icon has been saved on folder:   " + path);
        }
        public Texture2D ConvertRenderTextureTo2DTexture(RenderTexture rTex)
        {
            if (rTex == null)
            {
                Debug.LogError("Could not save because render texture is null");
                return EditorGUIUtility.whiteTexture;
            }
            else
            {
                Texture2D tex = new Texture2D((int)Resolution.x, (int)Resolution.y, TextureFormat.ARGB32, false);
                RenderTexture.active = rTex;
                tex.ReadPixels(new Rect(0, 0, rTex.width, rTex.height), 0, 0);
                tex.alphaIsTransparency = true;
                tex.Apply();
                return tex;
            }
        }
    }

}

namespace JUTPS.CustomEditors
{
    [CustomEditor(typeof(JUIconGenerator))]
    public class JUIconGeneratorEditor : Editor
    {
        JUIconGenerator targeteditor;
        private RenderTexture renderTexture;
        private Texture Tex;
        private void OnEnable()
        {
            JUIconGenerator tg = (JUIconGenerator)target;
            targeteditor = tg;

            GetDependencies();
        }

        private void OnDestroy()
        {
            DestroyDependencies();
        }
        private void OnDisable()
        {
            if (targeteditor.IsRenderingSolidColor() == false) targeteditor.SetRenderersMaterialsToOriginalColors();
        }

        public void GetDependencies()
        {
            renderTexture = new RenderTexture((int)targeteditor.Resolution.x, (int)targeteditor.Resolution.y, (int)RenderTextureFormat.ARGB32);

            if (targeteditor.cam != null) return;

            targeteditor.StartCameraRenderingModes();

            targeteditor.cam = new GameObject("IconGeneratorCameraPreview").AddComponent<Camera>();
            targeteditor.cam.transform.position = targeteditor.transform.position + targeteditor.transform.right * 5;
            targeteditor.cam.transform.rotation = targeteditor.CameraRotation;

            targeteditor.cam.clearFlags = CameraClearFlags.Color;
            targeteditor.cam.backgroundColor = Color.clear;
            targeteditor.cam.targetTexture = renderTexture;
            if (targeteditor.ViewLayer == LayerMask.GetMask("Nothing"))
                targeteditor.ViewLayer = LayerMask.GetMask("Default");
            targeteditor.cam.cullingMask = targeteditor.ViewLayer;

            targeteditor.cam.fieldOfView = targeteditor.FieldOfView;
            targeteditor.cam.farClipPlane = targeteditor.Distance;
        }

        private static Color oldColor;
        void UpdateCameraRenderingModes()
        {
            // >>> CAMERA RENDERING

            //Camera Perspective
            if (targeteditor.Projection == JUIconGenerator.CameraViewMode.Perspective)
                targeteditor.cam.orthographic = false;
            else
                targeteditor.cam.orthographic = true; targeteditor.cam.orthographicSize = targeteditor.Distance;

            //Camera Vertex Lit
            if (targeteditor.RenderType == JUIconGenerator.RenderModes.VertexLit)
            {
                targeteditor.cam.renderingPath = RenderingPath.VertexLit;
            }
            else
            {
                targeteditor.cam.renderingPath = RenderingPath.UsePlayerSettings;
            }

            //Solid Color Rendering


            //Update Color
            if (targeteditor.RenderType == JUIconGenerator.RenderModes.ColorVector && targeteditor.VectorColor != oldColor)
            {
                targeteditor.SetSolidColor(targeteditor.VectorColor);
                targeteditor.SetRenderersMaterialsToSolidColor();
                oldColor = targeteditor.VectorColor;
                //Debug.Log("Rendering Solid Color");
            }
            else
            {
                oldColor = Color.clear;
            }
            if (targeteditor.RenderType != JUIconGenerator.RenderModes.ColorVector && targeteditor.IsRenderingSolidColor())
            {
                targeteditor.SetRenderersMaterialsToOriginalColors();
                //Debug.Log("Rendering Original Materials");
            }

            ///Debug.Log("IS RENDERING SOLID COLOR: " + targeteditor.IsRenderingSolidColor());
        }
        void UpdateRenderTextureResolution()
        {
            renderTexture = new RenderTexture((int)targeteditor.Resolution.x, (int)targeteditor.Resolution.y, (int)RenderTextureFormat.ARGB32);
        }
        public void DestroyDependencies()
        {
            targeteditor.ClearAllTemporaryReferences();
        }

        public void OnSceneGUI()
        {
            if (targeteditor == null) return;


            if (targeteditor.cam != null)
            {
                //Render a render texture
                targeteditor.cam.cullingMask = targeteditor.ViewLayer;
                targeteditor.cam.targetTexture = renderTexture;
                targeteditor.cam.Render();
                targeteditor.cam.targetTexture = null;
                targeteditor.currentrendertexture = renderTexture;

                //Apply camera settings
                targeteditor.cam.fieldOfView = targeteditor.FieldOfView;
                targeteditor.cam.farClipPlane = targeteditor.RenderDistance;
                targeteditor.cam.nearClipPlane = 0.01f;
                if (targeteditor.Projection == JUIconGenerator.CameraViewMode.Perspective)
                    targeteditor.cam.orthographic = false;
                else
                    targeteditor.cam.orthographic = true; targeteditor.cam.orthographicSize = targeteditor.Distance;


                //Camera positioning
                targeteditor.cam.transform.position = targeteditor.transform.position - targeteditor.cam.transform.forward * targeteditor.Distance + targeteditor.CenterOffset;

                //Camera rotation

                //targeteditor.cam.transform.rotation = Handles.DoRotationHandle(targeteditor.cam.transform.rotation, targeteditor.transform.position);
                //targeteditor.transform.rotation = Handles.Disc(targeteditor.transform.rotation, targeteditor.transform.position, new Vector3(0, 1, 0), 0.5f, true, 0);
                Handles.color = Color.red;
                targeteditor.cam.transform.rotation = Handles.Disc(targeteditor.cam.transform.rotation, targeteditor.transform.position + targeteditor.CenterOffset, targeteditor.cam.transform.up, 1, true, 5);
                Handles.color = Color.green;
                targeteditor.cam.transform.rotation = Handles.Disc(targeteditor.cam.transform.rotation, targeteditor.transform.position + targeteditor.CenterOffset, targeteditor.cam.transform.right, 1, true, 5f);


                targeteditor.CameraRotation = targeteditor.cam.transform.rotation;

                if (targeteditor.LockZAxisRotation)
                {
                    Vector3 rot = targeteditor.cam.transform.eulerAngles;
                    rot.z = 0; targeteditor.cam.transform.eulerAngles = rot;
                }

                UpdateCameraRenderingModes();
            }
        }
        //Exclude script field
        private static readonly string[] _dontIncludeMe = new string[] { "m_Script" };
        public override void OnInspectorGUI()
        {
            JUIconGenerator tg = (JUIconGenerator)target;

            JUTPSEditor.CustomEditorUtilities.JUTPSTitle("Icon Generator for JU TPS");

            serializedObject.Update();

            DrawPropertiesExcluding(serializedObject, _dontIncludeMe);

            GUILayout.Space(10);

            //PREVIEW
            {
                GUILayout.Label("Preview", JUTPSEditor.CustomEditorStyles.Header(), GUILayout.Width((int)targeteditor.Resolution.x));

                Rect PrevRect = EditorGUILayout.GetControlRect(GUILayout.Width((int)targeteditor.Resolution.x), GUILayout.Height((int)targeteditor.Resolution.y));
                Tex = renderTexture;
                EditorGUI.DrawTextureTransparent(PrevRect, Tex);
            }

            GUILayout.Space(10);

            //Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Update Resolution", GUILayout.Width((int)targeteditor.Resolution.x / 2)))
            {
                UpdateRenderTextureResolution();
            }
            if (GUILayout.Button("Reset View", GUILayout.Width((int)targeteditor.Resolution.x / 2)))
            {
                tg.CameraRotation = Quaternion.identity;
                tg.cam.transform.rotation = Quaternion.identity;
            }
            GUILayout.EndHorizontal();
            if (GUILayout.Button("Save icon to Texture", GUILayout.Width((int)targeteditor.Resolution.x)))
            {
                targeteditor.SaveTexture();
            }

            serializedObject.ApplyModifiedProperties();

        }
    }
}

#endif