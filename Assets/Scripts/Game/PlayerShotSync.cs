using JUTPS;
using JUTPS.WeaponSystem;
using Mirror;
using UnityEngine;

/// <summary>
/// Makes a player's gunfire visible and audible to everyone else.
///
/// Firing lives entirely in Weapon.Shot: it raycasts, spawns the bullet, plays the
/// shot sound and spawns the muzzle flash. Only the owner ever reaches it, because
/// PlayerNetworkSetup disables the character controller that drives it on remote
/// players - so a remote player used to fire completely silently and invisibly.
/// Damage already crossed the network on its own (the shooter's bullet reports the
/// hit through PlayerHealthManager.CmdShootTarget), which is why players took damage
/// from shots they never saw.
///
/// The owner reports each shot and everyone else replays its effects on the same
/// weapon. Only effects are replayed - the tracer spawned here deals no damage, since
/// the shooter's client is the one authority on whether the shot hit.
/// </summary>
[DisallowMultipleComponent]
public class PlayerShotSync : NetworkBehaviour
{
    [Header("References")]
    [Tooltip("Resolved automatically when left empty.")]
    [SerializeField] private JUCharacterController character;

    [Header("Replicated Effects")]
    [Tooltip("Seconds before a replicated tracer is cleaned up.")]
    [SerializeField] private float tracerLifetime = 3f;

    [Tooltip("Seconds before a replicated muzzle flash is cleaned up.")]
    [SerializeField] private float muzzleFlashLifetime = 2f;

    private bool subscribed;

    private void Awake()
    {
        if (character == null)
            character = GetComponent<JUCharacterController>();
    }

    // Weapon.OnShotFired is static, so the subscription is kept to the shortest
    // possible window: only while we actually own a character that can shoot.
    public override void OnStartAuthority()
    {
        base.OnStartAuthority();
        Subscribe(true);
    }

    public override void OnStopAuthority()
    {
        base.OnStopAuthority();
        Subscribe(false);
    }

    private void OnDestroy()
    {
        Subscribe(false);
    }

    private void Subscribe(bool on)
    {
        if (on == subscribed)
            return;

        if (on)
            Weapon.OnShotFired += OnShotFired;
        else
            Weapon.OnShotFired -= OnShotFired;

        subscribed = on;
    }

    private void OnShotFired(Weapon weapon)
    {
        // A static event reports every character's shots, including other players on a
        // host. Ours are the only ones we have any business reporting.
        if (weapon == null || character == null || !weapon.transform.IsChildOf(transform))
            return;

        Vector3 endPoint = weapon.LastShotEndPoint;
        uint hitNetId = 0;
        Vector3 hitLocalPoint = Vector3.zero;

        // If the shot landed on a networked object - another player - send the point
        // relative to that player instead. Everyone sees that player slightly behind
        // where the shooter saw them, so a world position would make the tracer fly
        // past them on every other screen even though the shot hit.
        if (weapon.Shoot_Position != null)
        {
            Vector3 from = weapon.Shoot_Position.position;
            Vector3 toEnd = endPoint - from;
            float distance = toEnd.magnitude;

            // Same origin, layers and line as the raycast inside Shot(), so it finds the
            // same collider that shot hit.
            if (distance > 0.01f &&
                Physics.Raycast(from, toEnd / distance, out RaycastHit hit, distance + 0.05f, weapon.RaycastingLayers))
            {
                NetworkIdentity victim = hit.collider.GetComponentInParent<NetworkIdentity>();

                if (victim != null && victim != netIdentity)
                {
                    hitNetId = victim.netId;
                    hitLocalPoint = victim.transform.InverseTransformPoint(hit.point);
                }
            }
        }

        CmdFire(weapon == character.WeaponInUseLeftHand, endPoint, hitNetId, hitLocalPoint);
    }

    // Unreliable: a dropped shot effect is not worth retransmitting, and automatic fire
    // sends these often.
    [Command(channel = Channels.Unreliable)]
    private void CmdFire(bool leftHand, Vector3 endPoint, uint hitNetId, Vector3 hitLocalPoint)
    {
        RpcFire(leftHand, endPoint, hitNetId, hitLocalPoint);
    }

