using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows the local player which side enemies are on, the way PUBG Mobile draws sound:
/// glowing arcs on one circle around the crosshair, each with a small icon just outside
/// it, pointing at
///   - whoever just hit the local player (red arc, blood drop icon),
///   - gunfire from any enemy within earshot (orange arc, bullet icon), and
///   - footsteps of enemies moving within range (white arc, footprints icon).
/// One enemy has one arc: being hit by someone turns their gunfire arc red instead of
/// drawing a second arc on top of it.
/// Ranges are deliberately long (200 m for gunfire, 80 m for movement) so a player
/// always knows which side a fight is coming from.
///
/// Closer enemies draw stronger arcs. When two enemies sit in almost the same direction
/// their arcs simply merge on the circle and only the more important icon is drawn, so
/// icons never pile up on top of each other. Teammates, the dead and the local player
/// are never shown. Everything comes from state every client already has - player
/// positions and PlayerShotSync's shot replays - so it costs no network traffic, and
/// it works the same on the host and on clients.
///
/// Builds its own images at runtime; only the four sprites need assigning.
/// </summary>
[DisallowMultipleComponent]
public class EnemyAwarenessHUD : MonoBehaviour
{
    [Header("Look")]
    [Tooltip("White ring; each indicator shows a radially filled slice of it.")]
    [SerializeField] private Sprite ringSprite;
    [SerializeField] private Sprite gunshotIcon;
    [SerializeField] private Sprite footstepIcon;
    [SerializeField] private Sprite damageIcon;
    [SerializeField] private Color damageColor = new Color(0.95f, 0.16f, 0.16f, 1f);
    [SerializeField] private Color gunshotColor = new Color(1f, 0.5f, 0.15f, 1f);
    [SerializeField] private Color footstepColor = new Color(1f, 1f, 1f, 0.9f);

    [Tooltip("Radius of the arc circle, in canvas units. Outside the red damage arcs.")]
    [SerializeField] private float ringRadius = 240f;

    [Tooltip("Width of a gunfire arc, in degrees.")]
    [SerializeField] private float gunshotArc = 34f;

    [Tooltip("Width of a footstep arc, in degrees.")]
    [SerializeField] private float footstepArc = 24f;

    [Tooltip("Width of a damage arc, in degrees.")]
    [SerializeField] private float damageArc = 46f;

    [SerializeField] private float iconSize = 52f;

    [Tooltip("How far outside the arc the icon sits, in canvas units.")]
    [SerializeField] private float iconGap = 34f;

    [Tooltip("Icons closer together than this, in degrees, would overlap; only the most important one is drawn.")]
    [SerializeField] private float iconSeparation = 16f;

    [Header("Hearing")]
    [Tooltip("Gunfire further away than this is not shown, in metres.")]
    [SerializeField] private float gunshotRange = 200f;

    [Tooltip("Footsteps further away than this are not shown, in metres.")]
    [SerializeField] private float footstepRange = 80f;

    [Tooltip("Enemies moving slower than this make no footstep noise, in m/s. Low enough that walking counts; only standing still (or barely creeping) stays hidden.")]
    [SerializeField] private float footstepMinSpeed = 1f;

    [SerializeField] private float damageHoldTime = 1.4f;
    [SerializeField] private float gunshotHoldTime = 2.5f;
    [SerializeField] private float footstepHoldTime = 0.6f;
    [SerializeField] private float fadeTime = 0.6f;

    [Min(1)]
    [SerializeField] private int maxIcons = 8;

    private const float ScanInterval = 0.2f;

    // Ordered by importance: a higher kind wins the slot and the icon.
    private enum Kind { Footstep, Gunshot, Damage }

    private class Indicator
    {
        public RectTransform pivot;
        public Image arc;
        public RectTransform icon;
        public Image iconImage;
        public LSPlayer enemy;
        public Kind kind;
        public float timeLeft;
        public float angle;
        public float closeness;
        public bool active;
    }

    private Indicator[] indicators;
    private readonly List<Indicator> byPriority = new List<Indicator>();
    private Camera viewCamera;

