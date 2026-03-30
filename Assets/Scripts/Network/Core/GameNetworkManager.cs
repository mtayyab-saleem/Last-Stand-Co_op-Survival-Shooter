// ============================================================
// GameNetworkManager.cs
// PURPOSE: Our custom NetworkManager that extends Mirror's.
//          Mirror's NetworkManager already does a lot —
//          we ONLY override what we need for OUR game.
//
// PLACE THIS ON: The same GameObject as Mirror's NetworkManager
//                (replace the default NetworkManager component)
// ============================================================

using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;

/// <summary>
/// Extends Mirror's NetworkManager with Last Stand specific logic.
/// Handles: player connect/disconnect callbacks, scene fallback.
/// </summary>
public class GameNetworkManager : NetworkManager
{
    // The name of your main menu scene in Build Settings
    [Header("Scene Names (must match Build Settings)")]
    [SerializeField] private string _mainMenuSceneName = "MainMenu";

    // ════════════════════════════════════════════════════════
    // SERVER CALLBACKS (only run on the Host device)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Called on server when a new client connects AND is ready to spawn.
    /// base.OnServerAddPlayer() does the actual spawning — don't remove it!
    /// </summary>
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn); // <-- This spawns the player prefab. Keep this!
        Debug.Log($"[GameNetworkManager] Player joined. Connection ID: {conn.connectionId}. " +
                  $"Total players: {NetworkServer.connections.Count}");
    }

    /// <summary>
    /// Called on server when a client disconnects.
    /// </summary>
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        Debug.Log($"[GameNetworkManager] Player left. Connection ID: {conn.connectionId}");
        base.OnServerDisconnect(conn); // This cleans up the player object
    }

    // ════════════════════════════════════════════════════════
    // CLIENT CALLBACKS (run on every connected device)
    // ════════════════════════════════════════════════════════

    /// <summary>
    /// Called on THIS device when it successfully connects to the host.
    /// </summary>
    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[GameNetworkManager] Connected to host successfully.");
    }

    /// <summary>
    /// Called on THIS device when it disconnects from the host.
    /// Returns the player back to the main menu.
    /// </summary>
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[GameNetworkManager] Disconnected from host. Returning to main menu.");

        // Only go back to menu if we're NOT already there
        if (SceneManager.GetActiveScene().name != _mainMenuSceneName)
        {
            SceneManager.LoadScene(_mainMenuSceneName);
        }
    }

    /// <summary>
    /// Called on server when it stops (host presses disconnect).
    /// </summary>
    public override void OnStopHost()
    {
        base.OnStopHost();
        Debug.Log("[GameNetworkManager] Host stopped the server.");
    }
}