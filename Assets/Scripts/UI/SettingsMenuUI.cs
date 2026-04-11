using UnityEngine;
using Michsky.MUIP;

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

    private void OnBackClick()
    {
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