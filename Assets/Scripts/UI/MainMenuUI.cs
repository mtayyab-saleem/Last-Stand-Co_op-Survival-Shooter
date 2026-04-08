using UnityEngine;
using Michsky.MUIP; // Michsky UI Namespace

/// <summary>
/// Handles interactions specifically for the Main Menu screen (Start, Join, Settings, Exit).
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Main Menu Buttons")]
    [SerializeField] private ButtonManager startButton;
    [SerializeField] private ButtonManager joinButton;
    [SerializeField] private ButtonManager settingButton;
    [SerializeField] private ButtonManager exitButton;

    [Header("Quit Modal Window")]
    [SerializeField] private ModalWindowManager quitModalWindow;
    [SerializeField] private ButtonManager confirmQuitButton;
    [SerializeField] private ButtonManager cancelQuitButton;

    private void Start()
    {
        SetupButtons();
        SetupQuitModal();
    }

    private void SetupButtons()
    {
        // Bind UI clicks to their respective functions
        if (startButton) startButton.onClick.AddListener(OnStartClick);
        if (joinButton) joinButton.onClick.AddListener(OnJoinClick);
        if (settingButton) settingButton.onClick.AddListener(OnSettingsClick);
        if (exitButton) exitButton.onClick.AddListener(OnExitClick);

        if (confirmQuitButton) confirmQuitButton.onClick.AddListener(OnConfirmQuit);
    }

    private void SetupQuitModal()
    {
        // Safely configure the Michsky Modal Window via code so it never breaks
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

    // =========================================================
    // BUTTON ACTIONS (Routing to the GameUIManager)
    // =========================================================

    private void OnStartClick()
    {
        GameUIManager.Instance.ShowHostPanel();
    }

    private void OnJoinClick()
    {
        GameUIManager.Instance.ShowConnectionPanel();
    }

    private void OnSettingsClick()
    {
        GameUIManager.Instance.ShowSettingsPanel();
    }

    private void OnExitClick()
    {
        if (quitModalWindow != null)
        {
            // Ensure the GameObject is active before opening the Michsky animation
            if (!quitModalWindow.gameObject.activeSelf)
            {
                quitModalWindow.gameObject.SetActive(true);
            }
            quitModalWindow.Open();
        }
    }

    private void OnConfirmQuit()
    {
        // Handles quitting securely whether you are in the Unity Editor or a Built Game
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}