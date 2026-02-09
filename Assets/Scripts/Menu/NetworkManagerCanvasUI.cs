using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP; // Michsky UI Namespace
using UnityEngine.SceneManagement;
using Mirror.Discovery; // Mirror Discovery Namespace
using JUTPS; // JU TPS Namespace

namespace Mirror
{
    [DisallowMultipleComponent]
    [AddComponentMenu("Network/Network Manager Modern UI")]
    [RequireComponent(typeof(NetworkDiscovery))]
    public class NetworkManagerModernUI : MonoBehaviour
    {
        NetworkManager manager;
        public NetworkDiscovery networkDiscovery;

        [Header("=== UI PANELS ===")]
        public GameObject mainMenuPanel;
        public GameObject connectionPanel;
        public GameObject hostPanel;
        public GameObject loadingPanel;
        public GameObject settingPanel;
        public ModalWindowManager quitModalWindow;

        [Header("=== MAIN MENU BUTTONS ===")]
        public ButtonManager startButton;
        public ButtonManager joinButton;
        public ButtonManager settingButton;
        public ButtonManager exitButton;

        [Header("=== CONNECTION PANEL ===")]
  
        public ButtonManager connectionExitButton;
        // Status Text Removed as requested

        [Header("=== SERVER DISCOVERY (FRIEND'S LOGIC) ===")]
        public ListView serverListView; // Assign Michsky List View
        public ButtonManager refreshButton; // Assign Refresh Button

        // Dictionary to keep track of servers we already found so we don't add them twice
        private Dictionary<long, GameObject> foundServers = new Dictionary<long, GameObject>();

        [Header("=== HOST PANEL ===")]
        public ButtonManager hostMatchButton;
        public ButtonManager hostbackButton;
        public ButtonManager[] mapButtons;
        public ButtonManager[] gameModeButtons;

        [Header("=== LOADING SCREEN ===")]
        public Image characterImage;
        public ProgressBar progressBar;
        public TextMeshProUGUI loadingText;

        [Header("=== QUIT MODAL ===")]
        public ButtonManager confirmQuitButton;
        public ButtonManager cancelQuitButton;

        [Header("=== SETTINGS ===")]
        public ButtonManager settingBackButton;

        [Header("=== COLORS ===")]
        public Color normalColor = new Color(0.2f, 0.2f, 0.2f, 1f);
        public Color selectedColor = new Color(0.0f, 0.8f, 0.2f, 1f);

        private int selectedMapIndex = -1;
        private int selectedModeIndex = -1;
        private string menuSceneName = "";

        void Awake()
        {
            manager = GetComponent<NetworkManager>();

            // Auto-Assign Network Discovery
            if (networkDiscovery == null) networkDiscovery = GetComponent<NetworkDiscovery>();

            menuSceneName = SceneManager.GetActiveScene().name;

            if (manager == null)
            {
                Debug.LogError("NetworkManager not found!");
                enabled = false;
                return;
            }

            SetupQuitModal();
            SetupButtonListeners();
            SetupDiscoveryEvents();

            ShowMainMenu();
        }

        // =========================================================
        // PART 1: NETWORK DISCOVERY (FRIEND'S FIX)
        // =========================================================

        void SetupDiscoveryEvents()
        {
            if (networkDiscovery != null)
            {
                // Ensure we don't double subscribe
                networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
                networkDiscovery.OnServerFound.AddListener(OnServerFound);
            }
        }

        public void StartDiscoverySearch()
        {
            // Safety Check
            if (serverListView == null || serverListView.itemParent == null)
            {
                Debug.LogError("Server List View or Item Parent is null! Check Inspector.");
                return;
            }

            // 1. Clear the list (Delete old buttons)
            foreach (Transform child in serverListView.itemParent)
            {
                Destroy(child.gameObject);
            }
            foundServers.Clear();

            // 2. Start Mirror Discovery
            if (networkDiscovery != null)
            {
                networkDiscovery.StopDiscovery();
                networkDiscovery.StartDiscovery();
                Debug.Log("🔍 Searching for local servers...");
            }
        }

        public void OnServerFound(ServerResponse info)
        {
            // If we already found this server, skip it
            if (foundServers.ContainsKey(info.serverId)) return;

            // Safety Check
            if (serverListView == null || serverListView.itemParent == null || serverListView.itemPreset == null) return;

            // 1. Instantiate the button manually (Bypassing ListView.cs crash)
            GameObject newItem = Instantiate(serverListView.itemPreset, serverListView.itemParent);

            // 2. Fix "Demo Text" - Set the text to the IP Address
            string serverName = info.EndPoint.Address.ToString();

            // Try to find Michsky Button Manager
            ButtonManager btnManager = newItem.GetComponent<ButtonManager>();
            if (btnManager != null)
            {
                btnManager.buttonText = serverName; // Set Variable
                if (btnManager.normalText != null) btnManager.normalText.text = serverName; // Set TMP directly

                // Add Click Listener
                btnManager.onClick.RemoveAllListeners();
                btnManager.onClick.AddListener(() => ConnectToFoundServer(info));

                // Refresh Michsky UI
                btnManager.UpdateUI();
            }
            else
            {
                // Fallback for standard Unity Buttons
                TextMeshProUGUI txt = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = serverName;

                Button btn = newItem.GetComponentInChildren<Button>();
                if (btn) btn.onClick.AddListener(() => ConnectToFoundServer(info));
            }

            // Add to dictionary so we don't add it again
            foundServers.Add(info.serverId, newItem);
        }

