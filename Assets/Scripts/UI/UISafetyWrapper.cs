using UnityEngine;
using JUTPS;
using System.Collections;

public class UISafetyWrapper : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject UIPanel;

    [Header("Settings")]
    public bool DisableUIOnStart = true;
    public float CheckInterval = 0.5f;

    private void Start()
    {
        if (DisableUIOnStart && UIPanel != null)
        {
            UIPanel.SetActive(false);
            Debug.Log("UI temporarily disabled, waiting for player...");
        }

        StartCoroutine(UIManagementRoutine());
    }

    private IEnumerator UIManagementRoutine()
    {
        while (JUGameManager.PlayerController == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log("Player found! Initializing UI...");
        yield return new WaitForSeconds(0.2f);
        SetUI(true);

        while (true)
        {
            yield return new WaitForSeconds(CheckInterval);

            bool playerExists = JUGameManager.PlayerController != null;

            if (!playerExists && UIPanel != null && UIPanel.activeSelf)
            {
                SetUI(false);
                yield return StartCoroutine(WaitForRespawn());
            }
        }
    }

    private IEnumerator WaitForRespawn()
    {
        Debug.Log("Player lost. Waiting for respawn...");
        while (JUGameManager.PlayerController == null)
        {
            yield return new WaitForSeconds(0.5f);
        }
        SetUI(true);
    }

    private void SetUI(bool state)
    {
        if (UIPanel != null)
        {
            UIPanel.SetActive(state);
        }
    }
}