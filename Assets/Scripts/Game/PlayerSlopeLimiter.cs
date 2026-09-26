using JUTPS;
using UnityEngine;

/// <summary>
/// Stops the local player walking or sprinting up ground steeper than maxClimbAngle.
///
/// JUTPS drives the character by setting its Rigidbody velocity every physics step and
/// only slides it back down afterwards, so pushing into a near-vertical bank simply
/// ran the capsule up it. This runs after JUCharacterController's FixedUpdate and
/// removes the part of the velocity that goes into a face that is too steep - the
/// player still slides along it and can walk away or back down freely.
///
/// Tree trunks are handled too: brushing one used to stop a sprint dead (the capsule
/// kept pushing into the trunk and friction held it there). A trunk is steered around
/// at full speed instead - the direction along its surface, or a side when running
/// straight into it. The surface is taken from the body's real contact when it touches
/// one, and from a probe ahead otherwise.
///
/// Added by PlayerNetworkSetup to the local player only; bots keep JUTPS's behaviour.
/// </summary>
[DefaultExecutionOrder(100)]
[DisallowMultipleComponent]
public class PlayerSlopeLimiter : MonoBehaviour
{
    [Tooltip("Ground steeper than this, in degrees, cannot be climbed.")]
    [Range(30f, 89f)]
    [SerializeField] private float maxClimbAngle = 55f;

    [Tooltip("How far past the body the ground ahead is felt.")]
    [SerializeField] private float probeDistance = 0.35f;

    private JUCharacterController character;
    private Rigidbody body;
    private float bodyRadius = 0.4f;

    // Steepest-facing contact from the last physics step (OnCollisionStay runs after it).
    private bool hasContact;
    private Vector3 contactNormal;
    private bool contactIsTree;

    private void Awake()
    {
        character = GetComponent<JUCharacterController>();
        body = GetComponent<Rigidbody>();

        if (TryGetComponent(out CapsuleCollider capsule))
            bodyRadius = capsule.radius;
    }

    private void FixedUpdate()
    {
        if (character == null || body == null || !character.enabled || body.isKinematic || character.IsDriving)
            return;

        Vector3 velocity = body.linearVelocity;
        Vector3 flat = new Vector3(velocity.x, 0f, velocity.z);

        bool touching = hasContact;
        Vector3 touchedNormal = contactNormal;
        bool touchedTree = contactIsTree;
        hasContact = false;

        if (flat.sqrMagnitude < 0.01f)
            return;

        Vector3 faceNormal;
        bool isTree;

        // What the body actually touches beats what a probe expects to touch: a probe
        // narrower than the body meets a round trunk at a different angle.
        if (touching && Vector3.Dot(flat, touchedNormal) < 0f)
        {
            faceNormal = touchedNormal;
            isTree = touchedTree;
        }
        else if (!FindSteepFace(flat.normalized, out faceNormal, out isTree))
        {
            return;
        }

        // Horizontal direction pointing away from the face; moving against it is climbing.
        Vector3 away = new Vector3(faceNormal.x, 0f, faceNormal.z);

        if (away.sqrMagnitude < 0.0001f)
            return;

        away.Normalize();
        float into = Vector3.Dot(flat, away);

        if (into >= 0f)
            return;   // moving along it or away from it

        Vector3 along = flat - away * into;

        if (isTree)
        {
            // Straight into the trunk leaves nothing along it: pass on one side.
            if (along.sqrMagnitude < 0.0001f)
                along = Vector3.Cross(Vector3.up, away);

            along = along.normalized * flat.magnitude;
        }

        flat = along;

        // A jump keeps its own upward speed; walking into the face never gains height.
        float vertical = character.IsJumping ? velocity.y : Mathf.Min(velocity.y, 0f);
        body.linearVelocity = new Vector3(flat.x, vertical, flat.z);
    }

    private void OnCollisionStay(Collision collision)
    {
        float minUp = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        Vector3 flat = new Vector3(body.linearVelocity.x, 0f, body.linearVelocity.z);

        for (int i = 0; i < collision.contactCount; i++)
        {
            Vector3 normal = collision.GetContact(i).normal;

            if (normal.y >= minUp)
                continue;   // ground that can be walked on

            // Keep the one most against the way the body is going.
            if (hasContact && Vector3.Dot(flat, normal) >= Vector3.Dot(flat, contactNormal))
                continue;

            hasContact = true;
            contactNormal = normal;
            contactIsTree = IsTrunk(collision.collider, normal);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        OnCollisionStay(collision);
    }

    // Terrain trees are part of the TerrainCollider; a trunk is the only near-vertical
    // surface it has (the ground itself never gets that steep).
    private static bool IsTrunk(Collider collider, Vector3 normal)
    {
        return collider is TerrainCollider && Mathf.Abs(normal.y) < 0.15f;
    }

    /// <summary>
    /// Feels ahead at knee and waist height, and under the feet, for ground steeper than
    /// the limit. Gentle slopes are hit too, but their normals are upright enough to pass.
    /// The forward probes are as wide as the body, so a trunk the body only brushes is
    /// found as well as one straight ahead.
    /// </summary>
    private bool FindSteepFace(Vector3 direction, out Vector3 normal, out bool isTree)
    {
        float minUp = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        float reach = probeDistance + bodyRadius * 0.25f;
        float width = bodyRadius * 0.75f;
        int mask = character.WhatIsGround;
        Vector3 feet = transform.position;

        if (Probe(feet + Vector3.up * 0.5f, direction, width, reach, mask, minUp, out normal, out isTree) ||
            Probe(feet + Vector3.up * 1.0f, direction, width, reach, mask, minUp, out normal, out isTree))
            return true;

        // Already standing on the steep part.
        return Probe(feet + Vector3.up * 0.5f + direction * bodyRadius, Vector3.down, 0f, 1f, mask, minUp, out normal, out isTree);
    }

    private static bool Probe(Vector3 origin, Vector3 direction, float width, float distance, int mask, float minUp,
                              out Vector3 normal, out bool isTree)
    {
        normal = Vector3.up;
        isTree = false;

        RaycastHit hit;
        bool found = width > 0f
            ? Physics.SphereCast(origin, width, direction, out hit, distance, mask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(origin, direction, out hit, distance, mask, QueryTriggerInteraction.Ignore);

        if (!found)
            return false;

        normal = hit.normal;

        isTree = IsTrunk(hit.collider, hit.normal);
        return hit.normal.y < minUp;
    }
}
