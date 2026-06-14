using UnityEngine;
using TMPro;
using Mirror;

public class PlayerNameplate : MonoBehaviour
{
    public TextMeshProUGUI nameText;
    public LSPlayer targetPlayer;

    // Hides the text at start to prevent the default white text from showing
    void Awake()
    {
        if (nameText != null)
        {
            nameText.gameObject.SetActive(false);
        }
    }

    // Checks team affiliation to show (Green) or hide the nameplate completely
    void Update()
    {
        if (targetPlayer == null || nameText == null || NetworkClient.localPlayer == null) return;

        LSPlayer localPlayer = NetworkClient.localPlayer.GetComponent<LSPlayer>();
        if (localPlayer == null) return;

        // Never show your own nameplate
        if (targetPlayer.isLocalPlayer)
        {
            nameText.gameObject.SetActive(false);
            return;
        }

        // Teammate rule: Not Solo mode, valid team ID, and matching team IDs
        bool isTeammate = (LSMatchManager.Instance != null &&
                           LSMatchManager.Instance.currentMode != LSMatchManager.GameMode.Solo) &&
                          (targetPlayer.teamID != -1) &&
                          (targetPlayer.teamID == localPlayer.teamID);

        // Apply visual logic
        if (isTeammate)
        {
            // If teammate: Show UI, set name, make it Green
            nameText.gameObject.SetActive(true);
            nameText.text = targetPlayer.playerName;
            nameText.color = Color.green;
        }
        else
        {
            // If enemy or solo mode: Completely hide the UI
            nameText.gameObject.SetActive(false);
        }
    }

    // Rotates the nameplate to always face the local camera
    void LateUpdate()
    {
        if (nameText.gameObject.activeInHierarchy && Camera.main != null)
        {
            transform.LookAt(transform.position + Camera.main.transform.rotation * Vector3.forward,
                             Camera.main.transform.rotation * Vector3.up);
        }
    }
}