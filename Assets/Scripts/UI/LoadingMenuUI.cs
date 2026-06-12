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
    private bool _isFirstLoad = true;

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

        if (_isFirstLoad)
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

            if (GameUIManager.Instance != null) GameUIManager.Instance.ShowMainMenu();
            _isFirstLoad = false;
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

            if (GameUIManager.Instance != null) GameUIManager.Instance.HideAllPanels();
            _isFirstLoad = true;
            AudioListener.volume = 1.0f;
        }
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