using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reduces the triangle count of one submesh with quadric error metrics
/// (Garland &amp; Heckbert), used to build lighter LODs of the vegetation.
///
/// Every step is a half-edge collapse: one vertex moves onto a neighbour that already
/// exists. No vertices are created, so normals, UVs and tangents stay valid without any
/// interpolation. Vertices on an open edge are locked - in index space a UV seam is an
/// open edge too, so seams and the open ends of branches keep their shape.
/// </summary>
public static class MeshSimplifier
{
    private struct Quadric
    {
        // Symmetric 4x4 matrix, upper triangle.
        public double a, b, c, d, e, f, g, h, i, j;

        public static Quadric FromPlane(double x, double y, double z, double w, double weight)
        {
            return new Quadric
            {
                a = x * x * weight, b = x * y * weight, c = x * z * weight, d = x * w * weight,
                e = y * y * weight, f = y * z * weight, g = y * w * weight,
                h = z * z * weight, i = z * w * weight,
                j = w * w * weight
            };
        }

        public void Add(Quadric q)
        {
            a += q.a; b += q.b; c += q.c; d += q.d; e += q.e;
            f += q.f; g += q.g; h += q.h; i += q.i; j += q.j;
        }

        public double Error(Vector3 p)
        {
            double x = p.x, y = p.y, z = p.z;
            return a * x * x + 2 * b * x * y + 2 * c * x * z + 2 * d * x
                 + e * y * y + 2 * f * y * z + 2 * g * y
                 + h * z * z + 2 * i * z
                 + j;
        }
    }

    private struct Candidate
    {
        public double cost;
        public int from, to;
        public int fromVersion, toVersion;
    }

    /// <summary>
    /// Returns a new index list for <paramref name="triangles"/> with at most about
    /// <paramref name="targetTriangles"/> triangles. Stops early rather than damage the
    /// shape when no safe collapse is left.
    /// </summary>
    public static int[] Simplify(Vector3[] positions, int[] triangles, int targetTriangles)
    {
        int triCount = triangles.Length / 3;
        if (targetTriangles >= triCount)
            return (int[])triangles.Clone();

        int[] tris = (int[])triangles.Clone();
        bool[] triAlive = new bool[triCount];
        var vertexTris = new Dictionary<int, List<int>>();

        for (int t = 0; t < triCount; t++)
        {
            triAlive[t] = true;
            for (int k = 0; k < 3; k++)
            {
                int v = tris[t * 3 + k];
                if (!vertexTris.TryGetValue(v, out List<int> list))
                    vertexTris[v] = list = new List<int>();
                list.Add(t);
            }
        }

        // Open edges (used by one triangle) lock both their vertices.
        var edgeUse = new Dictionary<long, int>();
        for (int t = 0; t < triCount; t++)
        {
            for (int k = 0; k < 3; k++)
            {
                long key = EdgeKey(tris[t * 3 + k], tris[t * 3 + (k + 1) % 3]);
                edgeUse.TryGetValue(key, out int n);
                edgeUse[key] = n + 1;
            }
        }

        var locked = new HashSet<int>();
        foreach (KeyValuePair<long, int> edge in edgeUse)
        {
            if (edge.Value != 1)
                continue;

            locked.Add((int)(edge.Key >> 32));
            locked.Add((int)(edge.Key & 0xffffffff));
        }

        // Area-weighted plane quadrics.
        var quadrics = new Dictionary<int, Quadric>();
        foreach (int v in vertexTris.Keys)
            quadrics[v] = new Quadric();

        for (int t = 0; t < triCount; t++)
        {
            Vector3 p0 = positions[tris[t * 3]], p1 = positions[tris[t * 3 + 1]], p2 = positions[tris[t * 3 + 2]];
            Vector3 cross = Vector3.Cross(p1 - p0, p2 - p0);
            double area = cross.magnitude;
            if (area < 1e-12)
                continue;

            Vector3 n = cross / (float)area;
            Quadric q = Quadric.FromPlane(n.x, n.y, n.z, -Vector3.Dot(n, p0), area);

            for (int k = 0; k < 3; k++)
            {
                int v = tris[t * 3 + k];
                Quadric sum = quadrics[v];
                sum.Add(q);
                quadrics[v] = sum;
            }
        }

        var version = new Dictionary<int, int>();
        foreach (int v in vertexTris.Keys)
            version[v] = 0;

        var heap = new List<Candidate>();

        void PushEdgesAround(int v)
        {
            if (!vertexTris.TryGetValue(v, out List<int> around))
                return;

            var neighbours = new HashSet<int>();
            foreach (int t in around)
            {
                if (!triAlive[t])
                    continue;

                for (int k = 0; k < 3; k++)
                {
                    int w = tris[t * 3 + k];
                    if (w != v)
                        neighbours.Add(w);
                }
            }

            foreach (int w in neighbours)
            {
                TryPush(v, w);
                TryPush(w, v);
            }
        }

        void TryPush(int from, int to)
        {
            if (locked.Contains(from))
                return;

            Quadric q = quadrics[from];
            q.Add(quadrics[to]);

            HeapPush(heap, new Candidate
            {
                cost = q.Error(positions[to]),
                from = from,
                to = to,
                fromVersion = version[from],
                toVersion = version[to]
            });
        }

        foreach (int v in new List<int>(vertexTris.Keys))
            PushEdgesAround(v);

        int alive = triCount;
        var removed = new HashSet<int>();

        while (alive > targetTriangles && heap.Count > 0)
        {
            Candidate c = HeapPop(heap);

            // Stale: one of the vertices changed since this entry was queued.
            if (removed.Contains(c.from) || removed.Contains(c.to))
                continue;
            if (version[c.from] != c.fromVersion || version[c.to] != c.toVersion)
                continue;

            if (!CanCollapse(c.from, c.to, positions, tris, triAlive, vertexTris))
                continue;

            // Collapse: triangles on the edge disappear, the rest move from -> to.
            List<int> toTris = vertexTris[c.to];

            foreach (int t in vertexTris[c.from])
            {
                if (!triAlive[t])
                    continue;

                bool hasTo = tris[t * 3] == c.to || tris[t * 3 + 1] == c.to || tris[t * 3 + 2] == c.to;

                if (hasTo)
                {
                    triAlive[t] = false;
                    alive--;
                    continue;
                }

                for (int k = 0; k < 3; k++)
                {
                    if (tris[t * 3 + k] == c.from)
                        tris[t * 3 + k] = c.to;
                }

                toTris.Add(t);
            }

            Quadric merged = quadrics[c.to];
            merged.Add(quadrics[c.from]);
            quadrics[c.to] = merged;

            removed.Add(c.from);
            version[c.to]++;

            PushEdgesAround(c.to);
        }

        var result = new List<int>(alive * 3);
        for (int t = 0; t < triCount; t++)
        {
            if (!triAlive[t])
                continue;

            result.Add(tris[t * 3]);
            result.Add(tris[t * 3 + 1]);
            result.Add(tris[t * 3 + 2]);
        }

        return result.ToArray();
    }

