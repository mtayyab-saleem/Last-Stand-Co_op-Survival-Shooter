using UnityEngine;

/// <summary>
/// The central router for all multiplayer menus. 
/// Specific panel scripts (like MainMenuUI or HostMenuUI) call these methods to navigate smoothly.
/// </summary>
[DisallowMultipleComponent]
public class GameUIManager : MonoBehaviour
{
    [Header("Core UI Panels")]
    [SerializeField] private GameObject _mainMenuPanel;
    [SerializeField] private GameObject _connectionPanel;
    [SerializeField] private GameObject _hostPanel;
    [SerializeField] private GameObject _loadingPanel;
    [SerializeField] private GameObject _settingPanel;

    // Optimized Singleton pattern to eliminate expensive FindFirstObjectByType calls across all UI scripts (Rule 6)
    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {
        // Enforce the Singleton pattern
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Debug.LogWarning("[GameUIManager] Duplicate instance found and destroyed.");
            Destroy(gameObject);
            return;
        }

        // Ensure we always start at the Main Menu when the scene loads
        ShowMainMenu();
    }

    /// <summary>
    /// Disables all panels to provide a clean slate for menu transitions.
    /// </summary>
    public void HideAllPanels()
    {
        if (_mainMenuPanel) _mainMenuPanel.SetActive(false);
        if (_connectionPanel) _connectionPanel.SetActive(false);
        if (_hostPanel) _hostPanel.SetActive(false);
        if (_loadingPanel) _loadingPanel.SetActive(false);
        if (_settingPanel) _settingPanel.SetActive(false);
    }

    public void ShowMainMenu()
    {
        HideAllPanels();
        if (_mainMenuPanel) _mainMenuPanel.SetActive(true);
    }

    public void ShowConnectionPanel()
    {
        HideAllPanels();
        if (_connectionPanel) _connectionPanel.SetActive(true);
    }

    public void ShowHostPanel()
    {
        HideAllPanels();
        if (_hostPanel) _hostPanel.SetActive(true);
    }

    public void ShowSettingsPanel()
    {
        HideAllPanels();
        if (_settingPanel) _settingPanel.SetActive(true);
    }

    public void ShowLoadingPanel()
    {
        HideAllPanels();
        if (_loadingPanel) _loadingPanel.SetActive(true);
    }
}