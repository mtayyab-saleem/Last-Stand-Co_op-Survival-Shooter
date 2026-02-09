using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
namespace JUTPS.DestructibleSystem
{
    [AddComponentMenu("JU TPS/Tools/Fracture Tool")]
    public class FractureTool : MonoBehaviour
    {
        private bool edgeSet = false;
        private Vector3 edgeVertex = Vector3.zero;
        private Vector2 edgeUV = Vector2.zero;
        private Plane edgePlane = new Plane();


        [Range(2, 15)]
        [Tooltip("Number of fractures. Numbers far above 10 can crash your Unity")]
        public int Fractures = 2;
        [Range(0.0f, 0.4f)]
        [Tooltip("Minimum fracture size, this excludes very small invoices that are not needed")]
        public float MinFracturedSize = 0.2f;
        [SerializeField]
        [Tooltip("Reference of the new fractured GameObject generated")]
        private GameObject NewFracturedObject;


        public static string PathToSave;
        public void DestroyMesh()
        {

            var originalMesh = GetComponent<MeshFilter>().sharedMesh;
            originalMesh.RecalculateBounds();
            var parts = new List<PartMesh>();
            var subParts = new List<PartMesh>();
            if (NewFracturedObject == null)
            {
                NewFracturedObject = new GameObject(gameObject.name + " Fractured");
                NewFracturedObject.transform.position = transform.position;
            }
            else
            {
                DestroyImmediate(NewFracturedObject);
                NewFracturedObject = new GameObject(gameObject.name + " Fractured");
                NewFracturedObject.transform.position = transform.position;
            }
            var mainPart = new PartMesh()
            {
                UV = originalMesh.uv,
                Vertices = originalMesh.vertices,
                Normals = originalMesh.normals,
                Triangles = new int[originalMesh.subMeshCount][],
                Bounds = originalMesh.bounds
            };
            for (int i = 0; i < originalMesh.subMeshCount; i++)
                mainPart.Triangles[i] = originalMesh.GetTriangles(i);

            parts.Add(mainPart);

            for (var c = 0; c < Fractures; c++)
            {
                for (var i = 0; i < parts.Count; i++)
                {
                    var bounds = parts[i].Bounds;
                    bounds.Expand(0.15f);

                    var plane = new Plane(UnityEngine.Random.onUnitSphere, new Vector3(UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                                                                                       UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                                                                                       UnityEngine.Random.Range(bounds.min.z, bounds.max.z)));


                    subParts.Add(GenerateMesh(parts[i], plane, true));
                    subParts.Add(GenerateMesh(parts[i], plane, false));
                }
                parts = new List<PartMesh>(subParts);
                subParts.Clear();
            }

            for (var i = 0; i < parts.Count; i++)
            {
                parts[i].MakeGameobject(this);
            }
            DestroySmallFractures();
        }

