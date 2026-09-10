using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class LSNetworkManager : NetworkManager
{
    public static event Action OnDisconnectedEvent;

    /// <summary>
    /// Gate for every incoming connection. Once the host starts the match the door is
    /// closed, and the lobby capacity is enforced here rather than only by discovery.
    /// </summary>
    public override void OnServerConnect(NetworkConnectionToClient conn)
    {
        // The host's own local connection is never rejected.
        if (conn == NetworkServer.localConnection)
        {
            base.OnServerConnect(conn);
            return;
        }

        LSMatchManager match = LSMatchManager.Instance;

        if (match != null)
        {
            if (match.matchStarted)
            {
                Debug.LogWarning(
                    $"[LSNetworkManager] Rejecting connection {conn.connectionId}: the match has already started."
                );

                conn.Disconnect();
                return;
            }

            if (numPlayers >= match.maxPlayers)
            {
                Debug.LogWarning(
                    $"[LSNetworkManager] Rejecting connection {conn.connectionId}: lobby is full " +
                    $"({numPlayers}/{match.maxPlayers})."
                );

                conn.Disconnect();
                return;
            }
        }

        base.OnServerConnect(conn);
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if (SceneManager.GetActiveScene().name == "LobbyScene")
        {
            Transform startPos = GetStartPosition();
            Vector3 spawnPos = startPos != null ? startPos.position : Vector3.zero;
            Quaternion spawnRot = startPos != null ? startPos.rotation : Quaternion.identity;

            // Dynamic Spacing based on current player count
            int playerIndex = numPlayers;
            float xOffset = 0f;

            if (playerIndex > 0)
            {
                int multiplier = (playerIndex + 1) / 2;
                float sign = (playerIndex % 2 != 0) ? 1f : -1f;
                xOffset = multiplier * 2.0f * sign;
            }

            // Apply spacing offset relative to the spawn rotation
            spawnPos += spawnRot * Vector3.right * xOffset;

            // Perfect Grounding: raycast down from an elevated position to find the ground
            Vector3 rayStart = spawnPos + Vector3.up * 10f;
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 20f))
            {
                spawnPos.y = hit.point.y;
            }

            GameObject player = Instantiate(playerPrefab, spawnPos, spawnRot);
            player.name = $"{playerPrefab.name} [connId={conn.connectionId}]";
            NetworkServer.AddPlayerForConnection(conn, player);
        }
        else
        {
            base.OnServerAddPlayer(conn);
        }
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        OnDisconnectedEvent?.Invoke();

        // Only trigger for pure clients, not the host itself.
        // This also covers a client refused because the match had already started:
        // the sequence resets lobby UI, tears the client down and routes back to the main menu.
        if (!NetworkServer.active)
        {
            if (GameUIManager.Instance != null)
            {
                GameUIManager.Instance.TriggerDisconnectSequence();
            }
        }
    }

    public override void OnStopServer()
    {
        base.OnStopServer();
        OnDisconnectedEvent?.Invoke();
    }
}