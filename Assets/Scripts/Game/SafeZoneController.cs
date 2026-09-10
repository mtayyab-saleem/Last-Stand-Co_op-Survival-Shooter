using System.Collections;
using System.Collections.Generic;
using Mirror;
using UnityEngine;

public class SafeZoneController : NetworkBehaviour
{
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
    [SerializeField] private Transform zoneVisual;

    [Tooltip("Optional ground ring visual. Leave empty if you do not use a ring.")]
    [SerializeField] private Transform zoneRing;

    [Min(0.01f)]
    [Tooltip("World-space diameter of the ring mesh when its X/Z scale is 1. Unity Plane = 10.")]
    [SerializeField] private float zoneRingDiameterAtScaleOne = 10f;

    [Min(0.1f)]
    [SerializeField] private float startRadius = 25f;

    [Header("Safe Zone Cases")]
    [SerializeField] private List<SafeZoneCase> cases = new List<SafeZoneCase>();

    [Header("Start Settings")]
    [SerializeField] private bool autoStart = true;
    [Min(0f)][SerializeField] private float startDelay = 0f;

    [Header("Runtime Debug")]
    [SyncVar][SerializeField] private int currentCaseIndex = -1;

    [SyncVar(hook = nameof(OnRadiusChanged))]
    [SerializeField] private float currentRadius;

    [SyncVar][SerializeField] private bool zoneRunning = false;

    private float currentDamagePerTick;
    private float currentDamageInterval = 1f;
    private float damageTimer;
    private Coroutine zoneRoutine;

    public float CurrentRadius => currentRadius;
    public int CurrentCaseIndex => currentCaseIndex;
    public bool ZoneRunning => zoneRunning;

private void Start()
    {
        // A client that joins while the zone is holding still would otherwise draw the
        // start radius forever: the SyncVar hook only fires when the value changes.
        ApplyVisualRadius(currentRadius > 0f ? currentRadius : startRadius);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
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

        zoneRunning = false;
        currentCaseIndex = -1;
    }

    [Server]
    private IEnumerator SafeZoneRoutine()
    {
        if (startDelay > 0f)
            yield return new WaitForSeconds(startDelay);

        if (cases == null || cases.Count == 0)
        {
            Debug.LogWarning("[SafeZone] No safe-zone cases configured.");
            zoneRoutine = null;
            yield break;
        }

        zoneRunning = true;

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

            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // SmoothStep makes the zone begin and finish each shrink gently.
                // Existing timing and target radius remain unchanged.
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                SetRadius(Mathf.Lerp(fromRadius, toRadius, smoothT));
                yield return null;
            }

            SetRadius(toRadius);

            if (zoneCase.stopDuration > 0f)
                yield return new WaitForSeconds(zoneCase.stopDuration);
        }

        currentCaseIndex = cases.Count - 1;
        zoneRunning = true;
        zoneRoutine = null;

        Debug.Log("[SafeZone] Final case reached. Final zone damage remains active.");
    }

private void Update()
    {
        if (!isServer || !zoneRunning)
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

        Vector2 zoneCenter = new Vector2(transform.position.x, transform.position.z);

        foreach (LSPlayer player in LSMatchManager.Instance.players)
        {
            if (player == null || !player.isAlive)
                continue;

            Vector2 playerPosition = new Vector2(player.transform.position.x, player.transform.position.z);
            float distance = Vector2.Distance(zoneCenter, playerPosition);

            if (distance <= currentRadius)
                continue;

            PlayerHealthManager health = player.GetComponent<PlayerHealthManager>();

            if (health != null)
                health.ServerApplyDamage(currentDamagePerTick);
        }
    }

    [Server]
    private void SetRadius(float radius)
    {
        currentRadius = Mathf.Max(0.1f, radius);
        ApplyVisualRadius(currentRadius);
    }

    private void OnRadiusChanged(float oldRadius, float newRadius)
    {
        ApplyVisualRadius(newRadius);
    }

    private void ApplyVisualRadius(float radius)
    {
        float diameter = radius * 2f;

        // Default Unity Cylinder has a world-space diameter of 1 at X/Z scale 1.
        if (zoneVisual != null)
        {
            Vector3 visualScale = zoneVisual.localScale;
            visualScale.x = diameter;
            visualScale.z = diameter;
            zoneVisual.localScale = visualScale;
        }

        // Optional ground ring.
        // Unity Plane is 10 x 10 world units at scale 1, so the default value is 10.
        if (zoneRing != null)
        {
            float baseDiameter = Mathf.Max(0.01f, zoneRingDiameterAtScaleOne);
            float ringScaleXZ = diameter / baseDiameter;

            Vector3 ringScale = zoneRing.localScale;
            ringScale.x = ringScaleXZ;
            ringScale.z = ringScaleXZ;
            zoneRing.localScale = ringScale;
        }
    }
}
