using System;
using System.Net;
using UnityEngine;
using Mirror;
using Mirror.Discovery;

public struct DiscoveryRequest : NetworkMessage
{
}

public struct DiscoveryResponse : NetworkMessage
{
    public IPEndPoint EndPoint { get; set; }
    public Uri uri;
    public long serverId;
    public string lobbyName;
}

[DisallowMultipleComponent]
[AddComponentMenu("Network/Custom Network Discovery")]
public class CustomNetworkDiscovery : NetworkDiscoveryBase<DiscoveryRequest, DiscoveryResponse>
{
    [Header("Profile & Events")]
    public PlayerProfileSO playerProfile;

    protected override DiscoveryRequest GetRequest() => new DiscoveryRequest();

    protected override DiscoveryResponse ProcessRequest(DiscoveryRequest request, IPEndPoint endpoint)
    {
        try
        {
            if (playerProfile == null)
            {
                playerProfile = ScriptableObject.CreateInstance<PlayerProfileSO>();
                playerProfile.LoadProfile();
            }
            
            string hostName = playerProfile.playerName;

            return new DiscoveryResponse
            {
                serverId = ServerId,
                uri = transport.ServerUri(),
                lobbyName = $"{hostName}'s Lobby"
            };
        }
        catch (NotImplementedException)
        {
            Debug.LogError($"Transport {transport} does not support network discovery");
            throw;
        }
    }

    protected override void ProcessResponse(DiscoveryResponse response, IPEndPoint endpoint)
    {
        response.EndPoint = endpoint;

        UriBuilder realUri = new UriBuilder(response.uri)
        {
            Host = response.EndPoint.Address.ToString()
        };
        response.uri = realUri.Uri;

        OnServerFound.Invoke(response);
    }
}