        void ConnectToFoundServer(ServerResponse info)
        {
            if (networkDiscovery != null) networkDiscovery.StopDiscovery();

            manager.StartClient(info.uri);

            ShowLoadingScreen("Connecting to Server...", false);
            StartCoroutine(RealLoadingSequence(false));
        }

        // =========================================================
        // PART 2: LOADING SCREEN (MOBILE OPTIMIZED)
        // =========================================================

        IEnumerator RealLoadingSequence(bool isHost)
        {
            float currentProgress = 0f;
            if (progressBar != null) progressBar.SetValue(0f);

            // Phase 1: Connection
            while (!NetworkClient.isConnected && !NetworkServer.active)
            {
                if (loadingPanel == null) yield break; // Safety stop

                if (loadingText != null) loadingText.text = "Connecting...";
                currentProgress = Mathf.MoveTowards(currentProgress, 30f, Time.deltaTime * 20f);
                if (progressBar != null) progressBar.SetValue(currentProgress);
                yield return null;
            }

            // Phase 2: Scene Load
            string currentScene = SceneManager.GetActiveScene().name;
            while (currentScene == menuSceneName)
            {
                if (loadingPanel == null) yield break;

                if (loadingText != null) loadingText.text = "Loading World...";
                currentScene = SceneManager.GetActiveScene().name;
                currentProgress = Mathf.MoveTowards(currentProgress, 60f, Time.deltaTime * 10f);
                if (progressBar != null) progressBar.SetValue(currentProgress);
                yield return null;
            }

            // Phase 3: Waiting for JU TPS Player to Spawn
            if (loadingText != null) loadingText.text = "Spawning Player...";

            float timeout = 20f;
            float timer = 0f;
            WaitForSeconds fastWait = new WaitForSeconds(0.1f); // Mobile Optimization

            while (JUGameManager.PlayerController == null)
            {
                if (loadingPanel == null) yield break;

                if (currentProgress < 90f)
                {
                    currentProgress += 1f;
                    if (progressBar != null) progressBar.SetValue(currentProgress);
                }

                timer += 0.1f;
                if (timer > timeout && loadingText != null) loadingText.text = "Waiting for Server...";

                yield return fastWait;
            }

            if (loadingText != null) loadingText.text = "Ready!";
            if (progressBar != null) progressBar.SetValue(100f);

            yield return new WaitForSeconds(0.5f);
            HideLoadingScreen();
        }

        // =========================================================
        // PART 3: BUTTONS & UI LOGIC
        // =========================================================

        void SetupButtonListeners()
        {
            if (startButton) startButton.onClick.AddListener(OnStartButtonClick);
            if (joinButton) joinButton.onClick.AddListener(OnJoinButtonClick);
            if (settingButton) settingButton.onClick.AddListener(OnSettingButtonClick);
            if (exitButton) exitButton.onClick.AddListener(OnExitButtonClick);

            if (connectionExitButton) connectionExitButton.onClick.AddListener(OnConnectionExitClick);

            // Refresh Button Listener
            if (refreshButton) refreshButton.onClick.AddListener(StartDiscoverySearch);

            if (hostMatchButton) hostMatchButton.onClick.AddListener(OnHostMatchClick);
            if (confirmQuitButton) confirmQuitButton.onClick.AddListener(OnConfirmQuit);
            if (cancelQuitButton) cancelQuitButton.onClick.AddListener(OnCancelQuit);
            if (settingBackButton) settingBackButton.onClick.AddListener(OnSettingBackClick);
            if (hostbackButton) hostbackButton.onClick.AddListener(OnHostBackClick);

            if (mapButtons != null)
            {
                for (int i = 0; i < mapButtons.Length; i++)
                {
                    int index = i;
                    if (mapButtons[i]) mapButtons[i].onClick.AddListener(() => OnMapSelected(index));
                }
            }

            if (gameModeButtons != null)
            {
                for (int i = 0; i < gameModeButtons.Length; i++)
                {
                    int index = i;
                    if (gameModeButtons[i]) gameModeButtons[i].onClick.AddListener(() => OnGameModeSelected(index));
                }
            }
        }

        void OnStartButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (hostPanel) hostPanel.SetActive(true);
            selectedMapIndex = -1;
            selectedModeIndex = -1;
            ResetMapButtonColors();
            ResetModeButtonColors();
            if (hostMatchButton) hostMatchButton.Interactable(false);
        }

