using Mirror;
using UnityEngine;

/// <summary>
/// Keeps a player inside the playable ground so nobody can run off the edge.
/// Bounds are read from the scene itself: the Terrain when there is one
/// (GameScene), otherwise the widest flat ground collider (the LobbyScene plane).
/// The area is then pulled in by edgeMargin on every side, so the player stops
/// well before the actual edge instead of at it.
/// </summary>
[DisallowMultipleComponent]
public class PlayerBoundary : MonoBehaviour
{
    [Header("Limits")]
    [Tooltip("How far inside the ground edge the player is stopped, in world units.")]
    [SerializeField] private float edgeMargin = 20f;

    [Tooltip("Ignore colliders taller than this when looking for the ground. Terrain is used first when present.")]
    [SerializeField] private float maxGroundThickness = 5f;

    [Header("Manual Override")]
    [Tooltip("Skip scene detection and use the values below instead.")]
    [SerializeField] private bool useManualBounds = false;
    [SerializeField] private Vector2 manualMinXZ = new Vector2(-500f, -500f);
    [SerializeField] private Vector2 manualMaxXZ = new Vector2(500f, 500f);

    [Header("Debug")]
    [SerializeField] private bool logBounds = true;

    private float minX;
    private float maxX;
    private float minZ;
    private float maxZ;

    private bool boundsReady;
    private Rigidbody body;
    private NetworkIdentity identity;

private void Awake()
    {
        body = GetComponent<Rigidbody>();
        identity = GetComponent<NetworkIdentity>();
    }

    private void Start()
    {
        // Start, not Awake: the ground has to exist in the loaded scene first.
        ResolveBounds();
    }

    private void LateUpdate()
    {
        // Only the player actually driving this character needs clamping. Doing it
        // here makes the stop immediate instead of waiting for a network correction.
        if (!boundsReady || identity == null || !identity.isOwned)
            return;

        Vector3 position = transform.position;

        float clampedX = Mathf.Clamp(position.x, minX, maxX);
        float clampedZ = Mathf.Clamp(position.z, minZ, maxZ);

        if (Mathf.Approximately(clampedX, position.x) && Mathf.Approximately(clampedZ, position.z))
            return;

        transform.position = new Vector3(clampedX, position.y, clampedZ);

        if (body != null)
        {
            // Cancel the outward velocity, otherwise holding forward grinds the player
            // against the invisible wall and the character keeps trying to push through.
            Vector3 velocity = body.linearVelocity;

            if (!Mathf.Approximately(clampedX, position.x)) velocity.x = 0f;
            if (!Mathf.Approximately(clampedZ, position.z)) velocity.z = 0f;

            body.linearVelocity = velocity;
        }
    }

    private void ResolveBounds()
    {
        if (useManualBounds)
        {
            SetBounds(manualMinXZ.x, manualMaxXZ.x, manualMinXZ.y, manualMaxXZ.y, "manual");
            return;
        }

        // GameScene: the terrain defines the playable area.
        Terrain terrain = Terrain.activeTerrain;

        if (terrain != null && terrain.terrainData != null)
        {
            Vector3 origin = terrain.transform.position;
            Vector3 size = terrain.terrainData.size;

            SetBounds(origin.x, origin.x + size.x, origin.z, origin.z + size.z, "terrain '" + terrain.name + "'");
            return;
        }

        // LobbyScene and anything else: fall back to the widest flat ground collider.
        Collider widest = null;
        float widestArea = 0f;

        Collider[] colliders = FindObjectsByType<Collider>(FindObjectsSortMode.None);

        for (int i = 0; i < colliders.Length; i++)
        {
            Collider candidate = colliders[i];

            if (candidate == null || candidate.isTrigger)
                continue;

            // Skip anything attached to a character.
            if (candidate.GetComponentInParent<LSPlayer>() != null)
                continue;

            Bounds bounds = candidate.bounds;

            if (bounds.size.y > maxGroundThickness)
                continue;

            float area = bounds.size.x * bounds.size.z;

            if (area > widestArea)
            {
                widestArea = area;
                widest = candidate;
            }
        }

        if (widest != null)
        {
            Bounds bounds = widest.bounds;
            SetBounds(bounds.min.x, bounds.max.x, bounds.min.z, bounds.max.z, "collider '" + widest.name + "'");
            return;
        }

        boundsReady = false;
        Debug.LogWarning("[PlayerBoundary] No terrain or ground collider found. Player movement is unbounded.");
    }

    private void SetBounds(float rawMinX, float rawMaxX, float rawMinZ, float rawMaxZ, string source)
    {
        float margin = Mathf.Max(0f, edgeMargin);

        minX = rawMinX + margin;
        maxX = rawMaxX - margin;
        minZ = rawMinZ + margin;
        maxZ = rawMaxZ - margin;

        // A margin wider than the ground itself would invert the range, which would
        // pin every player to a single point. Collapse to the centre instead.
        if (minX > maxX)
        {
            float centreX = (rawMinX + rawMaxX) * 0.5f;
            minX = centreX;
            maxX = centreX;
        }

        if (minZ > maxZ)
        {
            float centreZ = (rawMinZ + rawMaxZ) * 0.5f;
            minZ = centreZ;
            maxZ = centreZ;
        }

        boundsReady = true;

        if (logBounds)
        {
            Debug.Log(
                $"[PlayerBoundary] Bounds from {source} | margin = {margin} | " +
                $"X [{minX:F1} .. {maxX:F1}] Z [{minZ:F1} .. {maxZ:F1}]"
            );
        }
    }
}