    private readonly Dictionary<LSPlayer, Vector3> lastPositions = new Dictionary<LSPlayer, Vector3>();
    private LSPlayer[] players = new LSPlayer[0];
    private float nextScan;
    private float nextPlayerRefresh;

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        PlayerShotSync.ShotHeard += OnShotHeard;
        PlayerHealthManager.LocalPlayerDamagedFrom += OnDamaged;
    }

    private void OnDisable()
    {
        PlayerShotSync.ShotHeard -= OnShotHeard;
        PlayerHealthManager.LocalPlayerDamagedFrom -= OnDamaged;
        HideAll();
    }

    // -------------------------
    // Damage
    // -------------------------

    /// <summary>The server reports every landed hit on the local player with the attacker.</summary>
    private void OnDamaged(uint attackerNetId, Vector3 attackerPosition)
    {
        if (indicators == null)
            return;

        if (!Mirror.NetworkClient.spawned.TryGetValue(attackerNetId, out Mirror.NetworkIdentity attacker) || attacker == null)
            return;

        LSPlayer enemy = attacker.GetComponent<LSPlayer>();

        // Any range: whoever is hurting the player must be shown.
        if (enemy != null && enemy != LSPlayer.LocalInstance)
            Show(enemy, Kind.Damage, damageHoldTime);
    }

    // -------------------------
    // Gunfire
    // -------------------------

    private void OnShotHeard(PlayerShotSync shooter)
    {
        if (indicators == null || shooter == null)
            return;

        LSPlayer enemy = shooter.GetComponent<LSPlayer>();
        LSPlayer local = LSPlayer.LocalInstance;

        if (!IsEnemy(enemy, local))
            return;

        if (FlatDistance(enemy.transform.position, local.transform.position) > gunshotRange)
            return;

        Show(enemy, Kind.Gunshot, gunshotHoldTime);
    }

    // -------------------------
    // Footsteps
    // -------------------------

    private void ScanFootsteps(LSPlayer local)
    {
        if (Time.time >= nextPlayerRefresh)
        {
            nextPlayerRefresh = Time.time + 2f;
            players = FindObjectsByType<LSPlayer>(FindObjectsSortMode.None);
        }

        foreach (LSPlayer player in players)
        {
            if (player == null)
                continue;

            Vector3 position = player.transform.position;
            bool moved = lastPositions.TryGetValue(player, out Vector3 last);
            lastPositions[player] = position;

            if (!moved || !IsEnemy(player, local))
                continue;

            // Speed from the last scan: works the same for bots on the host and for
            // network-driven players on a client.
            float speed = FlatDistance(position, last) / ScanInterval;

            if (speed >= footstepMinSpeed && FlatDistance(position, local.transform.position) <= footstepRange)
                Show(player, Kind.Footstep, footstepHoldTime);
        }
    }

    // -------------------------
    // Indicators
    // -------------------------

    private void Show(LSPlayer enemy, Kind kind, float holdTime)
    {
        Indicator slot = FindSlot(enemy);

        // A weaker kind never replaces a stronger one still showing for this enemy.
        if (slot.active && slot.enemy == enemy && slot.kind > kind && slot.timeLeft > fadeTime)
            return;

        slot.enemy = enemy;
        slot.timeLeft = holdTime + fadeTime;

        if (slot.kind != kind || !slot.active)
        {
            slot.kind = kind;
            slot.iconImage.sprite = kind == Kind.Damage ? damageIcon : kind == Kind.Gunshot ? gunshotIcon : footstepIcon;

            // The arc is a radial slice of the ring starting at the top, turned back by
            // half its width so it is centred on the pivot's direction.
            float span = kind == Kind.Damage ? damageArc : kind == Kind.Gunshot ? gunshotArc : footstepArc;
            slot.arc.fillAmount = span / 360f;
            slot.arc.rectTransform.localEulerAngles = new Vector3(0f, 0f, span * 0.5f);
        }

        if (!slot.active)
        {
            slot.active = true;
            slot.pivot.gameObject.SetActive(true);
        }

        if (kind != Kind.Footstep)
            slot.pivot.localScale = Vector3.one * (kind == Kind.Damage ? 1.12f : 1.08f);   // a kick on each hit or shot
    }

    /// <summary>This enemy's indicator, else a free one, else the one closest to fading out.</summary>
    private Indicator FindSlot(LSPlayer enemy)
    {
        Indicator free = null;
        Indicator oldest = indicators[0];

        foreach (Indicator indicator in indicators)
        {
            if (indicator.active && indicator.enemy == enemy)
                return indicator;

            if (!indicator.active && free == null)
                free = indicator;

            if (indicator.timeLeft < oldest.timeLeft)
                oldest = indicator;
        }

        return free ?? oldest;
    }

    private void Update()
    {
        if (indicators == null)
            return;

        LSPlayer local = LSPlayer.LocalInstance;

        if (local == null || !local.isAlive)
        {
            HideAll();
            return;
        }

        if (Time.time >= nextScan)
        {
            nextScan = Time.time + ScanInterval;
            ScanFootsteps(local);
        }

        Vector3 forward = ViewForward(local.transform);
        float deltaTime = Time.deltaTime;

        foreach (Indicator indicator in indicators)
        {
            if (indicator.active)
                UpdateIndicator(indicator, local, forward, deltaTime);
        }

        SeparateIcons();
    }

    private void UpdateIndicator(Indicator indicator, LSPlayer local, Vector3 forward, float deltaTime)
    {
        indicator.timeLeft -= deltaTime;

        if (indicator.timeLeft <= 0f || indicator.enemy == null || !indicator.enemy.isAlive)
        {
            indicator.active = false;
            indicator.pivot.gameObject.SetActive(false);
            return;
        }

        Vector3 toEnemy = indicator.enemy.transform.position - local.transform.position;
        toEnemy.y = 0f;

        if (toEnemy.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f)
        {
            // Positive angle = enemy to the right; UI rotation is counter-clockwise.
            indicator.angle = Vector3.SignedAngle(forward, toEnemy, Vector3.up);
            indicator.pivot.localEulerAngles = new Vector3(0f, 0f, -indicator.angle);
            indicator.icon.localEulerAngles = new Vector3(0f, 0f, indicator.angle);   // keep the icon upright
        }

        // Being hit is always drawn at full strength, however far away the shooter is.
        float range = indicator.kind == Kind.Gunshot ? gunshotRange : footstepRange;
        indicator.closeness = indicator.kind == Kind.Damage ? 1f : 1f - Mathf.Clamp01(toEnemy.magnitude / Mathf.Max(1f, range));
        float fade = indicator.timeLeft >= fadeTime ? 1f : indicator.timeLeft / Mathf.Max(0.01f, fadeTime);

        Color color = indicator.kind == Kind.Damage ? damageColor : indicator.kind == Kind.Gunshot ? gunshotColor : footstepColor;
        // Far enemies are dimmer but must stay readable at the long ranges.
        color.a *= fade * Mathf.Lerp(0.65f, 1f, indicator.closeness);
        indicator.arc.color = color;
        indicator.iconImage.color = color;

        indicator.pivot.localScale = Vector3.MoveTowards(indicator.pivot.localScale, Vector3.one, 0.6f * deltaTime);
    }

    /// <summary>
    /// Arcs in the same direction just merge on the circle, but their icons would sit on
    /// top of each other. Only the most important icon in any spot is drawn: damage,
    /// then gunfire, then footsteps, then the closer enemy.
    /// </summary>
    private void SeparateIcons()
    {
        byPriority.Clear();

        foreach (Indicator indicator in indicators)
        {
            if (indicator.active)
                byPriority.Add(indicator);
        }

        byPriority.Sort((a, b) => a.kind != b.kind ? b.kind.CompareTo(a.kind) : b.closeness.CompareTo(a.closeness));

        for (int i = 0; i < byPriority.Count; i++)
        {
            bool covered = false;

            for (int j = 0; j < i && !covered; j++)
            {
                if (byPriority[j].icon.gameObject.activeSelf &&
                    Mathf.Abs(Mathf.DeltaAngle(byPriority[i].angle, byPriority[j].angle)) < iconSeparation)
                    covered = true;
            }

            if (byPriority[i].icon.gameObject.activeSelf == covered)
                byPriority[i].icon.gameObject.SetActive(!covered);
        }
    }

    private bool IsEnemy(LSPlayer other, LSPlayer local)
    {
        if (other == null || local == null || other == local || !other.isAlive)
            return false;

        // Solo gives everyone their own team, so only a shared positive team is friendly.
        return local.teamID <= 0 || other.teamID != local.teamID;
    }

    /// <summary>Flattened camera forward, falling back to the character when there is no camera.</summary>
    private Vector3 ViewForward(Transform character)
    {
        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            viewCamera = Camera.main;

        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : character.forward;
        forward.y = 0f;
        return forward;
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private void HideAll()
    {
        if (indicators == null)
            return;

        foreach (Indicator indicator in indicators)
        {
            if (!indicator.active)
                continue;

            indicator.active = false;
            indicator.timeLeft = 0f;
            indicator.pivot.gameObject.SetActive(false);
        }
    }

    private void Build()
    {
        if (ringSprite == null || gunshotIcon == null || footstepIcon == null || damageIcon == null)
        {
            Debug.LogWarning("[EnemyAwarenessHUD] Ring, damage, gunshot and footstep sprites must all be assigned.");
            return;
        }

        indicators = new Indicator[maxIcons];

        // The ring sprite's circle runs close to its edge, so the image is sized to put
        // the arc on ringRadius.
        float ringSize = ringRadius * 2f * (256f / 237f);

        for (int i = 0; i < maxIcons; i++)
        {
            // Zero-size pivot at the screen centre; rotating it swings arc and icon round.
            var pivotGo = new GameObject("EnemyIndicator " + (i + 1), typeof(RectTransform));
            pivotGo.transform.SetParent(transform, false);

            var pivot = (RectTransform)pivotGo.transform;
            pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 0.5f);
            pivot.anchoredPosition = Vector2.zero;
            pivot.sizeDelta = Vector2.zero;

            var arcGo = new GameObject("Arc", typeof(RectTransform));
            arcGo.transform.SetParent(pivot, false);

            var arc = arcGo.AddComponent<Image>();
            arc.sprite = ringSprite;
            arc.type = Image.Type.Filled;
            arc.fillMethod = Image.FillMethod.Radial360;
            arc.fillOrigin = (int)Image.Origin360.Top;
            arc.fillClockwise = true;
            arc.raycastTarget = false;   // never swallow touches meant for the controls
            arc.rectTransform.sizeDelta = new Vector2(ringSize, ringSize);

            var iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(pivot, false);

            var icon = (RectTransform)iconGo.transform;
            icon.anchorMin = icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.anchoredPosition = new Vector2(0f, ringRadius + iconGap);
            icon.sizeDelta = new Vector2(iconSize, iconSize);

            var iconImage = iconGo.AddComponent<Image>();
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            // Readable over bright sky and dark ground alike.
            var outline = iconGo.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.6f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            pivotGo.SetActive(false);

            indicators[i] = new Indicator { pivot = pivot, arc = arc, icon = icon, iconImage = iconImage };
        }
    }
}
