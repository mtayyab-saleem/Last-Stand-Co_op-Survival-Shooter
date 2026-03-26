using UnityEngine;
using Mirror;

public class DisconnectButton : MonoBehaviour
{
    public void OnDisconnectClick()
    {
        NetworkManager networkManager = FindFirstObjectByType<NetworkManager>();
        if (networkManager == null) return;

        if (NetworkServer.active && NetworkClient.active)
            networkManager.StopHost();       // This machine is the Host
        else if (NetworkClient.isConnected)
            networkManager.StopClient();     // This machine is a Client
    }
}