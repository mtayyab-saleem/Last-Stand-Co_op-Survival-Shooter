using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP;
using TMPro;

/// <summary>
/// Join Match popup: a scrollable list of LAN servers, one row each.
///
/// Rows are built here rather than cloned from a button prefab, so a long server
/// name wraps and shrinks inside its row instead of stretching the row itself.
/// </summary>
public class ConnectionMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root object toggled on and off. Defaults to this GameObject.")]
    [SerializeField] private GameObject panelRoot;

    [Header("UI References")]
    [Tooltip("Content object of the scroll rect. Rows are added here.")]
    [SerializeField] private RectTransform serverListContent;

    [Tooltip("Shown while the list is empty.")]
    [SerializeField] private TMP_Text emptyStateText;

    [SerializeField] private ButtonManager refreshButton;
    [SerializeField] private ButtonManager closeButton;

    [Header("Row Style")]
    [SerializeField] private TMP_FontAsset rowFont;
    [SerializeField] private float rowHeight = 78f;
    [SerializeField] private float rowSpacing = 8f;
    [SerializeField] private Color rowBackground = new Color32(0x16, 0x1A, 0x20, 0xFF);
    [SerializeField] private Color rowNameColor = new Color32(0xE6, 0xEC, 0xF2, 0xFF);
    [SerializeField] private Color accentColor = new Color32(0x38, 0xD4, 0x72, 0xFF);
    [SerializeField] private float rowNameMaxSize = 26f;
    [SerializeField] private float rowNameMinSize = 15f;

    [Header("Discovery Settings")]
    [Tooltip("How often (seconds) to automatically re-scan for new servers while the panel is open.")]
    [SerializeField] private float autoRefreshInterval = 5f;

    private CustomNetworkDiscovery _networkDiscovery;
    private Coroutine _autoRefreshCoroutine;
    private readonly Dictionary<long, GameObject> _foundServers = new Dictionary<long, GameObject>();

    private bool isOpen;

    public bool IsOpen { get { return isOpen; } }

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        if (refreshButton != null)
        {
            refreshButton.onClick.RemoveListener(StartDiscoverySearch);
            refreshButton.onClick.AddListener(StartDiscoverySearch);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        WireBackdrop();
    }

    private void Start()
    {
        // Deliberately not in Awake. A panel left disabled in the scene only runs
        // Awake when Open() switches it on, so hiding it there would instantly undo
        // the open - which is why the first click appeared to do nothing.
        if (!isOpen)
            panelRoot.SetActive(false);
    }

    /// <summary>Clicking the dimmed area behind the window closes the popup.</summary>
    private void WireBackdrop()
    {
        Transform dim = panelRoot.transform.Find("Dim");
        if (dim == null) return;

        Button backdrop = dim.GetComponent<Button>();
        if (backdrop == null) backdrop = dim.gameObject.AddComponent<Button>();

        backdrop.transition = Selectable.Transition.None;
        backdrop.targetGraphic = dim.GetComponent<Image>();
        backdrop.onClick.RemoveAllListeners();
        backdrop.onClick.AddListener(Close);
    }

    // -------------------------
    // Open / close
    // -------------------------

    public void Toggle()
    {
        if (isOpen) Close();
        else Open();
    }

    public void Open()
    {
        if (isOpen) return;

        isOpen = true;
        panelRoot.SetActive(true);

        // Everything below needs a live GameObject. If a parent is still off, stop
        // here rather than throwing halfway and leaving the panel half-opened.
        if (!isActiveAndEnabled)
        {
            Debug.LogWarning("[ConnectionMenuUI] Panel could not be activated; is a parent object disabled?");
            return;
        }

        InitializeDiscovery();
        StartDiscoverySearch();

        if (_autoRefreshCoroutine != null) StopCoroutine(_autoRefreshCoroutine);
        _autoRefreshCoroutine = StartCoroutine(PeriodicDiscoveryRefresh());
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;

        if (_autoRefreshCoroutine != null)
        {
            StopCoroutine(_autoRefreshCoroutine);
            _autoRefreshCoroutine = null;
        }

        if (_networkDiscovery != null)
            _networkDiscovery.StopDiscovery();

        panelRoot.SetActive(false);
    }

    private void InitializeDiscovery()
    {
        if (NetworkManager.singleton != null)
            _networkDiscovery = NetworkManager.singleton.GetComponent<CustomNetworkDiscovery>();

        if (_networkDiscovery != null)
        {
            _networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
            _networkDiscovery.OnServerFound.AddListener(OnServerFound);
        }
        else
        {
            Debug.LogError("[ConnectionMenuUI] CustomNetworkDiscovery component missing on Mirror NetworkManager!");
        }
    }

    // -------------------------
    // Discovery
    // -------------------------

    public void StartDiscoverySearch()
    {
        InitializeDiscovery();
        ClearList();

        if (_networkDiscovery != null)
        {
            _networkDiscovery.StopDiscovery();
            CancelInvoke(nameof(ExecuteStartDiscovery));
            Invoke(nameof(ExecuteStartDiscovery), 0.15f);
        }
    }

    private void ExecuteStartDiscovery()
    {
        if (gameObject.activeInHierarchy && _networkDiscovery != null && !NetworkServer.active)
        {
            _networkDiscovery.StartDiscovery();
            Debug.Log("[ConnectionMenuUI] Scanning for LAN servers...");
        }
    }

    private IEnumerator PeriodicDiscoveryRefresh()
    {
        while (true)
        {
            yield return new WaitForSeconds(autoRefreshInterval);

            if (!isOpen) yield break;
            if (NetworkServer.active) yield break;

            InitializeDiscovery();
            if (_networkDiscovery == null) continue;

            if (!TryStopDiscovery()) continue;

            yield return new WaitForSeconds(0.15f);

            TryStartDiscovery();
        }
    }

    private bool TryStopDiscovery()
    {
        try
        {
            _networkDiscovery.StopDiscovery();
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ConnectionMenuUI] StopDiscovery failed: " + e.Message);
            return false;
        }
    }

    private void TryStartDiscovery()
    {
        if (!isOpen || NetworkServer.active) return;

        try
        {
            _networkDiscovery.StartDiscovery();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[ConnectionMenuUI] StartDiscovery failed: " + e.Message);
        }
    }

    // -------------------------
    // List
    // -------------------------

    private void ClearList()
    {
        if (serverListContent != null)
        {
            for (int i = serverListContent.childCount - 1; i >= 0; i--)
                Destroy(serverListContent.GetChild(i).gameObject);
        }

        _foundServers.Clear();
        UpdateEmptyState();
    }

    private void UpdateEmptyState()
    {
        if (emptyStateText != null)
            emptyStateText.gameObject.SetActive(_foundServers.Count == 0);
    }

    private void OnServerFound(DiscoveryResponse info)
    {
        if (_foundServers.ContainsKey(info.serverId)) return;
        if (serverListContent == null) return;

        string serverName = string.IsNullOrEmpty(info.lobbyName)
            ? info.EndPoint.Address.ToString()
            : info.lobbyName;

        GameObject row = BuildRowV2(serverName, info);
        _foundServers.Add(info.serverId, row);
        UpdateEmptyState();
    }

    /// <summary>
    /// Green status dot and server name on the left, a green JOIN button on the right.
    /// </summary>
    private GameObject BuildRowV2(string serverName, DiscoveryResponse info)
    {
        var row = new GameObject("ServerRow", typeof(RectTransform));
        row.transform.SetParent(serverListContent, false);
        ((RectTransform)row.transform).sizeDelta = new Vector2(0f, rowHeight);

        var layout = row.AddComponent<LayoutElement>();
        layout.minHeight = rowHeight;
        layout.preferredHeight = rowHeight;

        var rowImage = row.AddComponent<Image>();
        rowImage.color = rowBackground;

        // Whole row is tappable, with a visible pressed state.
        var rowButton = row.AddComponent<Button>();
        rowButton.targetGraphic = rowImage;
        var colors = rowButton.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.25f, 1.25f, 1.25f, 1f);
        colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.fadeDuration = 0.08f;
        rowButton.colors = colors;
        rowButton.onClick.AddListener(delegate { ConnectToFoundServer(info); });

        // Accent stripe down the left edge
        var stripe = new GameObject("Stripe", typeof(RectTransform));
        stripe.transform.SetParent(row.transform, false);
        var stripeRect = (RectTransform)stripe.transform;
        stripeRect.anchorMin = new Vector2(0f, 0f);
        stripeRect.anchorMax = new Vector2(0f, 1f);
        stripeRect.pivot = new Vector2(0f, 0.5f);
        stripeRect.sizeDelta = new Vector2(4f, 0f);
        stripeRect.anchoredPosition = Vector2.zero;
        var stripeImage = stripe.AddComponent<Image>();
        stripeImage.color = accentColor;
        stripeImage.raycastTarget = false;

        // Status dot
        var dot = new GameObject("Dot", typeof(RectTransform));
        dot.transform.SetParent(row.transform, false);
        var dotRect = (RectTransform)dot.transform;
        dotRect.anchorMin = new Vector2(0f, 0.5f);
        dotRect.anchorMax = new Vector2(0f, 0.5f);
        dotRect.pivot = new Vector2(0f, 0.5f);
        dotRect.anchoredPosition = new Vector2(26f, 0f);
        dotRect.sizeDelta = new Vector2(14f, 14f);
        var dotImage = dot.AddComponent<Image>();
        dotImage.color = accentColor;
        dotImage.raycastTarget = false;

        // Server name
        var nameGo = new GameObject("Name", typeof(RectTransform));
        nameGo.transform.SetParent(row.transform, false);
        var nameRect = (RectTransform)nameGo.transform;
        nameRect.anchorMin = new Vector2(0f, 0f);
        nameRect.anchorMax = new Vector2(1f, 1f);
        nameRect.offsetMin = new Vector2(58f, 10f);
        nameRect.offsetMax = new Vector2(-190f, -10f);

        var nameText = nameGo.AddComponent<TextMeshProUGUI>();
        if (rowFont != null) nameText.font = rowFont;
        nameText.text = serverName;
        nameText.color = rowNameColor;
        nameText.alignment = TextAlignmentOptions.MidlineLeft;
        nameText.textWrappingMode = TextWrappingModes.Normal;
        nameText.overflowMode = TextOverflowModes.Ellipsis;
        nameText.enableAutoSizing = true;
        nameText.fontSizeMin = rowNameMinSize;
        nameText.fontSizeMax = rowNameMaxSize;
        nameText.raycastTarget = false;

        // JOIN button
        var joinGo = new GameObject("JoinButton", typeof(RectTransform));
        joinGo.transform.SetParent(row.transform, false);
        var joinRect = (RectTransform)joinGo.transform;
        joinRect.anchorMin = new Vector2(1f, 0.5f);
        joinRect.anchorMax = new Vector2(1f, 0.5f);
        joinRect.pivot = new Vector2(1f, 0.5f);
        joinRect.anchoredPosition = new Vector2(-18f, 0f);
        joinRect.sizeDelta = new Vector2(140f, rowHeight - 24f);

        var joinImage = joinGo.AddComponent<Image>();
        joinImage.color = accentColor;

        var joinButton = joinGo.AddComponent<Button>();
        joinButton.targetGraphic = joinImage;
        joinButton.onClick.AddListener(delegate { ConnectToFoundServer(info); });

        var joinLabelGo = new GameObject("Label", typeof(RectTransform));
        joinLabelGo.transform.SetParent(joinGo.transform, false);
        var joinLabelRect = (RectTransform)joinLabelGo.transform;
        joinLabelRect.anchorMin = Vector2.zero;
        joinLabelRect.anchorMax = Vector2.one;
        joinLabelRect.offsetMin = Vector2.zero;
        joinLabelRect.offsetMax = Vector2.zero;

        var joinLabel = joinLabelGo.AddComponent<TextMeshProUGUI>();
        if (rowFont != null) joinLabel.font = rowFont;
        joinLabel.text = "JOIN";
        joinLabel.fontSize = 22f;
        joinLabel.characterSpacing = 4f;
        joinLabel.color = new Color32(0x06, 0x0A, 0x0D, 0xFF);
        joinLabel.alignment = TextAlignmentOptions.Center;
        joinLabel.raycastTarget = false;

        return row;
    }

    private void ConnectToFoundServer(DiscoveryResponse info)
    {
        if (_autoRefreshCoroutine != null)
        {
            StopCoroutine(_autoRefreshCoroutine);
            _autoRefreshCoroutine = null;
        }

        if (_networkDiscovery != null) _networkDiscovery.StopDiscovery();

        isOpen = false;
        panelRoot.SetActive(false);

        GameUIManager.Instance.ShowLoadingPanel();

        if (NetworkManager.singleton != null)
            NetworkManager.singleton.StartClient(info.uri);
    }
}
