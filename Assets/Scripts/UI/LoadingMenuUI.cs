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
    [SerializeField] private TextMeshProUGUI loadingText;
    [SerializeField] private GameObject characterImage;

    private string _menuSceneName;

    private bool _isFirstLoad = true;

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

        StartCoroutine(LoadingSequence());
    }

    private IEnumerator LoadingSequence()
    {
        float visualProgress = 0f;

        if (_isFirstLoad)
        {
            SetLoadingText("Loading.");

            while (visualProgress < 50f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 50f, Time.deltaTime * 50f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            SetLoadingText("Loading..");

            while (visualProgress < 90f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 90f, Time.deltaTime * 50f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            SetLoadingText("Loading...");

            while (visualProgress < 100f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 100f, Time.deltaTime * 50f);
                UpdateProgress(visualProgress);
                yield return null;
            }
            if (GameUIManager.Instance != null)
            {
                    GameUIManager.Instance.ShowMainMenu(); 
            }
            _isFirstLoad = false;
        }
        else
        {
            AudioListener.volume = 0f;
            visualProgress = 0f;
            SetLoadingText("Connecting to Network...");
            while (visualProgress < 40f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 40f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            SetLoadingText("Loading World...");
            while (visualProgress < 80f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 80f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);
                yield return null;
            }

            SetLoadingText("Ready!");

            while (visualProgress < 100f)
            {
                visualProgress = Mathf.MoveTowards(visualProgress, 100f, Time.deltaTime * 100f);
                UpdateProgress(visualProgress);

                yield return null;
            }
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.HideAllPanels();
            }
            _isFirstLoad = true;
            AudioListener.volume = 1.0f;

        }
    }

    private void SetLoadingText(string message)
    {
        if (loadingText != null) loadingText.text = message;
    }

    private void UpdateProgress(float value)
    {
        if (progressBar != null) progressBar.SetValue(value);
    }
}