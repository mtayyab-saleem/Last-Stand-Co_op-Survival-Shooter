using UnityEngine;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP;

/// <summary>
/// Host Match popup: pick a match mode and a match type, then start hosting.
///
/// It is a centred popup rather than a separate screen, so opening it leaves the
/// menu behind it visible and nothing else has to be torn down.
/// </summary>
public class HostMenuUI : MonoBehaviour
{
    [Header("Panel")]
    [Tooltip("Root object toggled on and off. Defaults to this GameObject.")]
    [SerializeField] private GameObject panelRoot;

    [Header("Network Components")]
    [SerializeField] private CustomNetworkDiscovery networkDiscovery;

    [Header("Buttons")]
    [SerializeField] private ButtonManager startHostButton;
    [SerializeField] private ButtonManager closeButton;

    [Header("Cards")]
    [Tooltip("Order must match LSMatchManager.GameMode: Solo, Duo, Squad.")]
    [SerializeField] private UISelectionCard[] modeCards;

    [Tooltip("Battle Royale, Battle Arena.")]
    [SerializeField] private UISelectionCard[] typeCards;

    private int selectedModeIndex = 0;
    private int selectedTypeIndex = 0;
    private bool isOpen;

    public bool IsOpen { get { return isOpen; } }

    private void Awake()
    {
        if (panelRoot == null)
            panelRoot = gameObject;

        SetupCards();

        if (startHostButton != null)
        {
            startHostButton.onClick.RemoveListener(OnHostMatchClick);
            startHostButton.onClick.AddListener(OnHostMatchClick);
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

        UnityEngine.UI.Button backdrop = dim.GetComponent<UnityEngine.UI.Button>();
        if (backdrop == null) backdrop = dim.gameObject.AddComponent<UnityEngine.UI.Button>();

        backdrop.transition = UnityEngine.UI.Selectable.Transition.None;
        backdrop.targetGraphic = dim.GetComponent<UnityEngine.UI.Image>();
        backdrop.onClick.RemoveAllListeners();
        backdrop.onClick.AddListener(Close);
    }

    private void SetupCards()
    {
        for (int i = 0; i < modeCards.Length; i++)
        {
            int index = i;
            if (modeCards[i] == null) continue;
            modeCards[i].Button.onClick.RemoveAllListeners();
            modeCards[i].Button.onClick.AddListener(delegate { SelectMode(index); });
        }

        for (int i = 0; i < typeCards.Length; i++)
        {
            int index = i;
            if (typeCards[i] == null) continue;
            typeCards[i].Button.onClick.RemoveAllListeners();
            typeCards[i].Button.onClick.AddListener(delegate { SelectType(index); });
        }
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

        // Start from a valid pick so Start Host is usable straight away.
        SelectMode(Mathf.Clamp(selectedModeIndex, 0, Mathf.Max(0, modeCards.Length - 1)));
        SelectType(Mathf.Clamp(selectedTypeIndex, 0, Mathf.Max(0, typeCards.Length - 1)));
    }

    public void Close()
    {
        if (!isOpen) return;

        isOpen = false;
        panelRoot.SetActive(false);
    }

    // -------------------------
    // Selection
    // -------------------------

    public void SelectMode(int index)
    {
        selectedModeIndex = index;

        for (int i = 0; i < modeCards.Length; i++)
            if (modeCards[i] != null) modeCards[i].SetSelected(i == index);

        RefreshStartButton();
    }

    public void SelectType(int index)
    {
        selectedTypeIndex = index;

        for (int i = 0; i < typeCards.Length; i++)
            if (typeCards[i] != null) typeCards[i].SetSelected(i == index);

        RefreshStartButton();
    }

    private void RefreshStartButton()
    {
        bool ready = selectedModeIndex >= 0 && selectedTypeIndex >= 0;

        if (startHostButton != null)
            startHostButton.Interactable(ready);
    }

    // -------------------------
    // Hosting
    // -------------------------

    private void OnHostMatchClick()
    {
        if (selectedModeIndex < 0 || selectedTypeIndex < 0)
            return;

        // LSMatchManager reads this on OnStartServer to pick the team rules.
        PlayerPrefs.SetInt("HostSelectedMode", selectedModeIndex);
        PlayerPrefs.SetInt("HostSelectedMap", selectedTypeIndex);
        PlayerPrefs.Save();

        Close();

        // Handed to GameUIManager so the coroutine survives this panel being hidden.
        GameUIManager.Instance.StartCoroutine(StartHostSequence());
        GameUIManager.Instance.ShowLoadingPanel();
    }

    private System.Collections.IEnumerator StartHostSequence()
    {
        // --- Step 1: stop any lingering discovery/network from a previous session ---
        CustomNetworkDiscovery discovery = GetDiscovery();
        if (discovery != null) discovery.StopDiscovery();

        if (NetworkServer.active)
        {
            NetworkManager.singleton.StopHost();
            // Give Mirror enough time to fully tear down the previous session.
            yield return new WaitForSeconds(1.0f);
        }

        // --- Step 2: start host ---
        if (NetworkManager.singleton != null)
        {
            NetworkManager.singleton.StartHost();
            Debug.Log("[HostMenuUI] StartHost called.");
        }

        // --- Step 3: wait so Mirror's transport and discovery are fully ready ---
        yield return new WaitForSeconds(1.0f);

        discovery = GetDiscovery();
        if (discovery != null)
        {
            discovery.AdvertiseServer();
            Debug.Log("[HostMenuUI] Server advertised on LAN.");
        }
        else
        {
            Debug.LogError("[HostMenuUI] Could not find NetworkDiscovery to advertise server!");
        }
    }

    /// <summary>
    /// Prefers the serialized reference but falls back to a live lookup, so a stale
    /// Inspector reference does not break reconnects.
    /// </summary>
    private CustomNetworkDiscovery GetDiscovery()
    {
        if (networkDiscovery != null) return networkDiscovery;

        if (NetworkManager.singleton != null)
            networkDiscovery = NetworkManager.singleton.GetComponent<CustomNetworkDiscovery>();

        return networkDiscovery;
    }
}