        private PartMesh GenerateMesh(PartMesh original, Plane plane, bool left)
        {
            var partMesh = new PartMesh() { };
            var ray1 = new Ray();
            var ray2 = new Ray();


            for (var i = 0; i < original.Triangles.Length; i++)
            {
                var triangles = original.Triangles[i];
                edgeSet = false;

                for (var j = 0; j < triangles.Length; j = j + 3)
                {
                    var sideA = plane.GetSide(original.Vertices[triangles[j]]) == left;
                    var sideB = plane.GetSide(original.Vertices[triangles[j + 1]]) == left;
                    var sideC = plane.GetSide(original.Vertices[triangles[j + 2]]) == left;

                    var sideCount = (sideA ? 1 : 0) +
                                    (sideB ? 1 : 0) +
                                    (sideC ? 1 : 0);
                    if (sideCount == 0)
                    {
                        continue;
                    }
                    if (sideCount == 3)
                    {
                        partMesh.AddTriangle(i,
                                             original.Vertices[triangles[j]], original.Vertices[triangles[j + 1]], original.Vertices[triangles[j + 2]],
                                             original.Normals[triangles[j]], original.Normals[triangles[j + 1]], original.Normals[triangles[j + 2]],
                                             original.UV[triangles[j]], original.UV[triangles[j + 1]], original.UV[triangles[j + 2]]);
                        continue;
                    }

                    //cut points
                    var singleIndex = sideB == sideC ? 0 : sideA == sideC ? 1 : 2;

                    ray1.origin = original.Vertices[triangles[j + singleIndex]];
                    var dir1 = original.Vertices[triangles[j + ((singleIndex + 1) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                    ray1.direction = dir1;
                    plane.Raycast(ray1, out var enter1);
                    var lerp1 = enter1 / dir1.magnitude;

                    ray2.origin = original.Vertices[triangles[j + singleIndex]];
                    var dir2 = original.Vertices[triangles[j + ((singleIndex + 2) % 3)]] - original.Vertices[triangles[j + singleIndex]];
                    ray2.direction = dir2;
                    plane.Raycast(ray2, out var enter2);
                    var lerp2 = enter2 / dir2.magnitude;

                    //first vertex = ancor
                    AddEdge(i,
                            partMesh,
                            left ? plane.normal * -1f : plane.normal,
                            ray1.origin + ray1.direction.normalized * enter1,
                            ray2.origin + ray2.direction.normalized * enter2,
                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));

                    if (sideCount == 1)
                    {
                        partMesh.AddTriangle(i,
                                            original.Vertices[triangles[j + singleIndex]],
                                            ray1.origin + ray1.direction.normalized * enter1,
                                            ray2.origin + ray2.direction.normalized * enter2,
                                            original.Normals[triangles[j + singleIndex]],
                                            Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                            original.UV[triangles[j + singleIndex]],
                                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));

                        continue;
                    }

                    if (sideCount == 2)
                    {
                        partMesh.AddTriangle(i,
                                            ray1.origin + ray1.direction.normalized * enter1,
                                            original.Vertices[triangles[j + ((singleIndex + 1) % 3)]],
                                            original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                            Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            original.Normals[triangles[j + ((singleIndex + 1) % 3)]],
                                            original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            original.UV[triangles[j + ((singleIndex + 1) % 3)]],
                                            original.UV[triangles[j + ((singleIndex + 2) % 3)]]);
                        partMesh.AddTriangle(i,
                                            ray1.origin + ray1.direction.normalized * enter1,
                                            original.Vertices[triangles[j + ((singleIndex + 2) % 3)]],
                                            ray2.origin + ray2.direction.normalized * enter2,
                                            Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            original.Normals[triangles[j + ((singleIndex + 2) % 3)]],
                                            Vector3.Lerp(original.Normals[triangles[j + singleIndex]], original.Normals[triangles[j + ((singleIndex + 2) % 3)]], lerp2),
                                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 1) % 3)]], lerp1),
                                            original.UV[triangles[j + ((singleIndex + 2) % 3)]],
                                            Vector2.Lerp(original.UV[triangles[j + singleIndex]], original.UV[triangles[j + ((singleIndex + 2) % 3)]], lerp2));
                        continue;
                    }


                }
            }

            partMesh.FillArrays();

            return partMesh;
        }

        private void AddEdge(int subMesh, PartMesh partMesh, Vector3 normal, Vector3 vertex1, Vector3 vertex2, Vector2 uv1, Vector2 uv2)
        {
            if (!edgeSet)
            {
                edgeSet = true;
                edgeVertex = vertex1;
                edgeUV = uv1;
            }
            else
            {
                edgePlane.Set3Points(edgeVertex, vertex1, vertex2);

                partMesh.AddTriangle(subMesh,
                                    edgeVertex,
                                    edgePlane.GetSide(edgeVertex + normal) ? vertex1 : vertex2,
                                    edgePlane.GetSide(edgeVertex + normal) ? vertex2 : vertex1,
                                    normal,
                                    normal,
                                    normal,
                                    edgeUV,
                                    uv1,
                                    uv2);
            }
        }

        public class PartMesh
        {
            private List<Vector3> _Verticies = new List<Vector3>();
            private List<Vector3> _Normals = new List<Vector3>();
            private List<List<int>> _Triangles = new List<List<int>>();
            private List<Vector2> _UVs = new List<Vector2>();
            public Vector3[] Vertices;
            public Vector3[] Normals;
            public int[][] Triangles;
            public Vector2[] UV;
            public GameObject NewFracture;
            public Bounds Bounds = new Bounds();

            public PartMesh()
            {

            }

            public void AddTriangle(int submesh, Vector3 vert1, Vector3 vert2, Vector3 vert3, Vector3 normal1, Vector3 normal2, Vector3 normal3, Vector2 uv1, Vector2 uv2, Vector2 uv3)
            {
                if (_Triangles.Count - 1 < submesh)
                    _Triangles.Add(new List<int>());

                _Triangles[submesh].Add(_Verticies.Count);
                _Verticies.Add(vert1);
                _Triangles[submesh].Add(_Verticies.Count);
                _Verticies.Add(vert2);
                _Triangles[submesh].Add(_Verticies.Count);
                _Verticies.Add(vert3);
                _Normals.Add(normal1);
                _Normals.Add(normal2);
                _Normals.Add(normal3);
                _UVs.Add(uv1);
                _UVs.Add(uv2);
                _UVs.Add(uv3);

                Bounds.min = Vector3.Min(Bounds.min, vert1);
                Bounds.min = Vector3.Min(Bounds.min, vert2);
                Bounds.min = Vector3.Min(Bounds.min, vert3);
                Bounds.max = Vector3.Min(Bounds.max, vert1);
                Bounds.max = Vector3.Min(Bounds.max, vert2);
                Bounds.max = Vector3.Min(Bounds.max, vert3);
            }

            public void FillArrays()
            {
                Vertices = _Verticies.ToArray();
                Normals = _Normals.ToArray();
                UV = _UVs.ToArray();
                Triangles = new int[_Triangles.Count][];
                for (var i = 0; i < _Triangles.Count; i++)
                    Triangles[i] = _Triangles[i].ToArray();
            }

            public void MakeGameobject(FractureTool original)
            {
                NewFracture = new GameObject(original.name);
                NewFracture.transform.position = original.transform.position;
                NewFracture.transform.rotation = original.transform.rotation;
                NewFracture.transform.localScale = original.transform.localScale;
                NewFracture.transform.SetParent(original.NewFracturedObject.transform);

                var mesh = new Mesh();
                mesh.name = original.GetComponent<MeshFilter>().sharedMesh.name;

                mesh.vertices = Vertices;
                mesh.normals = Normals;
                mesh.uv = UV;
                for (var i = 0; i < Triangles.Length; i++)
                    mesh.SetTriangles(Triangles[i], i, true);
                Bounds = mesh.bounds;

                var renderer = NewFracture.AddComponent<MeshRenderer>();
                renderer.materials = original.GetComponent<MeshRenderer>().sharedMaterials;

                var filter = NewFracture.AddComponent<MeshFilter>();
                filter.mesh = mesh;

                var collider = NewFracture.AddComponent<MeshCollider>();
                collider.convex = true;

                var rigidbody = NewFracture.AddComponent<Rigidbody>();
            }

        }

        public void DestroySmallFractures()
        {
            int childs = NewFracturedObject.transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                var childfracture = NewFracturedObject.transform.GetChild(i).gameObject;
                childfracture.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();

                if (childfracture.GetComponent<MeshFilter>().sharedMesh.bounds.size.x < MinFracturedSize && childfracture.GetComponent<MeshFilter>().sharedMesh.bounds.size.y < MinFracturedSize && childfracture.GetComponent<MeshFilter>().sharedMesh.bounds.size.z < MinFracturedSize)
                {
                    DestroyImmediate(childfracture);
                    print("Destroyed a small fracture for optimization");
                }
            }
        }


