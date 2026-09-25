using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Turns the Flooded Grounds trees and bushes into mobile-ready LOD prefabs and plants
/// them on the GameScene terrain.
///
/// The pack prefabs are left untouched. Everything generated lives in Assets/Forest and
/// can be rebuilt at any time from Tools > Last Stand > Build Forest - prefab GUIDs are
/// kept on rebuild, so the terrain keeps pointing at them.
///
/// Where the cost goes on mobile: the pack trees are 3.8k-26k triangles with hundreds of
/// alpha-tested leaf cards each. Every LOD here cuts the bark with a quadric simplifier
/// and thins the leaf cards (scaling up the survivors so the crown keeps its volume),
/// which removes both triangles and alpha-test overdraw. Only LOD0 casts shadows.
/// </summary>
public static class ForestBuilder
{
    private const string PackDir = "Assets/Other Packs/World Environment Packs/Flooded_Grounds/Prefabs/Nature/";
    private const string OutDir = "Assets/Forest";
    private const string MeshDir = OutDir + "/Meshes";
    private const string TerrainDataPath = OutDir + "/GameScene_TerrainData.asset";

    private struct Lod
    {
        public int barkTris;        // bark triangle budget
        public float leafKeep;      // fraction of leaf cards kept
        public float screenHeight;  // LODGroup transition height
        public bool shadows;

        public Lod(int barkTris, float leafKeep, float screenHeight, bool shadows)
        {
            this.barkTris = barkTris;
            this.leafKeep = leafKeep;
            this.screenHeight = screenHeight;
            this.shadows = shadows;
        }
    }

    private class Plant
    {
        public string source;
        public string name;
        public float scale = 1f;     // baked into the mesh
        public float weight;         // how often it is picked
        public bool isTree;
        public Lod[] lods;

        // Shrubs only: a dome of the source tree's leaf cards.
        public int cards;
        public float radius, height, cardSize;
    }

    // Scales bring every tree to roughly 18-19 m, next to a ~1.8 m character.
    // With lodBias 1 (the Mobile quality level) a ~19 m tree shows LOD0 within ~27 m,
    // LOD1 to ~70 m and LOD2 beyond that until ForestRenderer's draw distance.
    private static readonly Plant[] Trees =
    {
        new Plant { source = "Trees/TreeCreator_Tall_A", name = "Forest_Tree_Tall", scale = 0.40f, weight = 0.40f, isTree = true,
            lods = new[] { new Lod(900, 0.8f, 0.5f, true), new Lod(380, 0.4f, 0.19f, false), new Lod(100, 0.15f, 0.06f, false) } },
        new Plant { source = "Trees/TreeCreator_Crinkly_A", name = "Forest_Tree_Crinkly", scale = 0.70f, weight = 0.35f, isTree = true,
            lods = new[] { new Lod(1100, 0.2f, 0.5f, true), new Lod(420, 0.09f, 0.19f, false), new Lod(100, 0.03f, 0.06f, false) } },
        new Plant { source = "Trees/TreeCreator_Crinkly_B", name = "Forest_Tree_Wide", scale = 0.42f, weight = 0.25f, isTree = true,
            lods = new[] { new Lod(1200, 0.14f, 0.5f, true), new Lod(450, 0.06f, 0.19f, false), new Lod(110, 0.02f, 0.06f, false) } },
    };

    // The pack's DecoBush meshes are trimmed garden hedges and look like green boxes in
    // a forest, so the undergrowth is built from the trees' own leaf cards instead:
    // ~400-500 triangles each up close and under half that further out, same leaf
    // material, no collider, no shadow, culled at ~50 m.
    private static readonly Lod[] ShrubLods = { new Lod(0, 1f, 0.1f, false), new Lod(0, 0.4f, 0.04f, false) };

    private static readonly Plant[] Bushes =
    {
        new Plant { source = "Trees/TreeCreator_Crinkly_A", name = "Forest_Shrub_Round", weight = 0.5f,
            cards = 110, radius = 1.2f, height = 1.4f, cardSize = 1.2f, lods = ShrubLods },
        new Plant { source = "Trees/TreeCreator_Crinkly_B", name = "Forest_Shrub_Wide", weight = 0.3f,
            cards = 130, radius = 1.8f, height = 1.3f, cardSize = 1.3f, lods = ShrubLods },
        new Plant { source = "Trees/TreeCreator_Tall_A", name = "Forest_Shrub_Tall", weight = 0.2f,
            cards = 100, radius = 0.9f, height = 1.9f, cardSize = 1.1f, lods = ShrubLods },
    };

