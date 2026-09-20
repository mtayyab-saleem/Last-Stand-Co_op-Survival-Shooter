using JUTPS;
using JUTPS.CameraSystems;
using Michsky.MUIP;
using TMPro;
using UnityEngine;
using Mirror;

/// <summary>
/// Displays one generic match result panel for Solo, Duo and Squad.
///
/// IMPORTANT:
/// Attach this script to an ACTIVE GameObject (for example the gameplay Canvas/HUD),
/// not to MatchResultPanel itself if that panel starts disabled.
/// </summary>
[DisallowMultipleComponent]
public class MatchResultUI : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panelRoot;

    [Header("Texts")]
    [SerializeField] private TMP_Text resultTitleText;
    [SerializeField] private TMP_Text winnerText;
    [SerializeField] private TMP_Text membersText;

    [Header("Button")]
    [SerializeField] private ButtonManager mainMenuButton;

    [Header("Text Settings")]
    [SerializeField] private string victoryText = "VICTORY";
    [SerializeField] private string defeatText = "DEFEAT";
    [SerializeField] private string matchOverText = "MATCH OVER";

    private bool resultShown;
    private bool leavingMatch;

    private void Awake()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.RemoveListener(OnMainMenuClicked);
            mainMenuButton.onClick.AddListener(OnMainMenuClicked);
        }
    }

    private void Update()
    {
        if (resultShown)
            return;

        MatchTracker tracker = MatchTracker.Instance;

        if (tracker == null || !tracker.TrackingActive || !tracker.MatchEnded)
            return;

        ShowResult(tracker);
    }

    private void ShowResult(MatchTracker tracker)
    {
        resultShown = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        LSMatchManager match = LSMatchManager.Instance;
        LSPlayer localPlayer = LSPlayer.LocalInstance;

        LSMatchManager.GameMode mode =
            match != null ? match.currentMode : LSMatchManager.GameMode.Solo;

        bool hasWinner =
            !string.IsNullOrWhiteSpace(tracker.WinnerPlayerName) ||
            tracker.WinningTeamId > 0;

        bool localVictory = false;

        if (hasWinner)
        {
            if (mode == LSMatchManager.GameMode.Solo)
            {
                // Do not compare names here because two players can use the same name.
                // The last alive local player is the Solo winner.
                localVictory = localPlayer != null && localPlayer.isAlive;
            }
            else
            {
                // Dead members of the winning Duo/Squad still belong to the winning team.
                localVictory =
                    localPlayer != null &&
                    localPlayer.teamID > 0 &&
                    localPlayer.teamID == tracker.WinningTeamId;
            }
        }

        if (resultTitleText != null)
        {
            resultTitleText.text = !hasWinner
                ? matchOverText
                : (localVictory ? victoryText : defeatText);
        }

        if (mode == LSMatchManager.GameMode.Solo)
        {
            if (winnerText != null)
            {
                winnerText.text = hasWinner
                    ? "WINNER: " + tracker.WinnerPlayerName
                    : "NO WINNER";
            }

            if (membersText != null)
                membersText.text = string.Empty;
        }
        else
        {
            if (winnerText != null)
            {
                winnerText.text = tracker.WinningTeamId > 0
                    ? "TEAM " + tracker.WinningTeamId + " WINS"
                    : "NO WINNER";
            }

            if (membersText != null)
            {
                membersText.text = tracker.WinningTeamId > 0 &&
                                   !string.IsNullOrWhiteSpace(tracker.WinningTeamMemberNames)
                    ? "WINNING TEAM\n" + FormatMemberNames(tracker.WinningTeamMemberNames)
                    : string.Empty;
            }
        }

        StopLocalGameplay();
    }

    /// <summary>
    /// MatchTracker stores team members as a comma-separated string.
    /// Display one player per line on the result panel.
    /// </summary>
    private string FormatMemberNames(string names)
    {
        if (string.IsNullOrWhiteSpace(names))
            return string.Empty;

        string[] split = names.Split(',');

        for (int i = 0; i < split.Length; i++)
            split[i] = split[i].Trim();

        return string.Join("\n", split);
    }

    private void StopLocalGameplay()
    {
        JUCharacterController character = JUGameManager.PlayerController;

        if (character != null)
        {
            character.DisableLocomotion();
            character.UseDefaultControllerInput = false;
            character.FiringMode = false;
            character.IsAiming = false;
        }

        // Stop camera look while the result UI is being used.
        if (CameraManager.MainCam != null)
            CameraManager.MainCam.enabled = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void OnMainMenuClicked()
    {
        if (leavingMatch)
            return;

        leavingMatch = true;

        if (mainMenuButton != null)
            mainMenuButton.Interactable(false);

        // Uses the project's existing safe host/client disconnect flow.
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.TriggerDisconnectSequence();
            return;
        }

        // Fallback only if the shared UI manager is unavailable.
        if (NetworkManager.singleton == null)
            return;

        if (NetworkServer.active && NetworkClient.active)
            NetworkManager.singleton.StopHost();
        else if (NetworkClient.isConnected)
            NetworkManager.singleton.StopClient();
    }
}
