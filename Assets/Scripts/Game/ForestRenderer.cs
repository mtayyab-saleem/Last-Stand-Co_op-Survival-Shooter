using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Draws the terrain's trees and shrubs with GPU instancing.
///
/// The terrain draws every tree as a renderer of its own, and with the SRP Batcher on -
/// which switches GPU instancing off - a dense forest view came to ~800 draw calls, far
/// too many for a phone. This draws the same tree instances with about one call per
/// mesh, LOD and material, picking each tree's LOD the way its LODGroup would.
///
/// The terrain keeps the tree data and builds the tree colliders from it as before; only
/// its own tree drawing is switched off while this component runs. Grass details are
/// untouched. In the editor, outside play mode, the terrain still draws the trees.
/// </summary>
[RequireComponent(typeof(Terrain))]
[DisallowMultipleComponent]
public class ForestRenderer : MonoBehaviour
{
    [Tooltip("Trees further than this are not drawn. Keep it inside the scene fog so the cut-off never shows. Quality levels cannot override it, unlike the terrain's own tree distance.")]
    public float drawDistance = 170f;

    [Tooltip("Size of the culling cells. Smaller cells cull tighter but cost more to test.")]
    [SerializeField] private float cellSize = 32f;

    [Tooltip("Cells are grown by this much before frustum tests, so trees just behind the camera still cast their shadows into view.")]
    [SerializeField] private float shadowMargin = 15f;

    private class LodDraw
    {
        public Mesh mesh;
        public Material[] materials;
        public ShadowCastingMode shadows;
        public float screenHeight;
        public readonly List<Matrix4x4> matrices = new List<Matrix4x4>();
    }

    private struct Plant
    {
        public Matrix4x4 matrix;
        public Vector3 centre;
        public float size;
        public int prototype;
    }

    private class Cell
    {
        public Bounds bounds;
        public readonly List<Plant> plants = new List<Plant>();
    }

    private Terrain terrain;
    private float terrainTreeDistance;
    private bool tookOver;
    private LodDraw[][] prototypes;
    private readonly List<Cell> cells = new List<Cell>();
    private readonly Plane[] frustum = new Plane[6];
    private Bounds drawBounds;

    private void OnEnable()
    {
        terrain = GetComponent<Terrain>();

        if (!Build())
        {
            enabled = false;
            return;
        }

        // Zero stops the terrain drawing the trees without touching their colliders.
        terrainTreeDistance = terrain.treeDistance;
        terrain.treeDistance = 0f;
        tookOver = true;
    }

    private void OnDisable()
    {
        if (terrain != null && tookOver)
            terrain.treeDistance = terrainTreeDistance;

        tookOver = false;
    }

