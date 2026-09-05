using UnityEngine;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP;

public class HostMenuUI : MonoBehaviour
{
    [Header("Network Components")]
    [SerializeField] private CustomNetworkDiscovery networkDiscovery;

    [Header("Host Controls")]
    [SerializeField] private ButtonManager hostMatchButton;
    [SerializeField] private ButtonManager backButton;

    [Header("Map Selection")]
    [SerializeField] private ButtonManager mapArena;
    [SerializeField] private ButtonManager mapBattleRoyale;

    [Header("Mode Selection")]
    [SerializeField] private ButtonManager modeSolo;
    [SerializeField] private ButtonManager modeDuo;
    [SerializeField] private ButtonManager modeSquad;

    [Header("UI Colors")]
    [SerializeField] private Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    [SerializeField] private Color selectedColor = new Color(0.0f, 0.8f, 0.2f, 1f);

    private int _selectedMapIndex = -1;
    private int _selectedModeIndex = -1;

    private void Start()
    {
        SetupButtons();
    }

    private void OnEnable()
    {
        _selectedMapIndex = -1;
        _selectedModeIndex = -1;
        UpdateUI();
        ValidateHostButton();
    }

    private void SetupButtons()
    {
        if (mapArena) mapArena.onClick.AddListener(() => SelectMap(0));
        if (mapBattleRoyale) mapBattleRoyale.onClick.AddListener(() => SelectMap(1));

        if (modeSolo) modeSolo.onClick.AddListener(() => SelectMode(0));
        if (modeDuo) modeDuo.onClick.AddListener(() => SelectMode(1));
        if (modeSquad) modeSquad.onClick.AddListener(() => SelectMode(2));

        if (hostMatchButton) hostMatchButton.onClick.AddListener(OnHostMatchClick);
        if (backButton) backButton.onClick.AddListener(OnBackClick);
    }

    private void SelectMap(int index) { _selectedMapIndex = index; UpdateUI(); }
    private void SelectMode(int index) { _selectedModeIndex = index; UpdateUI(); }

    private void UpdateUI()
    {
        if (mapArena) SetColor(mapArena, _selectedMapIndex == 0);
        if (mapBattleRoyale) SetColor(mapBattleRoyale, _selectedMapIndex == 1);
        if (modeSolo) SetColor(modeSolo, _selectedModeIndex == 0);
        if (modeDuo) SetColor(modeDuo, _selectedModeIndex == 1);
        if (modeSquad) SetColor(modeSquad, _selectedModeIndex == 2);
        ValidateHostButton();
    }

    private void SetColor(ButtonManager btn, bool isSelected)
    {
        if (btn.normalImage != null)
            btn.normalImage.color = isSelected ? selectedColor : normalColor;
    }

    private void ValidateHostButton()
    {
        bool isReady = (_selectedMapIndex >= 0 && _selectedModeIndex >= 0);
        if (hostMatchButton) hostMatchButton.Interactable(isReady);
    }

    private void OnHostMatchClick()
    {
        PlayerPrefs.SetInt("HostSelectedMode", _selectedModeIndex);
        PlayerPrefs.SetInt("HostSelectedMap", _selectedMapIndex);
        PlayerPrefs.Save();

        // Hand the coroutine to GameUIManager so it survives panel deactivation.
        GameUIManager.Instance.StartCoroutine(StartHostSequence());
        GameUIManager.Instance.ShowLoadingPanel();
    }

    private System.Collections.IEnumerator StartHostSequence()
    {
        // --- Step 1: Stop any lingering discovery/network from a previous session ---
        CustomNetworkDiscovery discovery = GetDiscovery();
        if (discovery != null) discovery.StopDiscovery();

        // Stop an already-running host cleanly before starting again.
        if (Mirror.NetworkServer.active)
        {
            Mirror.NetworkManager.singleton.StopHost();
            // Give Mirror enough time to fully tear down the previous session.
            yield return new WaitForSeconds(1.0f);
        }

        // --- Step 2: Start host ---
        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StartHost();
            Debug.Log("[HostMenuUI] StartHost called.");
        }

        // --- Step 3: Wait longer than before so Mirror's transport and discovery
        //             internal state are fully ready on a SECOND run. ---
        yield return new WaitForSeconds(1.0f);

        // Re-fetch in case the reference was invalidated by a scene reload.
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
    /// Returns the NetworkDiscovery, preferring the serialized reference but
    /// falling back to a live lookup so stale Inspector refs don't break reconnects.
    /// </summary>
    private CustomNetworkDiscovery GetDiscovery()
    {
        if (networkDiscovery != null) return networkDiscovery;

        if (Mirror.NetworkManager.singleton != null)
            networkDiscovery = Mirror.NetworkManager.singleton.GetComponent<CustomNetworkDiscovery>();

        return networkDiscovery;
    }

    private void OnBackClick() => GameUIManager.Instance.ShowMainMenu();
}