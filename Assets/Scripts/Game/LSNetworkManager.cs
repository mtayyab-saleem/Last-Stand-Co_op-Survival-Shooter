using UnityEngine;
using Mirror;
using System;

public class LSNetworkManager : NetworkManager
{
    public static event Action OnDisconnectedEvent;

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