using UnityEngine;
using TMPro;
using System.Collections;
using Mirror;
using Michsky.MUIP; // Michsky UI Namespace
using UnityEngine.SceneManagement;
using JUTPS;

/// <summary>
/// Handles the visual loading sequence, safely transitioning the player from the menu to the active game world.
/// </summary>
public class LoadingMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private ProgressBar progressBar;
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject characterImage;

    // Cache the menu scene name to detect when the server successfully changes the scene
    private string _menuSceneName;

    private void Awake()
    {
        _menuSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        // Reset the UI state immediately when the loading screen appears
        if (progressBar != null)
        {
            progressBar.isOn = false;
            progressBar.SetValue(0f);
        }

        if (characterImage != null)
        {
            characterImage.SetActive(true);
        }

        // Start the loading sequence automatically
        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        float currentProgress = 0f;

        // =========================================================
        // PHASE 1: Network Connection
        // =========================================================
        SetLoadingText("Connecting to Network...");

        while (!NetworkClient.isConnected && !NetworkServer.active)
        {
            // Smoothly animate the progress bar up to 30% while waiting for network handshake
            currentProgress = Mathf.MoveTowards(currentProgress, 30f, Time.deltaTime * 20f);
            UpdateProgress(currentProgress);
            yield return null;
        }

        // =========================================================
        // PHASE 2: Scene Loading
        // =========================================================
        SetLoadingText("Loading World...");

        string currentScene = SceneManager.GetActiveScene().name;
        while (currentScene == _menuSceneName)
        {
            currentScene = SceneManager.GetActiveScene().name;

            // Smoothly animate the progress bar up to 60% while waiting for the map to load
            currentProgress = Mathf.MoveTowards(currentProgress, 60f, Time.deltaTime * 10f);
            UpdateProgress(currentProgress);
            yield return null;
        }

        // =========================================================
        // PHASE 3: Await Player Spawn (JUTPS Integration)
        // =========================================================
        SetLoadingText("Spawning Player...");

        float timeout = 20f;
        float timer = 0f;

        // Mobile Optimization: Using a cached WaitForSeconds instead of yielding every single frame
        WaitForSeconds fastWait = new WaitForSeconds(0.1f);

        // Wait until JUTPS Game Manager confirms your local player character exists
        while (JUGameManager.PlayerController == null)
        {
            if (currentProgress < 90f)
            {
                currentProgress += 1f;
                UpdateProgress(currentProgress);
            }

            timer += 0.1f;
            if (timer > timeout)
            {
                SetLoadingText("Waiting for Server Sync...");
            }

            yield return fastWait;
        }

        // =========================================================
        // COMPLETION
        // =========================================================
        SetLoadingText("Ready!");
        UpdateProgress(100f);

        // Brief pause so the player actually sees "100%" before it vanishes
        yield return new WaitForSeconds(0.5f);

        // Tell the central router to hide all UI panels, revealing the active game!
        if (GameUIManager.Instance != null)
        {
            GameUIManager.Instance.HideAllPanels();
        }
    }

    // --- Helper Methods to prevent Null Reference Exceptions ---

    private void SetLoadingText(string message)
    {
        if (loadingText != null) loadingText.text = message;
    }

    private void UpdateProgress(float value)
    {
        if (progressBar != null) progressBar.SetValue(value);
    }
}