        void OnJoinButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (connectionPanel) connectionPanel.SetActive(true);

            // Auto start searching when opening panel
            StartDiscoverySearch();
        }

        void OnSettingButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (settingPanel) settingPanel.SetActive(true);
        }

        void OnExitButtonClick()
        {
            if (quitModalWindow != null)
            {
                if (!quitModalWindow.gameObject.activeSelf) quitModalWindow.gameObject.SetActive(true);
                quitModalWindow.Open();
            }
        }

        void OnConnectionExitClick()
        {
            if (networkDiscovery != null) networkDiscovery.StopDiscovery();
            if (connectionPanel) connectionPanel.SetActive(false);
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }

        void OnHostMatchClick()
        {
            if (selectedMapIndex < 0 || selectedModeIndex < 0) return;

            ShowLoadingScreen("Starting Server...", false);

#if UNITY_WEBGL
            NetworkServer.listen = false;
#endif
            if (networkDiscovery != null) networkDiscovery.AdvertiseServer();
            manager.StartHost();

            StartCoroutine(RealLoadingSequence(true));
        }

        void OnConfirmQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void OnCancelQuit() { }

        void OnSettingBackClick()
        {
            if (settingPanel) settingPanel.SetActive(false);
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }

        void OnHostBackClick()
        {
            if (hostPanel) hostPanel.SetActive(false);
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }

        void OnMapSelected(int mapIndex)
        {
            selectedMapIndex = mapIndex;
            UpdateMapButtonColors(mapIndex);
            CheckHostReady();
        }

        void OnGameModeSelected(int modeIndex)
        {
            selectedModeIndex = modeIndex;
            UpdateModeButtonColors(modeIndex);
            CheckHostReady();
        }

        void UpdateMapButtonColors(int selectedIndex)
        {
            if (mapButtons == null) return;
            for (int i = 0; i < mapButtons.Length; i++)
            {
                if (mapButtons[i] != null && mapButtons[i].normalImage != null)
                    mapButtons[i].normalImage.color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }

        void UpdateModeButtonColors(int selectedIndex)
        {
            if (gameModeButtons == null) return;
            for (int i = 0; i < gameModeButtons.Length; i++)
            {
                if (gameModeButtons[i] != null && gameModeButtons[i].normalImage != null)
                    gameModeButtons[i].normalImage.color = (i == selectedIndex) ? selectedColor : normalColor;
            }
        }

        void ResetMapButtonColors()
        {
            if (mapButtons == null) return;
            for (int i = 0; i < mapButtons.Length; i++)
            {
                if (mapButtons[i] != null && mapButtons[i].normalImage != null)
                    mapButtons[i].normalImage.color = normalColor;
            }
        }

        void ResetModeButtonColors()
        {
            if (gameModeButtons == null) return;
            for (int i = 0; i < gameModeButtons.Length; i++)
            {
                if (gameModeButtons[i] != null && gameModeButtons[i].normalImage != null)
                    gameModeButtons[i].normalImage.color = normalColor;
            }
        }

        void CheckHostReady()
        {
            bool canHost = (selectedMapIndex >= 0 && selectedModeIndex >= 0);
            if (hostMatchButton) hostMatchButton.Interactable(canHost);
        }

        void SetupQuitModal()
        {
            if (quitModalWindow == null) return;
            quitModalWindow.titleText = "Quit Game";
            quitModalWindow.descriptionText = "Are you sure you want to quit?";
            quitModalWindow.showCancelButton = true;
            quitModalWindow.showConfirmButton = true;
            quitModalWindow.closeOnCancel = true;
            quitModalWindow.closeOnConfirm = true;
            quitModalWindow.startBehaviour = ModalWindowManager.StartBehaviour.Disable;
            quitModalWindow.gameObject.SetActive(false);
        }

        void ShowMainMenu()
        {
            HideAllPanels();
            if (mainMenuPanel) mainMenuPanel.SetActive(true);
        }

        void ShowLoadingScreen(string message, bool showCharacter)
        {
            HideAllPanels();
            if (loadingPanel) loadingPanel.SetActive(true);
            if (loadingText) loadingText.text = message;

            if (characterImage) characterImage.gameObject.SetActive(showCharacter);

            if (progressBar)
            {
                progressBar.isOn = false;
                progressBar.SetValue(0f);
            }
        }

        void HideLoadingScreen()
        {
            if (loadingPanel) loadingPanel.SetActive(false);
            if (progressBar) progressBar.SetValue(0f);
        }

        void HideAllPanels()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (connectionPanel) connectionPanel.SetActive(false);
            if (hostPanel) hostPanel.SetActive(false);
            if (loadingPanel) loadingPanel.SetActive(false);
            if (settingPanel) settingPanel.SetActive(false);
        }
    }
}