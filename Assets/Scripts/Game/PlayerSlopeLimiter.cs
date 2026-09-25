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

        if (flat.sqrMagnitude < 0.01f)
            return;

        if (!FindSteepFace(flat.normalized, out Vector3 faceNormal))
            return;

        // Horizontal direction pointing away from the face; moving against it is climbing.
        Vector3 away = new Vector3(faceNormal.x, 0f, faceNormal.z);

        if (away.sqrMagnitude < 0.0001f)
            return;

        away.Normalize();
        float into = Vector3.Dot(flat, away);

        if (into >= 0f)
            return;   // moving along it or away from it

        flat -= away * into;

        // A jump keeps its own upward speed; walking into the face never gains height.
        float vertical = character.IsJumping ? velocity.y : Mathf.Min(velocity.y, 0f);
        body.linearVelocity = new Vector3(flat.x, vertical, flat.z);
    }

    /// <summary>
    /// Feels ahead at knee and waist height, and under the feet, for ground steeper than
    /// the limit. Gentle slopes are hit too, but their normals are upright enough to pass.
    /// </summary>
    private bool FindSteepFace(Vector3 direction, out Vector3 normal)
    {
        float minUp = Mathf.Cos(maxClimbAngle * Mathf.Deg2Rad);
        float reach = bodyRadius + probeDistance;
        int mask = character.WhatIsGround;
        Vector3 feet = transform.position;

        if (Probe(feet + Vector3.up * 0.3f, direction, reach, mask, minUp, out normal) ||
            Probe(feet + Vector3.up * 0.9f, direction, reach, mask, minUp, out normal))
            return true;

        // Already standing on the steep part.
        return Probe(feet + Vector3.up * 0.5f + direction * bodyRadius, Vector3.down, 1f, mask, minUp, out normal);
    }

    private static bool Probe(Vector3 origin, Vector3 direction, float distance, int mask, float minUp, out Vector3 normal)
    {
        normal = Vector3.up;

        if (!Physics.Raycast(origin, direction, out RaycastHit hit, distance, mask, QueryTriggerInteraction.Ignore))
            return false;

        normal = hit.normal;
        return hit.normal.y < minUp;
    }
}