    // Planting. The play area is the safe zone: centre (48, 15), start radius 450.
    private static readonly Vector2 ClearingCentre = new Vector2(48f, 15f);
    private const float ClearingRadius = 45f;      // spawn ring sits ~30 m out
    private const float ClearingFade = 35f;        // density ramps back up over this
    private const float TreeSpacing = 9f;
    private const float BushSpacing = 11f;
    private const float MaxSlope = 32f;
    private const float TreeDistance = 170f;       // GameScene fog hides the cut-off
    private const int Seed = 7331;
    private const float NoiseOffset = 517.3f;   // shared, so bushes can follow the groves

    [MenuItem("Tools/Last Stand/Build Forest")]
    public static void BuildForest()
    {
        Terrain terrain = FindGameSceneTerrain();
        if (terrain == null)
        {
            EditorUtility.DisplayDialog("Build Forest", "Open GameScene first (it can be loaded additively).", "OK");
            return;
        }

        EnsureFolder(OutDir);
        EnsureFolder(MeshDir);

        var treePrefabs = new List<GameObject>();
        var bushPrefabs = new List<GameObject>();

        foreach (Plant p in Trees)
            treePrefabs.Add(BuildPrefab(p));
        foreach (Plant p in Bushes)
            bushPrefabs.Add(BuildPrefab(p));

        PlantTerrain(terrain, treePrefabs, bushPrefabs);
    }

    private static Terrain FindGameSceneTerrain()
    {
        foreach (Terrain t in Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.gameObject.scene.name == "GameScene")
                return t;
        }

