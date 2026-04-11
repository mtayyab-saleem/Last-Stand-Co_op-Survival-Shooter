using UnityEngine;
using Mirror;
using Mirror.Discovery;
using Michsky.MUIP;

public class HostMenuUI : MonoBehaviour
{
    [Header("Network Components")]
    [SerializeField] private NetworkDiscovery networkDiscovery;

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

    private int _selectedMapIndex = -1; // 0: Arena, 1: Battle Royale
    private int _selectedModeIndex = -1; // 0: Solo, 1: Duo, 2: Squad

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
        // Map Callbacks
        if (mapArena) mapArena.onClick.AddListener(() => SelectMap(0));
        if (mapBattleRoyale) mapBattleRoyale.onClick.AddListener(() => SelectMap(1));

        // Mode Callbacks
        if (modeSolo) modeSolo.onClick.AddListener(() => SelectMode(0));
        if (modeDuo) modeDuo.onClick.AddListener(() => SelectMode(1));
        if (modeSquad) modeSquad.onClick.AddListener(() => SelectMode(2));

        if (hostMatchButton) hostMatchButton.onClick.AddListener(OnHostMatchClick);
        if (backButton) backButton.onClick.AddListener(OnBackClick);
    }

    private void SelectMap(int index)
    {
        _selectedMapIndex = index;
        UpdateUI();
    }

    private void SelectMode(int index)
    {
        _selectedModeIndex = index;
        UpdateUI();
    }

    private void UpdateUI()
    {
        // Update Maps
        if (mapArena) SetColor(mapArena, _selectedMapIndex == 0);
        if (mapBattleRoyale) SetColor(mapBattleRoyale, _selectedMapIndex == 1);

        // Update Modes
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
        GameUIManager.Instance.ShowLoadingPanel();

        if (networkDiscovery != null) networkDiscovery.AdvertiseServer();

        if (Mirror.NetworkManager.singleton != null)
        {
            Mirror.NetworkManager.singleton.StartHost();
        }
    }

    private void OnBackClick() => GameUIManager.Instance.ShowMainMenu();
}