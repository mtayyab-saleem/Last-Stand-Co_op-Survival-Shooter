// ============================================================
// NetworkUIController.cs
// PURPOSE: Manage ALL UI panels for the main menu and connection flow.
//          This script controls: panels show/hide, button listeners,
//          loading screen, quit modal.
//
// REPLACES: NetworkManagerCanvasUI.cs
// KEY FIX: NOT inside Mirror namespace anymore. Own namespace used.
// SINGLE RESPONSIBILITY: UI panels and transitions only.
//                        Network discovery = LobbyUIController's job.
// ============================================================

using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using System.Collections;
using TMPro;
using Michsky.MUIP;
using JUTPS;

// ✅ Our own namespace — NOT "namespace Mirror"
namespace LastStand.UI
{
    /// <summary>
    /// Controls all main menu UI panels and transitions.
    /// Works with LobbyUIController for server discovery.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Last Stand/UI/Network UI Controller")]
    public class NetworkUIController : MonoBehaviour
    {
        // ── Manager References ────────────────────────────────
        [Header("Network Components (on same GameObject)")]
        [SerializeField] private NetworkManager _networkManager;
        [SerializeField] private LobbyUIController _lobbyUI;

        // ── UI Panels ─────────────────────────────────────────
        [Header("UI Panels (assign in Inspector)")]
        [SerializeField] private GameObject _mainMenuPanel;
        [SerializeField] private GameObject _connectionPanel;   // Join panel
        [SerializeField] private GameObject _hostPanel;
        [SerializeField] private GameObject _loadingPanel;
        [SerializeField] private GameObject _settingsPanel;

        // ── Main Menu Buttons ─────────────────────────────────
        [Header("Main Menu Buttons")]
        [SerializeField] private ButtonManager _hostButton;
        [SerializeField] private ButtonManager _joinButton;
        [SerializeField] private ButtonManager _settingsButton;
        [SerializeField] private ButtonManager _exitButton;

        // ── Host Panel ────────────────────────────────────────
        [Header("Host Panel")]
        [SerializeField] private ButtonManager _startMatchButton;
        [SerializeField] private ButtonManager _hostBackButton;
        [SerializeField] private ButtonManager[] _mapButtons;
        [SerializeField] private ButtonManager[] _gameModeButtons;

        // ── Join Panel ────────────────────────────────────────
        [Header("Join Panel")]
        [SerializeField] private ButtonManager _joinBackButton;

        // ── Loading Screen ────────────────────────────────────
        [Header("Loading Screen")]
        [SerializeField] private TextMeshProUGUI _loadingText;
        [SerializeField] private ProgressBar _progressBar;

        // ── Quit Modal ────────────────────────────────────────
        [Header("Quit Modal (Michsky)")]
        [SerializeField] private ModalWindowManager _quitModal;
        [SerializeField] private ButtonManager _confirmQuitButton;
        [SerializeField] private ButtonManager _cancelQuitButton;

        // ── Settings ─────────────────────────────────────────
        [Header("Settings Panel")]
        [SerializeField] private ButtonManager _settingsBackButton;

        // ── Button Colors ─────────────────────────────────────
        [Header("Selection Colors")]
        [SerializeField] private UnityEngine.Color _normalColor = new(0.2f, 0.2f, 0.2f, 1f);
        [SerializeField] private UnityEngine.Color _selectedColor = new(0.0f, 0.8f, 0.2f, 1f);

        // ── Private State ─────────────────────────────────────
        private int _selectedMapIndex = -1;
        private int _selectedModeIndex = -1;
        private string _menuSceneName;

        // ════════════════════════════════════════════════════════
        // UNITY LIFECYCLE
        // ════════════════════════════════════════════════════════

        private void Awake()
        {
            if (_networkManager == null)
                _networkManager = GetComponent<NetworkManager>();

            _menuSceneName = SceneManager.GetActiveScene().name;

            SetupQuitModal();
            SetupButtonListeners();

            // LobbyUI fires this when player clicks a server in the list
            if (_lobbyUI != null)
                _lobbyUI.OnServerSelected += OnServerSelected;

            ShowPanel(_mainMenuPanel);
        }

        private void OnDestroy()
        {
            if (_lobbyUI != null)
                _lobbyUI.OnServerSelected -= OnServerSelected;
        }