    private bool Build()
    {
        TerrainData data = terrain.terrainData;
        if (data == null)
            return false;

        TreePrototype[] source = data.treePrototypes;
        prototypes = new LodDraw[source.Length][];
        var groupSize = new float[source.Length];
        var groupCentre = new Vector3[source.Length];

        for (int i = 0; i < source.Length; i++)
        {
            LODGroup group = source[i].prefab != null ? source[i].prefab.GetComponent<LODGroup>() : null;

            // Anything that is not an LOD prefab is left to the terrain.
            if (group == null)
            {
                Debug.LogWarning($"[ForestRenderer] Tree prototype {i} has no LODGroup; leaving trees to the terrain.");
                prototypes = null;
                return false;
            }

            LOD[] lods = group.GetLODs();
            prototypes[i] = new LodDraw[lods.Length];
            groupSize[i] = group.size;
            groupCentre[i] = group.localReferencePoint;

            for (int l = 0; l < lods.Length; l++)
            {
                var renderer = (MeshRenderer)lods[l].renderers[0];
                prototypes[i][l] = new LodDraw
                {
                    mesh = renderer.GetComponent<MeshFilter>().sharedMesh,
                    materials = renderer.sharedMaterials,
                    shadows = renderer.shadowCastingMode,
                    screenHeight = lods[l].screenRelativeTransitionHeight
                };
            }
        }

        Vector3 origin = terrain.transform.position;
        int columns = Mathf.Max(1, Mathf.CeilToInt(data.size.x / cellSize));
        int rows = Mathf.Max(1, Mathf.CeilToInt(data.size.z / cellSize));
        var grid = new Cell[columns * rows];

        cells.Clear();
        drawBounds = new Bounds(origin + data.size * 0.5f, data.size);

        foreach (TreeInstance tree in data.treeInstances)
        {
            Vector3 position = Vector3.Scale(tree.position, data.size) + origin;
            Quaternion rotation = Quaternion.AngleAxis(tree.rotation * Mathf.Rad2Deg, Vector3.up);
            var scale = new Vector3(tree.widthScale, tree.heightScale, tree.widthScale);

            var plant = new Plant
            {
                matrix = Matrix4x4.TRS(position, rotation, scale),
                prototype = tree.prototypeIndex,
                size = groupSize[tree.prototypeIndex] * Mathf.Max(tree.widthScale, tree.heightScale)
            };
            plant.centre = plant.matrix.MultiplyPoint3x4(groupCentre[tree.prototypeIndex]);

            int cx = Mathf.Clamp((int)(tree.position.x * columns), 0, columns - 1);
            int cz = Mathf.Clamp((int)(tree.position.z * rows), 0, rows - 1);
            Cell cell = grid[cz * columns + cx];

            var plantBounds = new Bounds(plant.centre, Vector3.one * plant.size);

            if (cell == null)
            {
                cell = grid[cz * columns + cx] = new Cell { bounds = plantBounds };
                cells.Add(cell);
            }
            else
            {
                cell.bounds.Encapsulate(plantBounds);
            }

            cell.plants.Add(plant);
            drawBounds.Encapsulate(plantBounds);
        }

        foreach (Cell cell in cells)
            cell.bounds.Expand(shadowMargin);

        return true;
    }

    private void LateUpdate()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        for (int p = 0; p < prototypes.Length; p++)
        {
            for (int l = 0; l < prototypes[p].Length; l++)
                prototypes[p][l].matrices.Clear();
        }

        GeometryUtility.CalculateFrustumPlanes(cam, frustum);
        Vector3 eye = cam.transform.position;
        float maxDistance = drawDistance;
        float maxSqr = maxDistance * maxDistance;

        // Same measure LODGroup uses: object size over the screen height at that distance.
        float metric = QualitySettings.lodBias / (2f * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad));

        for (int c = 0; c < cells.Count; c++)
        {
            Cell cell = cells[c];

            if (cell.bounds.SqrDistance(eye) > maxSqr || !GeometryUtility.TestPlanesAABB(frustum, cell.bounds))
                continue;

            List<Plant> plants = cell.plants;
            for (int i = 0; i < plants.Count; i++)
            {
                Plant plant = plants[i];
                float distance = Vector3.Distance(eye, plant.centre);
                if (distance > maxDistance)
                    continue;

                float screenHeight = plant.size * metric / Mathf.Max(distance, 0.01f);
                LodDraw[] lods = prototypes[plant.prototype];

                // Below the last LOD's height the tree is culled, as with an LODGroup.
                for (int l = 0; l < lods.Length; l++)
                {
                    if (screenHeight >= lods[l].screenHeight)
                    {
                        lods[l].matrices.Add(plant.matrix);
                        break;
                    }
                }
            }
        }

        for (int p = 0; p < prototypes.Length; p++)
        {
            for (int l = 0; l < prototypes[p].Length; l++)
            {
                LodDraw lod = prototypes[p][l];
                if (lod.matrices.Count == 0)
                    continue;

                for (int s = 0; s < lod.materials.Length; s++)
                {
                    // Only the camera the selection was made for; any other camera would
                    // get this camera's culling and LODs.
                    var rp = new RenderParams(lod.materials[s])
                    {
                        camera = cam,
                        worldBounds = drawBounds,
                        shadowCastingMode = lod.shadows,
                        receiveShadows = true,
                        layer = gameObject.layer
                    };

                    Graphics.RenderMeshInstanced(rp, lod.mesh, s, lod.matrices);
                }
            }
        }
    }
}
