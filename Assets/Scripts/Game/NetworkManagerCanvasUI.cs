using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using Michsky.MUIP;
using UnityEngine.SceneManagement;
using Mirror.Discovery;
using JUTPS;

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

        [Header("=== SERVER DISCOVERY ===")]
        public ListView serverListView;
        public ButtonManager refreshButton;

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
            if (networkDiscovery == null) networkDiscovery = GetComponent<NetworkDiscovery>();

            menuSceneName = SceneManager.GetActiveScene().name;

            if (manager == null) { Debug.LogError("NetworkManager not found!"); enabled = false; return; }

            SetupQuitModal();
            SetupButtonListeners();
            SetupDiscoveryEvents();
            ShowMainMenu();
        }

        // =========================================================
        // PART 1: SERVER DISCOVERY
        // =========================================================

        void SetupDiscoveryEvents()
        {
            if (networkDiscovery == null) return;
            networkDiscovery.OnServerFound.RemoveListener(OnServerFound);
            networkDiscovery.OnServerFound.AddListener(OnServerFound);
        }

        public void StartDiscoverySearch()
        {
            if (serverListView == null || serverListView.itemParent == null) return;

            foreach (Transform child in serverListView.itemParent)
                Destroy(child.gameObject);
            foundServers.Clear();

            if (networkDiscovery != null)
            {
                networkDiscovery.StopDiscovery();
                networkDiscovery.StartDiscovery();
            }
        }

        public void OnServerFound(ServerResponse info)
        {
            if (foundServers.ContainsKey(info.serverId)) return;
            if (serverListView?.itemParent == null || serverListView.itemPreset == null) return;

            // Instantiate manually to avoid a crash in Michsky's ListView.AddItem()
            GameObject newItem = Instantiate(serverListView.itemPreset, serverListView.itemParent);
            string serverAddress = info.EndPoint.Address.ToString();

            ButtonManager btn = newItem.GetComponent<ButtonManager>();
            if (btn != null)
            {
                btn.buttonText = serverAddress;
                if (btn.normalText != null) btn.normalText.text = serverAddress;
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => ConnectToFoundServer(info));
                btn.UpdateUI();
            }
            else
            {
                var txt = newItem.GetComponentInChildren<TextMeshProUGUI>();
                if (txt) txt.text = serverAddress;
                var button = newItem.GetComponentInChildren<Button>();
                if (button) button.onClick.AddListener(() => ConnectToFoundServer(info));
            }

            foundServers.Add(info.serverId, newItem);
        }

        void ConnectToFoundServer(ServerResponse info)
        {
            networkDiscovery?.StopDiscovery();
            manager.StartClient(info.uri);
            ShowLoadingScreen("Connecting to Server...", false);
            StartCoroutine(RealLoadingSequence(false));
        }

        // =========================================================
        // PART 2: LOADING SCREEN
        // =========================================================

        IEnumerator RealLoadingSequence(bool isHost)
        {
            float progress = 0f;
            if (progressBar != null) progressBar.SetValue(0f);

            // Phase 1: Wait for connection
            while (!NetworkClient.isConnected && !NetworkServer.active)
            {
                if (loadingPanel == null) yield break;
                if (loadingText != null) loadingText.text = "Connecting...";
                progress = Mathf.MoveTowards(progress, 30f, Time.deltaTime * 20f);
                if (progressBar != null) progressBar.SetValue(progress);
                yield return null;
            }

            // Phase 2: Wait for game scene to load
            string currentScene = SceneManager.GetActiveScene().name;
            while (currentScene == menuSceneName)
            {
                if (loadingPanel == null) yield break;
                if (loadingText != null) loadingText.text = "Loading World...";
                currentScene = SceneManager.GetActiveScene().name;
                progress = Mathf.MoveTowards(progress, 60f, Time.deltaTime * 10f);
                if (progressBar != null) progressBar.SetValue(progress);
                yield return null;
            }

            // Phase 3: Wait for JU TPS player to spawn
            if (loadingText != null) loadingText.text = "Spawning Player...";

            float timeout = 20f, timer = 0f;
            WaitForSeconds wait = new WaitForSeconds(0.1f);

            while (JUGameManager.PlayerController == null)
            {
                if (loadingPanel == null) yield break;
                if (progress < 90f) { progress += 1f; if (progressBar != null) progressBar.SetValue(progress); }
                timer += 0.1f;
                if (timer > timeout && loadingText != null) loadingText.text = "Waiting for Server...";
                yield return wait;
            }

            if (loadingText != null) loadingText.text = "Ready!";
            if (progressBar != null) progressBar.SetValue(100f);
            yield return new WaitForSeconds(0.5f);
            HideLoadingScreen();
        }

        // =========================================================
        // PART 3: BUTTON LISTENERS & HANDLERS
        // =========================================================

        void SetupButtonListeners()
        {
            if (startButton) startButton.onClick.AddListener(OnStartButtonClick);
            if (joinButton) joinButton.onClick.AddListener(OnJoinButtonClick);
            if (settingButton) settingButton.onClick.AddListener(OnSettingButtonClick);
            if (exitButton) exitButton.onClick.AddListener(OnExitButtonClick);
            if (connectionExitButton) connectionExitButton.onClick.AddListener(OnConnectionExitClick);
            if (refreshButton) refreshButton.onClick.AddListener(StartDiscoverySearch);
            if (hostMatchButton) hostMatchButton.onClick.AddListener(OnHostMatchClick);
            if (confirmQuitButton) confirmQuitButton.onClick.AddListener(OnConfirmQuit);
            if (cancelQuitButton) cancelQuitButton.onClick.AddListener(OnCancelQuit);
            if (settingBackButton) settingBackButton.onClick.AddListener(OnSettingBackClick);
            if (hostbackButton) hostbackButton.onClick.AddListener(OnHostBackClick);

            if (mapButtons != null)
                for (int i = 0; i < mapButtons.Length; i++)
                { int idx = i; if (mapButtons[i]) mapButtons[i].onClick.AddListener(() => OnMapSelected(idx)); }

            if (gameModeButtons != null)
                for (int i = 0; i < gameModeButtons.Length; i++)
                { int idx = i; if (gameModeButtons[i]) gameModeButtons[i].onClick.AddListener(() => OnGameModeSelected(idx)); }
        }

        void OnStartButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (hostPanel) hostPanel.SetActive(true);
            selectedMapIndex = -1; selectedModeIndex = -1;
            ResetMapButtonColors(); ResetModeButtonColors();
            if (hostMatchButton) hostMatchButton.Interactable(false);
        }

        void OnJoinButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (connectionPanel) connectionPanel.SetActive(true);
            StartDiscoverySearch();
        }

        void OnSettingButtonClick()
        {
            if (mainMenuPanel) mainMenuPanel.SetActive(false);
            if (settingPanel) settingPanel.SetActive(true);
        }

        void OnExitButtonClick()
        {
            if (quitModalWindow == null) return;
            if (!quitModalWindow.gameObject.activeSelf) quitModalWindow.gameObject.SetActive(true);
            quitModalWindow.Open();
        }

        void OnConnectionExitClick()
        {
            networkDiscovery?.StopDiscovery();
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
            networkDiscovery?.AdvertiseServer();
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

        void UpdateMapButtonColors(int selected)
        {
            if (mapButtons == null) return;
            for (int i = 0; i < mapButtons.Length; i++)
                if (mapButtons[i]?.normalImage != null)
                    mapButtons[i].normalImage.color = (i == selected) ? selectedColor : normalColor;
        }

        void UpdateModeButtonColors(int selected)
        {
            if (gameModeButtons == null) return;
            for (int i = 0; i < gameModeButtons.Length; i++)
                if (gameModeButtons[i]?.normalImage != null)
                    gameModeButtons[i].normalImage.color = (i == selected) ? selectedColor : normalColor;
        }

        void ResetMapButtonColors()
        {
            if (mapButtons == null) return;
            for (int i = 0; i < mapButtons.Length; i++)
                if (mapButtons[i]?.normalImage != null) mapButtons[i].normalImage.color = normalColor;
        }

        void ResetModeButtonColors()
        {
            if (gameModeButtons == null) return;
            for (int i = 0; i < gameModeButtons.Length; i++)
                if (gameModeButtons[i]?.normalImage != null) gameModeButtons[i].normalImage.color = normalColor;
        }

        void CheckHostReady()
        {
            if (hostMatchButton) hostMatchButton.Interactable(selectedMapIndex >= 0 && selectedModeIndex >= 0);
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

        void ShowMainMenu() { HideAllPanels(); if (mainMenuPanel) mainMenuPanel.SetActive(true); }

        void ShowLoadingScreen(string message, bool showCharacter)
        {
            HideAllPanels();
            if (loadingPanel) loadingPanel.SetActive(true);
            if (loadingText) loadingText.text = message;
            if (characterImage) characterImage.gameObject.SetActive(showCharacter);
            if (progressBar) { progressBar.isOn = false; progressBar.SetValue(0f); }
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