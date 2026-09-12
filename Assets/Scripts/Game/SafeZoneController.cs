using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

/// <summary>
/// Battle-royale safe zone: shrinks in stages and moves as it shrinks.
///
/// Each stage picks its next centre as a random point inside an auxiliary circle
/// whose radius is (currentRadius - nextRadius), centred on the current zone. That
/// guarantees the next circle sits fully inside the current one while still being
/// off-centre, which is what makes players have to move.
///
/// Centre, radius and phase are all SyncVars, so every client draws the same zone
/// and can run its own countdown without extra messages. The countdown uses
/// NetworkTime so all clients agree on the remaining seconds.
/// </summary>
public class SafeZoneController : NetworkBehaviour
{
    public enum ZonePhase
    {
        Idle,       // not started yet
        Waiting,    // holding still before the next shrink
        Shrinking,  // actively closing in
        Final       // last stage reached, damage stays on
    }

    [System.Serializable]
    public class SafeZoneCase
    {
        [Min(0.1f)] public float runDuration = 4f;
        [Min(0f)] public float stopDuration = 15f;
        [Min(0.1f)] public float targetRadius = 20f;
        [Min(0f)] public float damagePerTick = 1f;
        [Min(0.1f)] public float damageInterval = 1f;
    }

    [Header("Zone Visual")]
    [Tooltip("Tall cylinder that reads as the wall. Scaled and moved to match the zone.")]
    [SerializeField] private Transform zoneVisual;

    [Tooltip("Optional ground ring visual. Leave empty if you do not use a ring.")]
    [SerializeField] private Transform zoneRing;

    [Min(0.01f)]
    [Tooltip("World-space diameter of the ring mesh when its X/Z scale is 1. Unity Plane = 10.")]
    [SerializeField] private float zoneRingDiameterAtScaleOne = 10f;

    [Min(1f)]
    [Tooltip("How tall the wall is drawn, in world units.")]
    [SerializeField] private float wallHeight = 160f;

    [Tooltip("World Y the wall sits on. Should be at or just below the ground.")]
    [SerializeField] private float groundY = 0f;

    [Min(0.1f)]
    [SerializeField] private float startRadius = 25f;

    [Header("Safe Zone Cases")]
    [SerializeField] private List<SafeZoneCase> cases = new List<SafeZoneCase>();

    [Header("Movement")]
    [Tooltip("0 = next zone stays concentric. 1 = it may sit anywhere fully inside the current zone.")]
    [Range(0f, 1f)]
    [SerializeField] private float driftAmount = 0.85f;

    [Header("Start Settings")]
    [SerializeField] private bool autoStart = true;
    [Min(0f)][SerializeField] private float startDelay = 0f;

    // -------------------------
    // Synced state
    // -------------------------

    [SyncVar(hook = nameof(OnCenterChanged))]
    private Vector3 zoneCenter;

    [SyncVar(hook = nameof(OnRadiusChanged))]
    private float currentRadius;

    [SyncVar]
    private ZonePhase phase = ZonePhase.Idle;

    /// <summary>NetworkTime.time at which the current phase ends. Lets each client count down alone.</summary>
    [SyncVar]
    private double phaseEndTime;

    [SyncVar]
    private int currentCaseIndex = -1;

    private float currentDamagePerTick;
    private float currentDamageInterval = 1f;
    private float damageTimer;
    private Coroutine zoneRoutine;

    // -------------------------
    // Read-only API for UI
    // -------------------------

    public float CurrentRadius => currentRadius;
    public int CurrentCaseIndex => currentCaseIndex;
    public int TotalCases => cases != null ? cases.Count : 0;
    public ZonePhase Phase => phase;
    public Vector3 ZoneCenter => zoneCenter;
    public bool ZoneRunning => phase == ZonePhase.Shrinking || phase == ZonePhase.Waiting || phase == ZonePhase.Final;

    /// <summary>Seconds left in the current phase, never negative.</summary>
    public float SecondsRemaining
    {
        get { return Mathf.Max(0f, (float)(phaseEndTime - NetworkTime.time)); }
    }

    // -------------------------
    // Lifecycle
    // -------------------------

    private void Start()
    {
        // A client that joins mid-hold would otherwise draw the authored values:
        // the SyncVar hooks only fire on change.
        ApplyVisual();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();

        zoneCenter = new Vector3(transform.position.x, groundY, transform.position.z);
        SetRadius(startRadius);

        if (autoStart)
            StartSafeZone();
    }

    [Server]
    public void StartSafeZone()
    {
        if (zoneRoutine != null)
            return;

        zoneRoutine = StartCoroutine(SafeZoneRoutine());
    }

    [Server]
    public void StopSafeZone()
    {
        if (zoneRoutine != null)
        {
            StopCoroutine(zoneRoutine);
            zoneRoutine = null;
        }

        phase = ZonePhase.Idle;
        currentCaseIndex = -1;
    }

