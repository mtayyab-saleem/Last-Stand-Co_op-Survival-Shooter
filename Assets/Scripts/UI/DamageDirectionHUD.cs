using Mirror;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Red arcs around the crosshair pointing at whoever is shooting the local player.
///
/// The server reports every landed enemy hit to the victim through
/// PlayerHealthManager.LocalPlayerDamagedFrom. Each attacker gets its own arc, so
/// being shot from several sides shows several arcs at once; a repeat hit from the
/// same attacker refreshes that attacker's arc instead of stacking another one.
///
/// Arcs are rotated against the camera, not the character, so they stay correct while
/// the player looks around. While an arc is visible it follows the attacker's live
/// position when that player is still spawned on this client.
///
/// Builds its own images at runtime, so the only wiring needed is the sprite.
/// </summary>
[DisallowMultipleComponent]
public class DamageDirectionHUD : MonoBehaviour
{
    [Header("Look")]
    [SerializeField] private Sprite indicatorSprite;
    [SerializeField] private Color tint = Color.white;

    [Tooltip("How many attackers can be shown at the same time.")]
    [Min(1)]
    [SerializeField] private int maxIndicators = 4;

    [Tooltip("Width of one arc in canvas units. Height follows the sprite's aspect.")]
    [SerializeField] private float indicatorWidth = 250f;

    [Tooltip("Distance from the screen centre to the top of the arc, in canvas units. Keep it about 0.72 x the width so the arcs sit on one ring around the crosshair.")]
    [SerializeField] private float distanceFromCenter = 181f;

    [Header("Timing")]
    [Tooltip("Seconds an arc stays fully visible after the last hit.")]
    [SerializeField] private float holdTime = 1.2f;

    [Tooltip("Seconds it then takes to fade out.")]
    [SerializeField] private float fadeTime = 0.8f;

    [Tooltip("Scale an arc jumps to on a hit before settling back to 1.")]
    [SerializeField] private float hitPulseScale = 1.12f;

    private class Indicator
    {
        public RectTransform pivot;
        public Image image;
        public uint attackerNetId;
        public Vector3 attackerPosition;
        public float timeLeft;
        public bool active;
    }

