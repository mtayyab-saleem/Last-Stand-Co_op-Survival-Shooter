using TMPro;
using UnityEngine;

/// <summary>
/// World space name plate shown above a player.
/// Only teammates see it: the plate stays hidden for enemies and for the
/// local player's own character. Text format is "PlayerName - T1 - #2".
/// Refreshes are driven by LSPlayer.OnTeamDataChanged, which the lobby
/// SyncList callback and the team SyncVar hooks both raise.
/// </summary>
[DisallowMultipleComponent]
public class PlayerOverheadUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Lobby manager holding the synced team roster. Resolved automatically when empty.")]
    [SerializeField] private LSMatchManager lobbyManager;

    [Tooltip("Player this name plate belongs to. Resolved automatically when empty.")]
    [SerializeField] private LSPlayer owner;

    [Tooltip("Optional. A world space canvas is built at runtime when this is empty.")]
    [SerializeField] private Canvas overheadCanvas;

    [Tooltip("Optional. Created together with the runtime canvas when empty.")]
    [SerializeField] private TMP_Text label;

    [Tooltip("Font asset for the plate. Uses the project UI font when assigned.")]
    [SerializeField] private TMP_FontAsset labelFont;

    [Header("Placement")]
    [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.1f, 0f);
    [SerializeField] private float canvasScale = 0.005f;
    [SerializeField] private Vector2 canvasSize = new Vector2(320f, 140f);
    [SerializeField] private float fontSize = 28f;
    [SerializeField] private Color teammateColor = new Color(0.35f, 1f, 0.45f, 1f);

    private Transform cameraTransform;

    private void Awake()
    {
        if (owner == null)
            owner = GetComponentInParent<LSPlayer>();

        if (overheadCanvas == null)
            BuildRuntimeCanvas();

        SetVisible(false);
    }

    private void OnEnable()
    {
        LSPlayer.OnTeamDataChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        LSPlayer.OnTeamDataChanged -= Refresh;
    }

    /// <summary>
    /// Shows the plate only when this player is a live teammate of the local player.
    /// </summary>
public void Refresh()
    {
        if (lobbyManager == null)
            lobbyManager = LSMatchManager.Instance;

        LSPlayer localPlayer = LSPlayer.LocalInstance;

        bool isTeammate =
            owner != null &&
            localPlayer != null &&
            owner != localPlayer &&
            owner.teamID > 0 &&
            owner.teamID == localPlayer.teamID;

        SetVisible(isTeammate);

        if (isTeammate && label != null)
        {
            // Name on the first line, team and slot smaller underneath:
            //   Hassan
            //   T1 - #2
            label.text = $"{owner.playerName}\n<size=75%>T{owner.teamID} - #{owner.teamMemberIndex}</size>";
        }
    }

    private void LateUpdate()
    {
        if (overheadCanvas == null || !overheadCanvas.enabled)
            return;

        if (cameraTransform == null)
        {
            Camera activeCamera = Camera.main;

            if (activeCamera == null)
                return;

            cameraTransform = activeCamera.transform;
        }

        // Keep the plate above the character and always facing the viewer.
        Transform canvasTransform = overheadCanvas.transform;
        canvasTransform.position = transform.position + worldOffset;

        Vector3 awayFromCamera = canvasTransform.position - cameraTransform.position;

        // LookRotation logs an error on a zero vector, which happens when the camera
        // sits exactly on the plate (first person, or a camera snapped inside the head).
        if (awayFromCamera.sqrMagnitude > 0.0001f)
        {
            canvasTransform.rotation = Quaternion.LookRotation(awayFromCamera);
        }
    }

    private void SetVisible(bool visible)
    {
        if (overheadCanvas != null)
            overheadCanvas.enabled = visible;
    }

    /// <summary>
    /// Builds the world space canvas so the player prefab needs no manual UI setup.
    /// </summary>
private void BuildRuntimeCanvas()
    {
        GameObject canvasObject = new GameObject("OverheadUI", typeof(RectTransform), typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);

        overheadCanvas = canvasObject.GetComponent<Canvas>();
        overheadCanvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect = (RectTransform)canvasObject.transform;
        canvasRect.localPosition = worldOffset;
        canvasRect.localRotation = Quaternion.identity;
        canvasRect.sizeDelta = canvasSize;
        canvasRect.localScale = Vector3.one * canvasScale;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform));
        labelObject.transform.SetParent(canvasObject.transform, false);

        TextMeshProUGUI text = labelObject.AddComponent<TextMeshProUGUI>();

        // Match the rest of the game's UI instead of the TMP fallback font.
        if (labelFont != null)
            text.font = labelFont;

        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = fontSize;
        text.color = teammateColor;
        text.richText = true;
        text.raycastTarget = false;
        label = text;

        RectTransform labelRect = text.rectTransform;
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;
    }
}
