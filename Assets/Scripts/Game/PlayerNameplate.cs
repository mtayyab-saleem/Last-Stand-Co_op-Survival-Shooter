using UnityEngine;
using TMPro; // TextMeshPro ke 
using Mirror;

public class PlayerNameplate : MonoBehaviour
{
    [Header("UI References")]
    public Canvas nameplateCanvas;
    public TextMeshProUGUI nameText;

    [Header("Player Reference")]
    public LSPlayer playerScript;

    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (playerScript == null) playerScript = GetComponentInParent<LSPlayer>();
    }

    void LateUpdate()
    {
        if (playerScript == null || nameText == null || nameplateCanvas == null) return;

        // 1. Nameplate ko hamesha Camera ki taraf face karwana (Billboard effect)
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null)
        {
            nameplateCanvas.transform.LookAt(nameplateCanvas.transform.position + mainCamera.transform.rotation * Vector3.forward, mainCamera.transform.rotation * Vector3.up);
        }

        // 2. Local Player ka data check karna
        var localPlayer = NetworkClient.localPlayer?.GetComponent<LSPlayer>();
        if (localPlayer != null && LSMatchManager.Instance != null)
        {
            // Naam update karna
            nameText.text = playerScript.playerName;

            // Check karna ke kya yeh samne wala player mera teammate hai?
            bool isTeammate = (LSMatchManager.Instance.currentMode != LSMatchManager.GameMode.Solo) &&
                              (playerScript.teamID != -1) &&
                              (playerScript.teamID == localPlayer.teamID);

            // 3. Color aur Visibility set karna
            if (playerScript.isLocalPlayer)
            {
                // Apna naam khud ko nahi dikhana
                nameplateCanvas.gameObject.SetActive(false);
            }
            else
            {
                nameplateCanvas.gameObject.SetActive(true);

                if (isTeammate)
                {
                    nameText.color = Color.green; // Teammate Hara
                }
                else
                {
                    nameText.color = Color.red; // Dushman Laal
                }
            }
        }
    }
}