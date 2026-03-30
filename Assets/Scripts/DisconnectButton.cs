// ============================================================
// DisconnectButton.cs
// PURPOSE: Disconnect from the current network session.
//          Works for both HOST and CLIENT.
//
// ATTACH TO: The "Disconnect" or "Leave Match" button's GameObject.
// ============================================================

using UnityEngine;
using Mirror;

/// <summary>
/// Simple disconnect button that handles both Host and Client cases.
/// </summary>
public class DisconnectButton : MonoBehaviour
{
    /// <summary>
    /// Call this from the Button's OnClick event in the Inspector.
    /// </summary>
    public void OnDisconnectClick()
    {
        var networkManager = FindFirstObjectByType<NetworkManager>();

        if (networkManager == null)
        {
            Debug.LogError("[DisconnectButton] NetworkManager not found in scene.");
            return;
        }

        // Host = is both a server AND a client at the same time
        bool isHost = NetworkServer.active && NetworkClient.active;

        if (isHost)
        {
            // Stop everything — server, all clients, and our own client
            networkManager.StopHost();
            Debug.Log("[DisconnectButton] Host stopped the session.");
        }
        else if (NetworkClient.isConnected)
        {
            // Just stop our client connection
            networkManager.StopClient();
            Debug.Log("[DisconnectButton] Client disconnected from session.");
        }
        else
        {
            Debug.LogWarning("[DisconnectButton] Disconnect clicked but no active connection found.");
        }
    }
}