        return null;
    }

    // ------------------------------------------------------------------ prefabs

    private static GameObject BuildPrefab(Plant plant)
    {
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(PackDir + plant.source + ".prefab");
        var srcFilter = source.GetComponent<MeshFilter>();
        var srcRenderer = source.GetComponent<MeshRenderer>();
        Mesh src = srcFilter.sharedMesh;

        // Trees stand on their origin, which is where the terrain plants a prefab.
        Vector3[] basePositions = src.vertices;
        for (int i = 0; i < basePositions.Length; i++)
            basePositions[i] *= plant.scale;

        Material[] materials = srcRenderer.sharedMaterials;
        Vector3[] normals = null;
        int[] shrubTris = null;

        if (!plant.isTree)
        {
            normals = src.normals;
            shrubTris = ArrangeShrub(plant, basePositions, normals, src.GetTriangles(1));
            materials = new[] { materials[1] };
        }

        var root = new GameObject(plant.name);
        var lods = new LOD[plant.lods.Length];

        for (int l = 0; l < plant.lods.Length; l++)
        {
            Lod spec = plant.lods[l];
            Vector3[] positions = (Vector3[])basePositions.Clone();
            var subMeshes = new List<int[]>();

            if (plant.isTree)
            {
                // Submesh 0 is bark, 1 is leaves - same order as the materials.
                int[] bark = MeshSimplifier.Simplify(positions, src.GetTriangles(0), spec.barkTris);
                subMeshes.Add(KeepLargestPieces(positions, bark, spec.barkTris));
                subMeshes.Add(ThinLeafCards(positions, src.GetTriangles(1), spec.leafKeep, Seed + l));
            }
            else
            {
                subMeshes.Add(ThinLeafCards(positions, shrubTris, spec.leafKeep, Seed + l));
            }

            // A rebuild writes into the existing mesh asset instead of recreating it, so
            // its references (and the prefab's) never break.
            string meshName = plant.name + "_LOD" + l;
            string meshPath = MeshDir + "/" + meshName + ".asset";
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);
            bool isNew = mesh == null;

            if (isNew)
                mesh = new Mesh { name = meshName };

            FillMesh(mesh, src, positions, normals, subMeshes);

            if (isNew)
                AssetDatabase.CreateAsset(mesh, meshPath);
            else
                EditorUtility.SetDirty(mesh);

            var child = new GameObject("LOD" + l);
            child.transform.SetParent(root.transform, false);
            child.AddComponent<MeshFilter>().sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = materials;
            renderer.shadowCastingMode = spec.shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            renderer.motionVectorGenerationMode = MotionVectorGenerationMode.Camera;

            lods[l] = new LOD(spec.screenHeight, new Renderer[] { renderer });
        }

        var group = root.AddComponent<LODGroup>();
        group.fadeMode = LODFadeMode.None;
        group.SetLODs(lods);
        group.RecalculateBounds();

        // The terrain builds tree colliders from the prefab root. Trees keep their trunk
        // capsule so they block movement, bullets and line of sight; bushes get none.
        if (plant.isTree && source.TryGetComponent(out CapsuleCollider srcCapsule))
        {
            var capsule = root.AddComponent<CapsuleCollider>();
            capsule.direction = srcCapsule.direction;
            capsule.radius = srcCapsule.radius * plant.scale;
            capsule.height = srcCapsule.height * plant.scale;
            capsule.center = srcCapsule.center * plant.scale;
        }

        string prefabPath = OutDir + "/" + plant.name + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        Debug.Log($"[ForestBuilder] {plant.name}: {TriangleReport(prefab)} (source {TriangleCount(src)})");
        return prefab;
    }

    /// <summary>
    /// Keeps a seeded random fraction of the leaf cards and scales each survivor about
    /// its own centre, so a thinner crown still reads as the same size from afar.
    /// A card is one connected piece of the leaf submesh.
    /// </summary>
    private static int[] ThinLeafCards(Vector3[] positions, int[] leafTris, float keep, int seed)
    {
        if (keep >= 1f)
            return leafTris;

        List<List<int>> cards = ConnectedPieces(leafTris);
        var random = new System.Random(seed);
        float grow = Mathf.Min(1.8f, Mathf.Sqrt(1f / Mathf.Max(0.01f, keep)));

        var result = new List<int>();
        var scaled = new HashSet<int>();

        foreach (List<int> card in cards)
        {
            if (random.NextDouble() > keep)
                continue;

            Vector3 centre = Vector3.zero;
            var verts = new HashSet<int>();
            foreach (int t in card)
            {
                for (int k = 0; k < 3; k++)
                    verts.Add(leafTris[t * 3 + k]);
            }

            foreach (int v in verts)
                centre += positions[v];
            centre /= verts.Count;

            foreach (int v in verts)
            {
                if (scaled.Add(v))
                    positions[v] = centre + (positions[v] - centre) * grow;
            }

            foreach (int t in card)
            {
                result.Add(leafTris[t * 3]);
                result.Add(leafTris[t * 3 + 1]);
                result.Add(leafTris[t * 3 + 2]);
            }
        }

        return result.ToArray();
    }

    /// <summary>
    /// The simplifier cannot touch a branch's UV seam or open ends, so bark made of many
    /// small branches stalls well above budget. What is left over is dropped here,
    /// smallest surface first: those are the twigs buried inside the leaf crown. Child
    /// branches are smaller than their parents, so this prunes from the tips inwards.
    /// </summary>
    private static int[] KeepLargestPieces(Vector3[] positions, int[] tris, int budget)
    {
        if (tris.Length / 3 <= budget)
            return tris;

        List<List<int>> pieces = ConnectedPieces(tris);
        var areas = new Dictionary<List<int>, float>();

        foreach (List<int> piece in pieces)
        {
            float area = 0f;
            foreach (int t in piece)
            {
                Vector3 a = positions[tris[t * 3]], b = positions[tris[t * 3 + 1]], c = positions[tris[t * 3 + 2]];
                area += Vector3.Cross(b - a, c - a).magnitude;
            }

            areas[piece] = area;
        }

        pieces.Sort((x, y) => areas[y].CompareTo(areas[x]));

        var result = new List<int>();
        foreach (List<int> piece in pieces)
        {
            // The trunk is always kept, even when it alone is over budget.
            // Stop rather than skip, so a twig never survives the branch it grows on.
            if (result.Count > 0 && result.Count / 3 + piece.Count > budget)
                break;

            foreach (int t in piece)
            {
                result.Add(tris[t * 3]);
                result.Add(tris[t * 3 + 1]);
                result.Add(tris[t * 3 + 2]);
            }
        }

        return result.ToArray();
    }

    /// <summary>Groups triangles into pieces that share vertices (union-find).</summary>
    private static List<List<int>> ConnectedPieces(int[] tris)
    {
        var parent = new Dictionary<int, int>();

        int Find(int v)
        {
            if (!parent.TryGetValue(v, out int p)) { parent[v] = v; return v; }
            while (p != v) { int gp = parent[p]; parent[v] = gp; v = p; p = gp; }
            return v;
        }

        for (int t = 0; t < tris.Length / 3; t++)
        {
            int a = Find(tris[t * 3]);
            parent[Find(tris[t * 3 + 1])] = a;
            parent[Find(tris[t * 3 + 2])] = a;
        }

        var pieces = new Dictionary<int, List<int>>();
        for (int t = 0; t < tris.Length / 3; t++)
        {
            int r = Find(tris[t * 3]);
            if (!pieces.TryGetValue(r, out List<int> list))
                pieces[r] = list = new List<int>();
            list.Add(t);
        }

        return new List<List<int>>(pieces.Values);
    }

    /// <summary>
    /// Scatters leaf cards from a tree crown over a low dome. Each card is resized to
    /// <see cref="Plant.cardSize"/> and spun randomly, and its normals point away from the
    /// dome's centre so the clump shades as one soft volume instead of flat cards.
    /// Moves the chosen cards in <paramref name="positions"/> and returns their triangles.
    /// </summary>
    private static int[] ArrangeShrub(Plant plant, Vector3[] positions, Vector3[] normals, int[] leafTris)
    {
        List<List<int>> cards = ConnectedPieces(leafTris);
        var random = new System.Random(Seed + plant.cards);
        Vector3 centre = new Vector3(0f, plant.height * 0.35f, 0f);
        var result = new List<int>();

        int count = Mathf.Min(plant.cards, cards.Count);
        for (int i = 0; i < count; i++)
        {
            // Partial shuffle, so no card is picked twice.
            int pick = i + random.Next(cards.Count - i);
            (cards[i], cards[pick]) = (cards[pick], cards[i]);
            List<int> card = cards[i];

            var verts = new HashSet<int>();
            foreach (int t in card)
            {
                for (int k = 0; k < 3; k++)
                    verts.Add(leafTris[t * 3 + k]);
            }

            Vector3 cardCentre = Vector3.zero;
            foreach (int v in verts) cardCentre += positions[v];
            cardCentre /= verts.Count;

            float extent = 0.001f;
            foreach (int v in verts) extent = Mathf.Max(extent, (positions[v] - cardCentre).magnitude);

            // A random direction over the upper hemisphere, placed in the outer shell of
            // the dome: the inside of a bush is never seen.
            Vector3 dir;
            do
            {
                dir = new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble(), (float)random.NextDouble() * 2f - 1f);
            }
            while (dir.sqrMagnitude > 1f || dir.sqrMagnitude < 0.01f);

            dir.Normalize();
            float depth = Mathf.Lerp(0.45f, 1f, (float)random.NextDouble());
            Vector3 spot = new Vector3(dir.x * plant.radius, 0.1f + dir.y * (plant.height - 0.4f), dir.z * plant.radius) * depth;

            // Face the card outwards so it shows its leaves, not its edge, from any side
            // the bush is seen; tilt and roll a little so the shell does not look tiled.
            int t0 = card[0];
            Vector3 p0 = positions[leafTris[t0 * 3]], p1 = positions[leafTris[t0 * 3 + 1]], p2 = positions[leafTris[t0 * 3 + 2]];
            Vector3 cardNormal = Vector3.Cross(p1 - p0, p2 - p0).normalized;
            Vector3 facing = Vector3.Slerp(dir, Random3(random), 0.3f).normalized;
            Quaternion spin = Quaternion.AngleAxis((float)random.NextDouble() * 360f, facing) *
                              Quaternion.FromToRotation(cardNormal, facing);
            float resize = plant.cardSize * 0.5f / extent;

            foreach (int v in verts)
            {
                positions[v] = spot + spin * ((positions[v] - cardCentre) * resize);
                normals[v] = (positions[v] - centre).normalized;
            }

            foreach (int t in card)
            {
                result.Add(leafTris[t * 3]);
                result.Add(leafTris[t * 3 + 1]);
                result.Add(leafTris[t * 3 + 2]);
            }
        }

        return result.ToArray();
    }

    private static Vector3 Random3(System.Random random)
    {
        return new Vector3((float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f, (float)random.NextDouble() * 2f - 1f);
    }

    /// <summary>
    /// Fills <paramref name="mesh"/> with only the vertices the given submeshes use. Normals come from
    /// <paramref name="normalsOverride"/> when given, otherwise from the source.
    /// </summary>
    private static void FillMesh(Mesh mesh, Mesh src, Vector3[] positions, Vector3[] normalsOverride, List<int[]> subMeshes)
    {
        var normals = new List<Vector3>();
        if (normalsOverride != null) normals.AddRange(normalsOverride);
        else src.GetNormals(normals);
        var tangents = new List<Vector4>(); src.GetTangents(tangents);
        var colors = new List<Color>(); src.GetColors(colors);
        var uv0 = new List<Vector4>(); src.GetUVs(0, uv0);
        var uv1 = new List<Vector4>(); src.GetUVs(1, uv1);

        var remap = new Dictionary<int, int>();
        var outPos = new List<Vector3>();
        var outNormals = new List<Vector3>();
        var outTangents = new List<Vector4>();
        var outColors = new List<Color>();
        var outUv0 = new List<Vector4>();
        var outUv1 = new List<Vector4>();
        var outSubs = new List<int[]>();

        foreach (int[] indices in subMeshes)
        {
            var mapped = new int[indices.Length];
            for (int i = 0; i < indices.Length; i++)
            {
                int v = indices[i];
                if (!remap.TryGetValue(v, out int n))
                {
                    n = outPos.Count;
                    remap[v] = n;
                    outPos.Add(positions[v]);
                    if (normals.Count > 0) outNormals.Add(normals[v]);
                    if (tangents.Count > 0) outTangents.Add(tangents[v]);
                    if (colors.Count > 0) outColors.Add(colors[v]);
                    if (uv0.Count > 0) outUv0.Add(uv0[v]);
                    if (uv1.Count > 0) outUv1.Add(uv1[v]);
                }

                mapped[i] = n;
            }

            outSubs.Add(mapped);
        }

        mesh.Clear();
        mesh.SetVertices(outPos);
        if (outNormals.Count > 0) mesh.SetNormals(outNormals);
        if (outTangents.Count > 0) mesh.SetTangents(outTangents);
        if (outColors.Count > 0) mesh.SetColors(outColors);
        if (outUv0.Count > 0) mesh.SetUVs(0, outUv0);
        if (outUv1.Count > 0) mesh.SetUVs(1, outUv1);

        mesh.subMeshCount = outSubs.Count;
        for (int s = 0; s < outSubs.Count; s++)
            mesh.SetTriangles(outSubs[s], s);

        mesh.RecalculateBounds();
        mesh.Optimize();
        mesh.UploadMeshData(false);
    }

    // ------------------------------------------------------------------ planting

    private static void PlantTerrain(Terrain terrain, List<GameObject> trees, List<GameObject> bushes)
    {
        // Work on a copy so the pack's terrain (and any other scene using it) is untouched.
        TerrainData data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        if (data == null)
        {
            string sourcePath = AssetDatabase.GetAssetPath(terrain.terrainData);
            AssetDatabase.CopyAsset(sourcePath, TerrainDataPath);
            data = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        }

        Undo.RecordObject(terrain, "Build Forest");
        terrain.terrainData = data;

        if (terrain.TryGetComponent(out TerrainCollider collider))
        {
            Undo.RecordObject(collider, "Build Forest");
            collider.terrainData = data;
        }

        var prototypes = new List<TreePrototype>();
        foreach (GameObject prefab in trees)
            prototypes.Add(new TreePrototype { prefab = prefab });
        foreach (GameObject prefab in bushes)
            prototypes.Add(new TreePrototype { prefab = prefab });

        data.treeInstances = new TreeInstance[0];
        data.treePrototypes = prototypes.ToArray();
        data.RefreshPrototypes();

        var random = new System.Random(Seed);
        var instances = new List<TreeInstance>();

        float[] treeWeights = new float[Trees.Length];
        for (int i = 0; i < Trees.Length; i++) treeWeights[i] = Trees[i].weight;
        float[] bushWeights = new float[Bushes.Length];
        for (int i = 0; i < Bushes.Length; i++) bushWeights[i] = Bushes[i].weight;

        Scatter(terrain, data, random, TreeSpacing, instances, 0, treeWeights, 0.85f, 1.2f,
                density => 0.1f + 0.8f * density);

        // Bushes crowd the forest edges and fill the gaps between groves.
        Scatter(terrain, data, random, BushSpacing, instances, trees.Count, bushWeights, 0.8f, 1.25f,
                density => 0.25f + 0.5f * (1f - Mathf.Abs(density - 0.5f) * 2f));

        data.SetTreeInstances(instances.ToArray(), true);

        terrain.treeDistance = TreeDistance;
        terrain.drawInstanced = true;
        terrain.Flush();

        // Draws the trees with GPU instancing at runtime; see ForestRenderer.
        if (!terrain.TryGetComponent(out ForestRenderer forest))
            forest = Undo.AddComponent<ForestRenderer>(terrain.gameObject);

        forest.drawDistance = TreeDistance;
        EditorUtility.SetDirty(forest);

        EditorUtility.SetDirty(data);
        EditorUtility.SetDirty(terrain);
        EditorSceneManager.MarkSceneDirty(terrain.gameObject.scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ForestBuilder] Planted {instances.Count} instances on {terrain.name}.");
    }

    /// <summary>
    /// Jittered grid over the terrain. A two-octave noise field decides where groves
    /// grow, steep ground and the spawn clearing are skipped.
    /// </summary>
    private static void Scatter(Terrain terrain, TerrainData data, System.Random random, float spacing,
                                List<TreeInstance> into, int prototypeOffset, float[] weights,
                                float minScale, float maxScale, System.Func<float, float> chance)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = data.size;
        float margin = 8f;

        // Layer 0 is the moss ground; the dirt paths and asphalt pads stay clear.
        float[,,] splat = data.GetAlphamaps(0, 0, data.alphamapWidth, data.alphamapHeight);

        for (float z = margin; z < size.z - margin; z += spacing)
        {
            for (float x = margin; x < size.x - margin; x += spacing)
            {
                float px = x + ((float)random.NextDouble() - 0.5f) * spacing * 0.8f;
                float pz = z + ((float)random.NextDouble() - 0.5f) * spacing * 0.8f;
                float nx = px / size.x, nz = pz / size.z;

                float worldX = origin.x + px, worldZ = origin.z + pz;

                // Groves: broad patches with finer breakup.
                float density =
                    0.7f * Mathf.PerlinNoise(NoiseOffset + worldX / 140f, NoiseOffset + worldZ / 140f) +
                    0.3f * Mathf.PerlinNoise(NoiseOffset + worldX / 40f, NoiseOffset + worldZ / 40f);
                density = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.32f, 0.68f, density));

                float fromClearing = Vector2.Distance(new Vector2(worldX, worldZ), ClearingCentre);
                if (fromClearing < ClearingRadius)
                    continue;

                float p = chance(density) * Mathf.Clamp01((fromClearing - ClearingRadius) / ClearingFade);

                if (random.NextDouble() > p)
                    continue;

                if (data.GetSteepness(nx, nz) > MaxSlope)
                    continue;

                int ax = Mathf.Min((int)(nx * data.alphamapWidth), data.alphamapWidth - 1);
                int az = Mathf.Min((int)(nz * data.alphamapHeight), data.alphamapHeight - 1);
                if (splat[az, ax, 0] < 0.6f)
                    continue;

                float scale = Mathf.Lerp(minScale, maxScale, (float)random.NextDouble());

                into.Add(new TreeInstance
                {
                    position = new Vector3(nx, data.GetInterpolatedHeight(nx, nz) / size.y, nz),
                    prototypeIndex = prototypeOffset + Pick(weights, random),
                    widthScale = scale,
                    heightScale = scale * Mathf.Lerp(0.92f, 1.08f, (float)random.NextDouble()),
                    rotation = (float)(random.NextDouble() * Mathf.PI * 2f),
                    color = Color.white,
                    lightmapColor = Color.white
                });
            }
        }
    }

    private static int Pick(float[] weights, System.Random random)
    {
        float total = 0f;
        foreach (float w in weights) total += w;

        float roll = (float)random.NextDouble() * total;
        for (int i = 0; i < weights.Length; i++)
        {
            roll -= weights[i];
            if (roll <= 0f)
                return i;
        }

        return weights.Length - 1;
    }

    // ------------------------------------------------------------------ helpers

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int slash = path.LastIndexOf('/');
        AssetDatabase.CreateFolder(path.Substring(0, slash), path.Substring(slash + 1));
    }

    private static int TriangleCount(Mesh mesh)
    {
        int count = 0;
        for (int s = 0; s < mesh.subMeshCount; s++)
            count += (int)mesh.GetIndexCount(s) / 3;
        return count;
    }

    private static string TriangleReport(GameObject prefab)
    {
        var parts = new List<string>();
        foreach (LOD lod in prefab.GetComponent<LODGroup>().GetLODs())
        {
            var mesh = lod.renderers[0].GetComponent<MeshFilter>().sharedMesh;
            parts.Add($"{mesh.name.Substring(mesh.name.LastIndexOf('_') + 1)} {TriangleCount(mesh)}");
        }

        return string.Join(", ", parts);
    }
}
