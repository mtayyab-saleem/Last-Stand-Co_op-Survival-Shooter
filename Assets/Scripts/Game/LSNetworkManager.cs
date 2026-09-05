using UnityEngine;
using Mirror;
using System;
using UnityEngine.SceneManagement;

public class LSNetworkManager : NetworkManager
{
    public static event Action OnDisconnectedEvent;

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

        // Only trigger for pure clients, not the host itself
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