#if UNITY_EDITOR
        public void SaveFracturedAssets()
        {
            GetPath();
            if (NewFracturedObject != null)
            {
                int childs = NewFracturedObject.transform.childCount;
                for (int i = childs - 1; i >= 0; i--)
                {
                    var childfracture = NewFracturedObject.transform.GetChild(i).gameObject;
                    SaveFractureMeshes(childfracture.GetComponent<MeshFilter>().sharedMesh, childfracture.name + "_fracture_" + i.ToString(), false, true, childfracture.name);
                }
            }
            else
            {
                Debug.LogError("There is no linked fractured game object. Click on ''Generate Fractured Object'' or link the fractured object and then click on ''Save Generated Meshes as asset''");
            }
        }
        public void DestroyAllChilds()
        {
            int childs = transform.childCount;
            for (int i = childs - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(transform.GetChild(i).gameObject);
            }
        }
        public static void SaveFractureMeshes(Mesh Fracture_mesh, string name, bool CreateNewInstance, bool MeshOptimization, string Path_Name)
        {
            if (System.IO.Directory.Exists(PathToSave + Path_Name + "_meshes_fractures/") == false)
            {
                System.IO.Directory.CreateDirectory(PathToSave + Path_Name + "_meshes_fractures/");
            }
            string path_to_save = PathToSave + Path_Name + "_meshes_fractures/" + name + ".asset";

            if (string.IsNullOrEmpty(path_to_save)) return;

            path_to_save = FileUtil.GetProjectRelativePath(path_to_save);

            Mesh meshToSave = (CreateNewInstance) ? Object.Instantiate(Fracture_mesh) as Mesh : Fracture_mesh;

            if (MeshOptimization)
            {
                MeshUtility.Optimize(meshToSave);
            }

            AssetDatabase.CreateAsset(meshToSave, path_to_save);
            AssetDatabase.SaveAssets();
            Application.OpenURL(path_to_save);
            print("Meshes saved at: " + path_to_save);
        }
        public void GetPath()
        {
            PathToSave = Application.dataPath + "/Julhiecio TPS Controller/Generated Fractures/";
            print("Path to save fractures: " + PathToSave);
        }
        [CustomEditor(typeof(FractureTool))]
        public class FractureToolEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                DrawDefaultInspector();

                FractureTool fracture_tool = (FractureTool)target;

                if (GUILayout.Button("Generate Fractured Object"))
                {
                    fracture_tool.DestroyMesh();
                }
                if (GUILayout.Button("Save Generated Meshes as asset"))
                {
                    fracture_tool.SaveFracturedAssets();
                }
            }
        }
#endif
    }

}