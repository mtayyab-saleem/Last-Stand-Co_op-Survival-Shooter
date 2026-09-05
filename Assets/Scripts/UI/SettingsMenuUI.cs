using UnityEngine;
using Michsky.MUIP;
using TMPro; // TMP Input Field ke liye zaroori hai

public class SettingsMenuUI : MonoBehaviour
{
    [Header("Settings Controls")]
    [SerializeField] private ButtonManager backButton;
    [SerializeField] private TMP_InputField playerNameInput; // Inspector mein UI Input Field yahan drag karein
    [SerializeField] private PlayerProfileSO playerProfile;

    private void Start()
    {
        SetupButtons();

        // Agar input field hai, toh pehle se save naam usme dikhayein
        if (playerNameInput != null)
        {
            if (playerProfile == null)
            {
                playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();
            }
            
            playerProfile.LoadProfile();
            playerNameInput.text = playerProfile.playerName;
            
            // Jab player type kar ke enter kare/bahar click kare, toh save kar lein
            playerNameInput.onEndEdit.AddListener(SavePlayerName);
        }
    }

    private void SavePlayerName(string newName)
    {
        if (!string.IsNullOrEmpty(newName))
        {
            if (playerProfile == null)
            {
                playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();
            }
            
            playerProfile.playerName = newName;
            playerProfile.SaveProfile();
            Debug.Log("Player Name Saved: " + newName);

            // Dynamically sync across the network if we are connected
            if (LSPlayer.LocalInstance != null)
            {
                LSPlayer.LocalInstance.CmdSetPlayerName(newName);
            }
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