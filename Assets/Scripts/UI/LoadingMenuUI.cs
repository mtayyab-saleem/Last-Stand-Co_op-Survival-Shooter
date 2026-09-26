using UnityEngine;
using TMPro;
using System.Collections;
using Mirror;
using Michsky.MUIP;
using UnityEngine.SceneManagement;
using JUTPS;

public class LoadingMenuUI : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private ProgressBar progressBar;
    [SerializeField] private TextMeshProUGUI loadingStatusText; // Status like "Connecting..."
    [SerializeField] private TextMeshProUGUI dotsLoadingText;   // Text that shows "Loading..."
    [SerializeField] private TextMeshProUGUI tipText;           // Left side Tip text
    [SerializeField] private GameObject characterImage;

    private string _menuSceneName;

    // 5 Simple Loading Tips
    private readonly string[] _tips = new string[]
    {
        "Headshots deal more damage and kill enemies faster.",
        "Keep moving to avoid enemy bullets.",
        "Stay inside the safe zone or you will lose health.",
        "Tap the screen to shoot automatically when enemy is in aim.",
        "Enemies are getting ready… Please wait."
    };

    private void Awake()
    {
        _menuSceneName = SceneManager.GetActiveScene().name;
    }

    private void OnEnable()
    {
        if (progressBar != null)
        {
            progressBar.isOn = false;
            progressBar.SetValue(0f);
        }

        if (characterImage != null)
        {
            characterImage.SetActive(true);
        }

        // Har baar random tip select hogi
        ShowRandomTip();

        StartCoroutine(LoadingSequence());
    }

    // The panel can be closed before its run ends (the intro video, a disconnect
    // opening the menu). The sound must not stay muted when that happens.
    private void OnDisable()
    {
        AudioListener.volume = 1.0f;
    }

    /// <summary>
    /// A host or client is running: the run ends on the lobby or match behind it.
    /// Otherwise (app start, after leaving a match) it ends on the main menu.
    /// Decided from the live network state rather than a flag flipped after every
    /// completed run: a run cut short left that flag wrong, and the next hosting then
    /// ended its loading screen by opening the main menu over the lobby.
    /// </summary>
    private static bool InSession => NetworkServer.active || NetworkClient.active;

    private void ShowRandomTip()
    {
        if (tipText != null)
        {
            int randomIndex = Random.Range(0, _tips.Length);
            tipText.text = "TIP: " + _tips[randomIndex]; // Left side tip
        }
    }

    private IEnumerator LoadingSequence()
    {
        float visualProgress = 0f;

        if (!InSession)
        {
            // 1. Loading Text with dots logic
            UpdateDotsText("Loading.");
            UpdateStatusText("Initializing...");

            while (visualProgress < 100f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 100f, Time.deltaTime * 40f);
                UpdateProgress(visualProgress);

                // Dots update based on progress
                if (visualProgress > 33f && visualProgress < 66f) UpdateDotsText("Loading..");
                else if (visualProgress >= 66f) UpdateDotsText("Loading...");

                yield return null;
            }

            FinishLoading();
        }
        else
        {
            AudioListener.volume = 0f;
            visualProgress = 0f;

            UpdateDotsText("Loading...");

            // 2. Status Messages
            UpdateStatusText("Connecting to Network...");
            while (visualProgress < 40f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 40f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            UpdateStatusText("Loading World...");
            while (visualProgress < 80f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 80f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            UpdateStatusText("Ready!");
            while (visualProgress < 100f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 100f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            AudioListener.volume = 1.0f;
            FinishLoading();
        }
    }

    private void FinishLoading()
    {
        if (GameUIManager.Instance == null)
            return;

        if (InSession)
            GameUIManager.Instance.HideAllPanels();
        else if (!IntroVideo.TryPlayPending())   // first launch: the intro opens the menu when it ends
            GameUIManager.Instance.ShowMainMenu();
    }

    private void UpdateStatusText(string message)
    {
        if (loadingStatusText != null) loadingStatusText.text = message;
    }

    private void UpdateDotsText(string dots)
    {
        if (dotsLoadingText != null) dotsLoadingText.text = dots;
    }

    private void UpdateProgress(float value)
    {
        if (progressBar != null) progressBar.SetValue(value);
    }
}