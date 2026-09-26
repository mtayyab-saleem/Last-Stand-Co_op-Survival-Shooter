using UnityEngine;

/// <summary>
/// Stops drawing the props a character carries - guns in hand and on the back, armour -
/// once it is far from the camera. The weapon models are the heaviest meshes in a match:
/// one character's holstered guns are about 42k triangles against 8k for its body, so a
/// few distant bots cost more than the whole forest. Past this distance the guns are a
/// few pixels in the fog; the body still shows.
///
/// Only rendering is switched off (Renderer.forceRenderingOff), so JUTPS enabling,
/// disabling and swapping items is untouched. Added by PlayerNetworkSetup to every
/// character except the local player's.
/// </summary>
public class CharacterPropCulling : MonoBehaviour
{
    /// <summary>Set per device tier by LSPerformanceBootstrap.</summary>
    public static float CullDistance = 60f;

    private const float CheckInterval = 0.25f;

    private MeshRenderer[] props;
    private bool hidden;
    private float nextCheck;

    private void Start()
    {
        // The body is skinned; every plain mesh on a character is a carried prop.
        props = GetComponentsInChildren<MeshRenderer>(true);

        // Spread the checks so all characters do not test on the same frame.
        nextCheck = Time.unscaledTime + Random.value * CheckInterval;
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck)
            return;

        nextCheck = Time.unscaledTime + CheckInterval;

        Camera cam = Camera.main;
        bool far = cam != null &&
                   (cam.transform.position - transform.position).sqrMagnitude > CullDistance * CullDistance;

        if (far != hidden)
            SetHidden(far);
    }

    private void OnDisable()
    {
        if (hidden)
            SetHidden(false);
    }

    private void SetHidden(bool value)
    {
        hidden = value;

        foreach (MeshRenderer prop in props)
        {
            if (prop != null)
                prop.forceRenderingOff = value;
        }
    }
}