    /// <summary>Rejects collapses that would flip or flatten a surviving triangle.</summary>
    private static bool CanCollapse(int from, int to, Vector3[] positions, int[] tris, bool[] triAlive,
                                    Dictionary<int, List<int>> vertexTris)
    {
        foreach (int t in vertexTris[from])
        {
            if (!triAlive[t])
                continue;

            int a = tris[t * 3], b = tris[t * 3 + 1], c = tris[t * 3 + 2];

            if (a == to || b == to || c == to)
                continue;   // this one disappears

            Vector3 pa = positions[a], pb = positions[b], pc = positions[c];
            Vector3 before = Vector3.Cross(pb - pa, pc - pa);

            if (a == from) pa = positions[to];
            if (b == from) pb = positions[to];
            if (c == from) pc = positions[to];

            Vector3 after = Vector3.Cross(pb - pa, pc - pa);

            if (after.sqrMagnitude < 1e-12f)
                return false;

            if (Vector3.Dot(before.normalized, after.normalized) < 0.3f)
                return false;
        }

        return true;
    }

    private static long EdgeKey(int a, int b)
    {
        int lo = Mathf.Min(a, b), hi = Mathf.Max(a, b);
        return ((long)lo << 32) | (uint)hi;
    }

    // Minimal binary heap: netstandard2.1 has no PriorityQueue.
    private static void HeapPush(List<Candidate> heap, Candidate item)
    {
        heap.Add(item);
        int i = heap.Count - 1;

        while (i > 0)
        {
            int parent = (i - 1) / 2;
            if (heap[parent].cost <= heap[i].cost)
                break;

            (heap[parent], heap[i]) = (heap[i], heap[parent]);
            i = parent;
        }
    }

    private static Candidate HeapPop(List<Candidate> heap)
    {
        Candidate top = heap[0];
        int last = heap.Count - 1;
        heap[0] = heap[last];
        heap.RemoveAt(last);

        int i = 0;
        while (true)
        {
            int l = i * 2 + 1, r = l + 1, smallest = i;
            if (l < heap.Count && heap[l].cost < heap[smallest].cost) smallest = l;
            if (r < heap.Count && heap[r].cost < heap[smallest].cost) smallest = r;
            if (smallest == i)
                break;

            (heap[smallest], heap[i]) = (heap[i], heap[smallest]);
            i = smallest;
        }

        return top;
    }
}
