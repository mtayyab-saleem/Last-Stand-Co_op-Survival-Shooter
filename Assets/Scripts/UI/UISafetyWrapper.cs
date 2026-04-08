using UnityEngine;
using JUTPS;
using System.Collections;

/// <summary>
/// UI Scripts ko safe banata hai bina original code change kiye
/// Scene mein ek empty GameObject pe lagao (naam: "UI Safety Manager")
/// </summary>
public class UISafetyWrapper : MonoBehaviour
{
    [Header("UI Elements (Auto-find hoga)")]
    public GameObject UIPanel; // "JUTPS User Interface" GameObject

    [Header("Settings")]
    [Tooltip("UI ko initially disable rakho? (Recommended: YES)")]
    public bool DisableUIOnStart = true;

    private bool uiInitialized = false;

    void Start()
    {
        if (DisableUIOnStart && UIPanel != null)
        {
            UIPanel.SetActive(false);
            Debug.Log("🔒 UI temporarily disabled, waiting for player...");
        }

        StartCoroutine(WaitForPlayerAndEnableUI());
    }

    IEnumerator WaitForPlayerAndEnableUI()
    {
        // Step 1: Player ka intezar karo
        while (JUGameManager.PlayerController == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("✅ Player found! Initializing UI...");

        // Step 2: Thoda wait karo taake player fully setup ho jaye
        yield return new WaitForSeconds(0.2f);

        // Step 3: UI ko enable karo
        EnableUI();

        uiInitialized = true;
    }

    void EnableUI()
    {
        // Auto-find UI panel agar assign nahi hai
        if (UIPanel == null)
        {
            UIPanel = GameObject.Find("JUTPS User Interface");
        }

        if (UIPanel != null)
        {
            // UI ko enable karo - ab Scripts safe hain kyunki Player mil gaya
            UIPanel.SetActive(true);
            Debug.Log("✅ UI enabled successfully!");
        }
        else
        {
            Debug.LogError("❌ UI Panel not found! Scene mein 'JUTPS User Interface' hai?");
        }
    }

    void Update()
    {
        // Agar player destroy ho gaya (disconnect), UI disable karo
        if (uiInitialized && JUGameManager.PlayerController == null)
        {
            if (UIPanel != null)
            {
                UIPanel.SetActive(false);
            }
            uiInitialized = false;
        }
    }
}