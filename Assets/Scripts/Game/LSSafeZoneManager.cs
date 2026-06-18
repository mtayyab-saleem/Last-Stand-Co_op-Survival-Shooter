using UnityEngine;
using Mirror;
using TMPro;

[RequireComponent(typeof(LineRenderer))]
public class LSSafeZoneManager : NetworkBehaviour
{
    [Header("Zone Phases (Timer)")]
    [SyncVar] public float waitTime = 60f;
    [SyncVar] public bool isShrinking = false;

    [Header("Zone Math Settings")]
    [SyncVar] public float currentRadius = 500f;
    [SyncVar] public Vector3 zoneCenter;
    public float shrinkRate = 5f;
    public float minRadius = 10f;

    [Header("Damage Settings")]
    public float outOfZoneDamage = 5f;
    public float damageInterval = 1f;
    private float damageTimer = 0f;

    [Header("UI & Visuals")]
    public TextMeshProUGUI timerText;
    private LineRenderer lineRenderer;
    public int lineSegments = 100; // Increased for a smoother AAA circle

    // Initializes the safe zone center on the server
    public override void OnStartServer()
    {
        zoneCenter = transform.position;
    }

    // Caches the LineRenderer component and sets basic properties
    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = lineSegments + 1;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = 2.0f; // Made line slightly thicker for visibility
        lineRenderer.endWidth = 2.0f;
    }

    // Runs visual rendering and server-side authority logic checks every frame
    void Update()
    {
        UpdateUI();
        DrawCircle();

        if (isServer)
        {
            HandleZoneLogic();
            HandleDamage();
        }
    }

    // Manages timers and shrinks the mathematical zone radius over time
    [Server]
    private void HandleZoneLogic()
    {
        if (!isShrinking)
        {
            waitTime -= Time.deltaTime;
            if (waitTime <= 0)
            {
                isShrinking = true;
            }
        }
        else
        {
            if (currentRadius > minRadius)
            {
                currentRadius -= shrinkRate * Time.deltaTime;
            }
        }
    }

    // Periodically evaluates player distances from the center to apply damage
    [Server]
    private void HandleDamage()
    {
        damageTimer += Time.deltaTime;

        if (damageTimer >= damageInterval)
        {
            damageTimer = 0f;
            if (LSMatchManager.Instance == null) return;

            foreach (var player in LSMatchManager.Instance.players)
            {
                if (player != null && player.isAlive)
                {
                    Vector3 pPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
                    Vector3 zPos = new Vector3(zoneCenter.x, 0, zoneCenter.z);

                    if (Vector3.Distance(pPos, zPos) > currentRadius)
                    {
                        ApplyDamageToPlayer(player.gameObject);
                    }
                }
            }
        }
    }

    // Invokes the JUTPS damage method with required network parameters via reflection
    [Server]
    private void ApplyDamageToPlayer(GameObject playerObj)
    {
        Component health = playerObj.GetComponent("JUHealth");
        if (health != null)
        {
            System.Reflection.MethodInfo doDamageMethod = health.GetType().GetMethod("DoDamage", new System.Type[] { typeof(float), typeof(Transform) });
            if (doDamageMethod != null)
            {
                doDamageMethod.Invoke(health, new object[] { outOfZoneDamage, this.transform });
            }
            else
            {
                System.Reflection.MethodInfo fallbackMethod = health.GetType().GetMethod("DoDamage", new System.Type[] { typeof(float), typeof(GameObject) });
                if (fallbackMethod != null)
                {
                    fallbackMethod.Invoke(health, new object[] { outOfZoneDamage, this.gameObject });
                }
            }
        }
    }

    // Projects the circle points downward onto the terrain surface using raycasting
    private void DrawCircle()
    {
        if (lineRenderer == null) return;

        float angle = 0f;
        for (int i = 0; i < (lineSegments + 1); i++)
        {
            float x = Mathf.Sin(Mathf.Deg2Rad * angle) * currentRadius;
            float z = Mathf.Cos(Mathf.Deg2Rad * angle) * currentRadius;

            // Start raycast from high above the terrain
            Vector3 rayStart = new Vector3(zoneCenter.x + x, zoneCenter.y + 200f, zoneCenter.z + z);
            Vector3 finalPos = rayStart;

            // Raycast down to find the exact terrain height
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 500f))
            {
                finalPos.y = hit.point.y + 0.3f; // Snaps exactly 0.3 units above the ground surface
            }
            else
            {
                finalPos.y = zoneCenter.y;
            }

            lineRenderer.SetPosition(i, finalPos);
            angle += (360f / lineSegments);
        }
    }

    // Syncs the countdown UI and warning messages onto the player's screen
    private void UpdateUI()
    {
        if (timerText == null) return;

        if (!isShrinking)
        {
            int minutes = Mathf.FloorToInt(waitTime / 60);
            int seconds = Mathf.FloorToInt(waitTime % 60);

            if (minutes <= 0 && seconds <= 0)
            {
                timerText.text = "WARNING: Zone is Shrinking!";
                timerText.color = Color.red;
            }
            else
            {
                timerText.text = $"Safe Zone Shrinks In: {minutes:00}:{seconds:00}";
                timerText.color = Color.white;
            }
        }
        else
        {
            if (currentRadius <= minRadius)
            {
                timerText.text = "Safe Zone Final Circle!";
                timerText.color = Color.red;
            }
            else
            {
                timerText.text = "WARNING: Zone is Shrinking!";
                timerText.color = Color.red;
            }
        }
    }
}