using UnityEngine;
using Michsky.MUIP;
using TMPro; // TMP Input Field ke liye zaroori hai

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Settings Controls")]
    [SerializeField] private ButtonManager backButton;
    [SerializeField] private TMP_InputField playerNameInput; // Inspector mein UI Input Field yahan drag karein

    private void Start()
    {
        SetupButtons();

        // Agar input field hai, toh pehle se save naam usme dikhayein
        if (playerNameInput != null)
        {
            playerNameInput.text = PlayerPrefs.GetString("PlayerName", "Player " + Random.Range(1000, 9999));
            // Jab player type kar ke enter kare/bahar click kare, toh save kar lein
            playerNameInput.onEndEdit.AddListener(SavePlayerName);
        }
    }

    private void SavePlayerName(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            PlayerPrefs.SetString("PlayerName", newName);
            PlayerPrefs.Save();
            Debug.Log("Player Name Saved: " + newName);
        }
    }

    private void SetupButtons()
    {
        if (backButton != null) backButton.onClick.AddListener(OnBackClick);
    }

    private void OnBackClick()
    {
        if (GameUIManager.Instance != null) GameUIManager.Instance.ShowMainMenu();
    }
}