    [Server]
    private IEnumerator SafeZoneRoutine()
    {
        if (cases == null || cases.Count == 0)
        {
            Debug.LogWarning("[SafeZone] No safe-zone cases configured.");
            zoneRoutine = null;
            yield break;
        }

        // Opening hold before the first shrink.
        if (startDelay > 0f)
        {
            BeginPhase(ZonePhase.Waiting, startDelay);
            yield return new WaitForSeconds(startDelay);
        }

        for (int i = 0; i < cases.Count; i++)
        {
            SafeZoneCase zoneCase = cases[i];

            if (zoneCase == null)
                continue;

            currentCaseIndex = i;
            currentDamagePerTick = Mathf.Max(0f, zoneCase.damagePerTick);
            currentDamageInterval = Mathf.Max(0.1f, zoneCase.damageInterval);
            damageTimer = 0f;

            float fromRadius = currentRadius;
            float toRadius = Mathf.Max(0.1f, zoneCase.targetRadius);
            float duration = Mathf.Max(0.1f, zoneCase.runDuration);

            Vector3 fromCenter = zoneCenter;
            Vector3 toCenter = PickNextCenter(fromCenter, fromRadius, toRadius);

            BeginPhase(ZonePhase.Shrinking, duration);

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // SmoothStep makes each shrink start and finish gently.
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                zoneCenter = Vector3.Lerp(fromCenter, toCenter, smoothT);
                SetRadius(Mathf.Lerp(fromRadius, toRadius, smoothT));
                yield return null;
            }

            zoneCenter = toCenter;
            SetRadius(toRadius);

            // Hold before the next stage.
            if (zoneCase.stopDuration > 0f)
            {
                BeginPhase(ZonePhase.Waiting, zoneCase.stopDuration);
                yield return new WaitForSeconds(zoneCase.stopDuration);
            }
        }

        currentCaseIndex = cases.Count - 1;
        BeginPhase(ZonePhase.Final, 0f);
        zoneRoutine = null;
    }

    /// <summary>
    /// Random point inside the auxiliary circle of radius (current - next), so the
    /// new circle is always fully contained by the old one.
    /// </summary>
    [Server]
    private Vector3 PickNextCenter(Vector3 center, float fromRadius, float toRadius)
    {
        float maxDrift = Mathf.Max(0f, (fromRadius - toRadius) * Mathf.Clamp01(driftAmount));

        if (maxDrift <= 0.01f)
            return center;

        // sqrt keeps the point uniformly distributed over the disc rather than
        // clustering towards the middle.
        Vector2 offset = Random.insideUnitCircle.normalized * (maxDrift * Mathf.Sqrt(Random.value));

        return new Vector3(center.x + offset.x, center.y, center.z + offset.y);
    }

    [Server]
    private void BeginPhase(ZonePhase newPhase, float seconds)
    {
        phase = newPhase;
        phaseEndTime = NetworkTime.time + seconds;
    }

    // -------------------------
    // Damage
    // -------------------------

    private void Update()
    {
        if (!isServer)
            return;

        // Only the shrink and hold stages damage; nothing happens before the match zone starts.
        if (phase != ZonePhase.Shrinking && phase != ZonePhase.Waiting && phase != ZonePhase.Final)
            return;

        if (currentCaseIndex < 0)
            return;

        damageTimer += Time.deltaTime;

        if (damageTimer < currentDamageInterval)
            return;

        // Subtract instead of zeroing so ticks do not drift on a slow frame.
        damageTimer -= currentDamageInterval;
        ApplyOutsideZoneDamage();
    }

    [Server]
    private void ApplyOutsideZoneDamage()
    {
        if (currentDamagePerTick <= 0f)
            return;

        if (LSMatchManager.Instance == null)
            return;

        Vector2 center = new Vector2(zoneCenter.x, zoneCenter.z);

        foreach (LSPlayer player in LSMatchManager.Instance.players)
        {
            if (player == null || !player.isAlive)
                continue;

            Vector2 playerPosition = new Vector2(player.transform.position.x, player.transform.position.z);

            if (Vector2.Distance(center, playerPosition) <= currentRadius)
                continue;

            PlayerHealthManager health = player.GetComponent<PlayerHealthManager>();

            if (health != null)
                health.ServerApplyDamage(currentDamagePerTick);
        }
    }

    /// <summary>True when this world position is inside the safe zone. Useful for UI arrows.</summary>
    public bool IsInsideZone(Vector3 worldPosition)
    {
        Vector2 center = new Vector2(zoneCenter.x, zoneCenter.z);
        Vector2 point = new Vector2(worldPosition.x, worldPosition.z);

        return Vector2.Distance(center, point) <= currentRadius;
    }

    // -------------------------
    // Visuals
    // -------------------------

    [Server]
    private void SetRadius(float radius)
    {
        currentRadius = Mathf.Max(0.1f, radius);
        ApplyVisual();
    }

    private void OnRadiusChanged(float oldRadius, float newRadius)
    {
        ApplyVisual();
    }

    private void OnCenterChanged(Vector3 oldCenter, Vector3 newCenter)
    {
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        float radius = currentRadius > 0f ? currentRadius : startRadius;
        float diameter = radius * 2f;

        Vector3 center = zoneCenter;
        if (center == Vector3.zero)
            center = new Vector3(transform.position.x, groundY, transform.position.z);

        // A Unity cylinder is 2 units tall and 1 unit wide at scale 1, so the wall
        // gets height/2 on Y and sits with its base on the ground.
        if (zoneVisual != null)
        {
            zoneVisual.position = new Vector3(center.x, groundY + wallHeight * 0.5f, center.z);
            zoneVisual.localScale = new Vector3(diameter, wallHeight * 0.5f, diameter);
        }

        // Unity Plane is 10 x 10 world units at scale 1, hence the default value.
        if (zoneRing != null)
        {
            float baseDiameter = Mathf.Max(0.01f, zoneRingDiameterAtScaleOne);
            float ringScaleXZ = diameter / baseDiameter;

            zoneRing.position = new Vector3(center.x, groundY + 0.15f, center.z);
            zoneRing.localScale = new Vector3(ringScaleXZ, 1f, ringScaleXZ);
        }
    }
}
