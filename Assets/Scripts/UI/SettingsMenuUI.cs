using UnityEngine;
using Michsky.MUIP; // Michsky UI Namespace

/// <summary>
/// Handles interactions specifically for the Settings Menu screen.
/// </summary>
public class SettingsMenuUI : MonoBehaviour
{
    [Header("Settings Controls")]
    [SerializeField] private ButtonManager backButton;

    private void Start()
    {
        SetupButtons();
    }

    private void SetupButtons()
    {
        // Bind the back button to the router
        if (backButton != null)
        {
            backButton.onClick.AddListener(OnBackClick);
        }
    }

    // =========================================================
    // BUTTON ACTIONS (Routing to the GameUIManager)
    // =========================================================

    private void OnBackClick()
    {
        // Tell the central router to hide this panel and show the Main Menu
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.ShowMainMenu();
        }
        else
        {
            Debug.LogError("[SettingsMenuUI] GameUIManager Instance is missing!");
        }
    }
}