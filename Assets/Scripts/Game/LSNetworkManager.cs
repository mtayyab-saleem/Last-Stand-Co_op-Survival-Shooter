using UnityEngine;
using Mirror;
using System;

public class LSNetworkManager : NetworkManager
{
    // Yahan humne apna Event define kar liya hai
    public static event Action OnDisconnectedEvent;

    // Yeh Mirror ka apna function hai jo Client ka connection tootne par chalta hai
    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        // Event ko trigger karwa diya
        OnDisconnectedEvent?.Invoke();
    }

    // Yeh function tab chalta hai jab Host khud server band karta hai (Leave Lobby)
    public override void OnStopServer()
    {
        base.OnStopServer();
        // Event ko trigger karwa diya
        OnDisconnectedEvent?.Invoke();
    }
}