    // The owner already played its own effects inside Shot().
    [ClientRpc(includeOwner = false, channel = Channels.Unreliable)]
    private void RpcFire(bool leftHand, Vector3 endPoint, uint hitNetId, Vector3 hitLocalPoint)
    {
        if (character == null)
            return;

        Weapon weapon = leftHand ? character.WeaponInUseLeftHand : character.WeaponInUseRightHand;

        if (weapon == null)
            return;

        // Land on the same spot of the victim the shooter hit, wherever that victim is
        // on this screen. Falls back to the world point if the victim is gone here.
        if (hitNetId != 0 && NetworkClient.spawned.TryGetValue(hitNetId, out NetworkIdentity victim) && victim != null)
            endPoint = victim.transform.TransformPoint(hitLocalPoint);

        PlayShotEffects(weapon, endPoint);
    }

    private void PlayShotEffects(Weapon weapon, Vector3 endPoint)
    {
        Transform muzzle = weapon.Shoot_Position;

        if (muzzle == null)
            return;

        if (weapon.MuzzleFlashParticlePrefab != null)
        {
            // Parented to the weapon so the flash travels with the gun, exactly as
            // Shot() does it for the owner.
            GameObject flash = Instantiate(
                weapon.MuzzleFlashParticlePrefab, muzzle.position, muzzle.rotation, weapon.transform);

            Destroy(flash, muzzleFlashLifetime);
        }

        if (weapon.ShootAudio != null && weapon.TryGetComponent(out AudioSource source))
        {
            source.pitch = Random.Range(0.7f, 1.1f);
            source.PlayOneShot(weapon.ShootAudio);
        }

        SpawnTracer(weapon, muzzle, endPoint);

        // Shot() only ejects a shell for these two fire modes.
        if (weapon.FireMode == Weapon.WeaponFireMode.Auto || weapon.FireMode == Weapon.WeaponFireMode.SemiAuto)
            weapon.EmitBulletShell();

        KickSlider(weapon);
    }

    /// <summary>
    /// A visual-only copy of the bullet. Weapon.WeaponRecoil is deliberately not used
    /// here: it calls RecoilReaction on the scene camera controller, which every
    /// character shares, so a remote player firing would shake the local camera.
    /// </summary>
    private void SpawnTracer(Weapon weapon, Transform muzzle, Vector3 endPoint)
    {
        if (weapon.BulletPrefab == null)
            return;

        // Fly at the shooter's real end point, not wherever this copy of the gun happens
        // to point - the remote gun pose is a smoothed approximation and spread is random.
        Vector3 direction = endPoint - muzzle.position;
        bool hasDirection = direction.sqrMagnitude > 0.0001f;
        Quaternion rotation = hasDirection ? Quaternion.LookRotation(direction) : muzzle.rotation;

        GameObject tracer = Instantiate(weapon.BulletPrefab, muzzle.position, rotation);

        if (tracer.TryGetComponent(out Bullet bullet))
        {
            // Bullet moves straight to FinalPoint, exactly as it does for the shooter.
            if (hasDirection)
            {
                bullet.FinalPoint = endPoint;
                bullet.FinalPointNormal = -direction.normalized;
            }

            // The shooter's own client decides what this shot hit and tells the server.
            // This copy must stay inert or the same shot would be counted twice, and
            // credited to whoever happens to be watching.
            bullet.BulletDamage = 0f;
            bullet.ImpactAddForce = false;

            bullet.SetOwner(gameObject);

            if (weapon.ListToIgnoreBulletCollision != null)
                bullet.Ignore(weapon.ListToIgnoreBulletCollision);
        }

        Destroy(tracer, tracerLifetime);
    }

    /// <summary>
    /// The slide kick. Weapon.ProceduralAnimation eases it back on its own, so a single
    /// nudge is the whole animation.
    /// </summary>
    private static void KickSlider(Weapon weapon)
    {
        if (!weapon.GenerateProceduralAnimation || weapon.GunSlider == null)
            return;

        Vector3 start = weapon.SliderStartLocalPosition;
        float offset = weapon.SliderMovementOffset;

        switch (weapon.SliderMovementAxis)
        {
            case Weapon.Axis.X:
                weapon.GunSlider.localPosition = new Vector3(start.x - offset, start.y, start.z);
                break;

            case Weapon.Axis.Y:
                weapon.GunSlider.localPosition = new Vector3(start.x, start.y - offset, start.z);
                break;

            default:
                weapon.GunSlider.localPosition = new Vector3(start.x, start.y, start.z - offset);
                break;
        }
    }
}