        // ════════════════════════════════════════════════════════
        // BUTTON LISTENER SETUP
        // ════════════════════════════════════════════════════════

        private void SetupButtonListeners()
        {
            // Main menu
            _hostButton?.onClick.AddListener(OnHostButtonClick);
            _joinButton?.onClick.AddListener(OnJoinButtonClick);
            _settingsButton?.onClick.AddListener(OnSettingsButtonClick);
            _exitButton?.onClick.AddListener(OnExitButtonClick);

            // Host panel
            _startMatchButton?.onClick.AddListener(OnStartMatchClick);
            _hostBackButton?.onClick.AddListener(OnHostBackClick);

            // Join panel
            _joinBackButton?.onClick.AddListener(OnJoinBackClick);

            // Settings
            _settingsBackButton?.onClick.AddListener(OnSettingsBackClick);

            // Quit modal
            _confirmQuitButton?.onClick.AddListener(OnConfirmQuit);
            _cancelQuitButton?.onClick.AddListener(OnCancelQuit);

            // Map selection buttons
            if (_mapButtons != null)
            {
                for (int i = 0; i < _mapButtons.Length; i++)
                {
                    int index = i; // Capture for lambda
                    _mapButtons[i]?.onClick.AddListener(() => OnMapSelected(index));
                }
            }

            // Game mode buttons
            if (_gameModeButtons != null)
            {
                for (int i = 0; i < _gameModeButtons.Length; i++)
                {
                    int index = i;
                    _gameModeButtons[i]?.onClick.AddListener(() => OnModeSelected(index));
                }
            }
        }

        // ════════════════════════════════════════════════════════
        // BUTTON HANDLERS
        // ════════════════════════════════════════════════════════

        private void OnHostButtonClick()
        {
            // Reset selections when opening host panel
            _selectedMapIndex = -1;
            _selectedModeIndex = -1;
            ResetButtonGroupColors(_mapButtons);
            ResetButtonGroupColors(_gameModeButtons);
            _startMatchButton?.Interactable(false);

            ShowPanel(_hostPanel);
        }

        private void OnJoinButtonClick()
        {
            ShowPanel(_connectionPanel);
            _lobbyUI?.StartSearch(); // Start searching for servers immediately
        }

        private void OnSettingsButtonClick()
        {
            ShowPanel(_settingsPanel);
        }

        private void OnExitButtonClick()
        {
            if (_quitModal != null)
            {
                if (!_quitModal.gameObject.activeSelf)
                    _quitModal.gameObject.SetActive(true);
                _quitModal.Open();
            }
        }

        private void OnStartMatchClick()
        {
            if (_selectedMapIndex < 0 || _selectedModeIndex < 0) return;

            if (_networkManager.GetComponent<Mirror.Discovery.NetworkDiscovery>() is { } discovery)
                discovery.AdvertiseServer(); // Make ourselves discoverable

            _networkManager.StartHost();

            ShowLoadingScreen("Starting server...");
            StartCoroutine(WaitForPlayerAndHideLoading());
        }

        private void OnServerSelected(Mirror.Discovery.ServerResponse serverInfo)
        {
            // Called by LobbyUIController when a server button is clicked
            ShowLoadingScreen("Connecting...");
            StartCoroutine(WaitForPlayerAndHideLoading());
        }

        private void OnHostBackClick() => ShowPanel(_mainMenuPanel);
        private void OnJoinBackClick() { _lobbyUI?.StopSearch(); ShowPanel(_mainMenuPanel); }
        private void OnSettingsBackClick() => ShowPanel(_mainMenuPanel);

        private void OnMapSelected(int index)
        {
            _selectedMapIndex = index;
            UpdateButtonGroupColors(_mapButtons, index);
            CheckIfCanStartMatch();
        }

        private void OnModeSelected(int index)
        {
            _selectedModeIndex = index;
            UpdateButtonGroupColors(_gameModeButtons, index);
            CheckIfCanStartMatch();
        }

        public void OnConfirmQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void OnCancelQuit() { /* Modal closes itself via Michsky settings */ }

        // ════════════════════════════════════════════════════════
        // LOADING SCREEN
        // ════════════════════════════════════════════════════════

        private void ShowLoadingScreen(string message)
        {
            HideAllPanels();
            _loadingPanel?.SetActive(true);

            if (_loadingText != null)
                _loadingText.text = message;

            if (_progressBar != null)
            {
                _progressBar.isOn = false;
                _progressBar.SetValue(0f);
            }
        }

