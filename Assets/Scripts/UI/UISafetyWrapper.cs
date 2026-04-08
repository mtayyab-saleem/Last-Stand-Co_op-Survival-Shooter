using UnityEngine;
using JUTPS;
using System.Collections;

// Disables JU TPS UI until a networked player is spawned, then re-enables it.
// Attach to an empty GameObject in the scene.
public class UISafetyWrapper : MonoBehaviour
{
    [Header("UI Elements (Auto-found if not assigned)")]
    public GameObject UIPanel;

    [Header("Settings")]
    public bool DisableUIOnStart = true;

    private bool uiInitialized = false;

    void Start()
    {
        if (DisableUIOnStart && UIPanel != null)
            UIPanel.SetActive(false);

        StartCoroutine(WaitForPlayerAndEnableUI());
    }

    void Update()
    {
        // If player is destroyed (disconnect), hide UI to prevent null-ref errors
        if (uiInitialized && JUGameManager.PlayerController == null)
        {
            if (UIPanel != null) UIPanel.SetActive(false);
            uiInitialized = false;
        }
    }

    IEnumerator WaitForPlayerAndEnableUI()
    {
        while (JUGameManager.PlayerController == null)
            yield return new WaitForSeconds(0.1f);

        yield return new WaitForSeconds(0.2f); // Let player fully initialize
        EnableUI();
        uiInitialized = true;
    }

    void EnableUI()
    {
        if (UIPanel == null)
            UIPanel = GameObject.Find("JUTPS User Interface");

        if (UIPanel != null)
            UIPanel.SetActive(true);
        else
            Debug.LogError("[UISafetyWrapper] 'JUTPS User Interface' not found in scene.");
    }
}