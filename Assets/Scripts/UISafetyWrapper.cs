// ============================================================
// UISafetyWrapper.cs
// PURPOSE: Enable/disable JU TPS UI panel based on player state.
//          UI should be HIDDEN until the local player spawns,
//          and HIDDEN again if player disconnects.
//
// IMPROVEMENT: Now uses NetworkEventBus events instead of
//              polling JUGameManager.PlayerController every frame.
// ============================================================

using UnityEngine;
using System.Collections;

/// <summary>
/// Safely enables the JU TPS in-game UI after the local player spawns.
/// Place this on any empty GameObject in your Game scene (not the menu scene).
/// Name suggestion: "UI Safety Manager"
/// </summary>
public class UISafetyWrapper : MonoBehaviour
{
    // ── Inspector Fields ─────────────────────────────────────
    [Header("JU TPS UI Panel")]
    [Tooltip("Drag the 'JUTPS User Interface' GameObject here. " +
             "If left empty, we'll try to find it by name automatically.")]
    [SerializeField] private GameObject _uiPanel;

    [Header("Timing")]
    [Tooltip("Extra delay after player spawns before showing UI. " +
             "Gives JU TPS time to fully initialize.")]
    [SerializeField] private float _initDelay = 0.3f;

    // ════════════════════════════════════════════════════════
    // UNITY LIFECYCLE
    // ════════════════════════════════════════════════════════

    private void Awake()
    {
        // Hide the UI immediately on scene load
        // It will be shown only after player spawns
        SetUIVisible(false);
    }

    private void OnEnable()
    {
        // Listen for player spawn/despawn events
        // These are fired by PlayerNetworkSetup
        NetworkEventBus.OnLocalPlayerSpawned += HandlePlayerSpawned;
        NetworkEventBus.OnLocalPlayerDespawned += HandlePlayerDespawned;
    }

    private void OnDisable()
    {
        // Always unsubscribe to prevent memory leaks
        NetworkEventBus.OnLocalPlayerSpawned -= HandlePlayerSpawned;
        NetworkEventBus.OnLocalPlayerDespawned -= HandlePlayerDespawned;
    }

    // ════════════════════════════════════════════════════════
    // EVENT HANDLERS
    // ════════════════════════════════════════════════════════

    private void HandlePlayerSpawned(GameObject player)
    {
        // Wait a tiny bit before enabling UI
        // Ensures JU TPS scripts are fully set up
        StartCoroutine(EnableUIAfterDelay());
    }

    private void HandlePlayerDespawned()
    {
        // Player left — hide UI immediately
        SetUIVisible(false);
        Debug.Log("[UISafetyWrapper] Player despawned — UI hidden.");
    }

    // ════════════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ════════════════════════════════════════════════════════

    private IEnumerator EnableUIAfterDelay()
    {
        yield return new WaitForSeconds(_initDelay);
        SetUIVisible(true);
    }

    private void SetUIVisible(bool visible)
    {
        // Auto-find panel if not assigned in Inspector
        if (_uiPanel == null)
            _uiPanel = GameObject.Find("JUTPS User Interface");

        if (_uiPanel != null)
        {
            _uiPanel.SetActive(visible);
            Debug.Log($"[UISafetyWrapper] UI {(visible ? "enabled" : "disabled")}.");
        }
        else if (visible)
        {
            // Only warn if we're TRYING to show it and can't find it
            Debug.LogWarning("[UISafetyWrapper] 'JUTPS User Interface' not found in scene. " +
                             "Assign it in the Inspector or check the GameObject name.");
        }
    }
}