    private Indicator[] indicators;
    private Camera viewCamera;

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        PlayerHealthManager.LocalPlayerDamagedFrom += OnDamaged;
    }

    private void OnDisable()
    {
        PlayerHealthManager.LocalPlayerDamagedFrom -= OnDamaged;
        HideAll();
    }

    private void OnDamaged(uint attackerNetId, Vector3 attackerPosition)
    {
        PlayerHealthManager local = PlayerHealthManager.LocalInstance;

        // Self-inflicted damage has no direction worth pointing at.
        if (indicators == null || (local != null && attackerNetId == local.netId))
            return;

        Indicator slot = FindSlot(attackerNetId);

        slot.attackerNetId = attackerNetId;
        slot.attackerPosition = attackerPosition;
        slot.timeLeft = holdTime + fadeTime;
        slot.pivot.localScale = Vector3.one * hitPulseScale;

        if (!slot.active)
        {
            slot.active = true;
            slot.pivot.gameObject.SetActive(true);
        }

        // Point it straight away rather than waiting a frame.
        UpdateIndicator(slot, local, 0f);
    }

    /// <summary>This attacker's arc, else a free one, else the one closest to fading out.</summary>
    private Indicator FindSlot(uint attackerNetId)
    {
        Indicator free = null;
        Indicator oldest = indicators[0];

        foreach (Indicator indicator in indicators)
        {
            if (indicator.active && indicator.attackerNetId == attackerNetId)
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

        PlayerHealthManager local = PlayerHealthManager.LocalInstance;

        // Nothing to point from, or nothing left to warn.
        if (local == null || local.netIsDead)
        {
            HideAll();
            return;
        }

        float deltaTime = Time.unscaledDeltaTime;

        foreach (Indicator indicator in indicators)
        {
            if (indicator.active)
                UpdateIndicator(indicator, local, deltaTime);
        }
    }

    private void UpdateIndicator(Indicator indicator, PlayerHealthManager local, float deltaTime)
    {
        indicator.timeLeft -= deltaTime;

        if (indicator.timeLeft <= 0f)
        {
            indicator.active = false;
            indicator.pivot.gameObject.SetActive(false);
            return;
        }

        // Track the attacker while it still exists here; otherwise keep the hit position.
        if (NetworkClient.spawned.TryGetValue(indicator.attackerNetId, out NetworkIdentity attacker) && attacker != null)
            indicator.attackerPosition = attacker.transform.position;

        Vector3 toAttacker = indicator.attackerPosition - local.transform.position;
        toAttacker.y = 0f;

        Vector3 forward = ViewForward(local.transform);

        // Standing on top of each other gives no usable direction; keep the last one.
        if (toAttacker.sqrMagnitude > 0.0001f && forward.sqrMagnitude > 0.0001f)
        {
            // Positive angle = attacker to the right. UI Z rotation is counter-clockwise,
            // hence the minus, so the arc's pointer swings right as well.
            float angle = Vector3.SignedAngle(forward, toAttacker, Vector3.up);
            indicator.pivot.localEulerAngles = new Vector3(0f, 0f, -angle);
        }

        float alpha = indicator.timeLeft >= fadeTime ? 1f : indicator.timeLeft / Mathf.Max(0.01f, fadeTime);
        Color color = tint;
        color.a *= alpha;
        indicator.image.color = color;

        indicator.pivot.localScale = Vector3.MoveTowards(
            indicator.pivot.localScale, Vector3.one, 1.5f * deltaTime);
    }

    /// <summary>Flattened camera forward, falling back to the character when there is no camera.</summary>
    private Vector3 ViewForward(Transform character)
    {
        if (viewCamera == null || !viewCamera.isActiveAndEnabled)
            viewCamera = Camera.main;

        Vector3 forward = viewCamera != null ? viewCamera.transform.forward : character.forward;
        forward.y = 0f;

        // Looking straight up or down leaves nothing on the ground plane.
        if (forward.sqrMagnitude < 0.0001f && viewCamera != null)
        {
            forward = viewCamera.transform.up;
            forward.y = 0f;
        }

        return forward;
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
        if (indicatorSprite == null)
        {
            Debug.LogWarning("[DamageDirectionHUD] No indicator sprite assigned.");
            return;
        }

        Rect spriteRect = indicatorSprite.rect;
        float height = indicatorWidth * (spriteRect.height / Mathf.Max(1f, spriteRect.width));

        indicators = new Indicator[maxIndicators];

        for (int i = 0; i < maxIndicators; i++)
        {
            // A zero-size pivot at the screen centre; rotating it swings the arc around
            // the crosshair.
            var pivotGo = new GameObject("DamageIndicator " + (i + 1), typeof(RectTransform));
            pivotGo.transform.SetParent(transform, false);

            var pivot = (RectTransform)pivotGo.transform;
            pivot.anchorMin = pivot.anchorMax = new Vector2(0.5f, 0.5f);
            pivot.pivot = new Vector2(0.5f, 0.5f);
            pivot.anchoredPosition = Vector2.zero;
            pivot.sizeDelta = Vector2.zero;

            var imageGo = new GameObject("Arc", typeof(RectTransform));
            imageGo.transform.SetParent(pivot, false);

            var rect = (RectTransform)imageGo.transform;
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 1f);           // hang from the top edge
            rect.anchoredPosition = new Vector2(0f, distanceFromCenter);
            rect.sizeDelta = new Vector2(indicatorWidth, height);

            var image = imageGo.AddComponent<Image>();
            image.sprite = indicatorSprite;
            image.color = tint;
            image.preserveAspect = true;

            // Must never swallow touches meant for the mobile controls underneath.
            image.raycastTarget = false;

            pivotGo.SetActive(false);

            indicators[i] = new Indicator { pivot = pivot, image = image };
        }
    }
}
