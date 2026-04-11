using UnityEngine;
using System.Collections;

/// <summary>
/// The central router for all UI panels. 
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

    //  Singleton
    public static GameUIManager Instance { get; private set; }

    private void Awake()
    {
        Application.targetFrameRate = 60;
        if (Instance == null) 
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);
        }
        else 
        {
            Debug.LogWarning("[GameUIManager] Duplicate instance found and destroyed.");
            Destroy(gameObject);
            return;
        }

        ShowLoadingPanel();
    }

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

    public void TriggerDisconnectSequence()
    {
        StartCoroutine(DisconnectSequence());
    }

    private System.Collections.IEnumerator DisconnectSequence()
    {
        ShowLoadingPanel();
        yield return new WaitForSeconds(2.5f);
        ShowMainMenu();
        Debug.Log("Disconnected from Host: Returned to Main Menu via Loading.");
    }
}