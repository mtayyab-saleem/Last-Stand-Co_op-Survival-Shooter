using UnityEngine;
using Mirror;

public class DisconnectButton : MonoBehaviour
{
    public void OnDisconnectClick()
    {
        Mirror.NetworkManager networkManager = FindFirstObjectByType<Mirror.NetworkManager>();

        if (networkManager != null)
        {
            if (NetworkServer.active && NetworkClient.active)
            {
                networkManager.StopHost();
            }
            else if (NetworkClient.isConnected)
            {
                networkManager.StopClient();
            }
        }
    }
}