        /// <summary>
        /// Waits until JU TPS player has spawned, then hides loading screen.
        /// This replaces the complex coroutine from the original script.
        /// </summary>
        private IEnumerator WaitForPlayerAndHideLoading()
        {
            float progress = 0f;
            float timeout = 25f;
            float timer = 0f;

            // Phase 1: Wait for network connection
            while (!NetworkClient.isConnected && !NetworkServer.active)
            {
                progress = Mathf.MoveTowards(progress, 30f, Time.deltaTime * 20f);
                SetLoadingProgress(progress, "Connecting...");
                timer += Time.deltaTime;
                if (timer > timeout) { OnLoadingTimeout(); yield break; }
                yield return null;
            }

            // Phase 2: Wait for scene to change from menu scene
            while (SceneManager.GetActiveScene().name == _menuSceneName)
            {
                progress = Mathf.MoveTowards(progress, 60f, Time.deltaTime * 10f);
                SetLoadingProgress(progress, "Loading world...");
                yield return null;
            }

            // Phase 3: Wait for JU TPS player controller to spawn
            timer = 0f;
            while (JUGameManager.PlayerController == null)
            {
                if (progress < 90f) progress += 0.5f;
                SetLoadingProgress(progress, "Spawning player...");
                timer += Time.deltaTime;
                if (timer > timeout) { OnLoadingTimeout(); yield break; }
                yield return new WaitForSeconds(0.1f); // Check every 0.1s (not every frame)
            }

            // Done!
            SetLoadingProgress(100f, "Ready!");
            yield return new WaitForSeconds(0.4f);
            HideLoadingScreen();
        }

        private void SetLoadingProgress(float value, string message)
        {
            _progressBar?.SetValue(value);
            if (_loadingText != null) _loadingText.text = message;
        }

        private void HideLoadingScreen()
        {
            _loadingPanel?.SetActive(false);
        }

        private void OnLoadingTimeout()
        {
            Debug.LogError("[NetworkUIController] Loading timed out! Returning to main menu.");
            _networkManager.StopHost();
            _networkManager.StopClient();
            ShowPanel(_mainMenuPanel);
        }

        // ════════════════════════════════════════════════════════
        // PANEL MANAGEMENT
        // ════════════════════════════════════════════════════════

        private void ShowPanel(GameObject panelToShow)
        {
            HideAllPanels();
            panelToShow?.SetActive(true);
        }

        private void HideAllPanels()
        {
            _mainMenuPanel?.SetActive(false);
            _connectionPanel?.SetActive(false);
            _hostPanel?.SetActive(false);
            _loadingPanel?.SetActive(false);
            _settingsPanel?.SetActive(false);
        }

        // ════════════════════════════════════════════════════════
        // BUTTON COLOR HELPERS
        // ════════════════════════════════════════════════════════

        private void CheckIfCanStartMatch()
        {
            bool ready = _selectedMapIndex >= 0 && _selectedModeIndex >= 0;
            _startMatchButton?.Interactable(ready);
        }

        private void UpdateButtonGroupColors(ButtonManager[] buttons, int selectedIndex)
        {
            if (buttons == null) return;
            for (int i = 0; i < buttons.Length; i++)
            {
                if (buttons[i]?.normalImage != null)
                    buttons[i].normalImage.color = (i == selectedIndex) ? _selectedColor : _normalColor;
            }
        }

        private void ResetButtonGroupColors(ButtonManager[] buttons)
        {
            if (buttons == null) return;
            foreach (var btn in buttons)
                if (btn?.normalImage != null) btn.normalImage.color = _normalColor;
        }

        // ════════════════════════════════════════════════════════
        // QUIT MODAL SETUP
        // ════════════════════════════════════════════════════════

        private void SetupQuitModal()
        {
            if (_quitModal == null) return;

            _quitModal.titleText = "Quit Game";
            _quitModal.descriptionText = "Are you sure you want to quit?";
            _quitModal.showCancelButton = true;
            _quitModal.showConfirmButton = true;
            _quitModal.closeOnCancel = true;
            _quitModal.closeOnConfirm = true;
            _quitModal.startBehaviour = ModalWindowManager.StartBehaviour.Disable;
            _quitModal.gameObject.SetActive(false);
        }
    }
}