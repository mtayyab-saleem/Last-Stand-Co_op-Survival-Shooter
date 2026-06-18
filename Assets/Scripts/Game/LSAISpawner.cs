using UnityEngine;
using Mirror;

public class LSAISpawner : NetworkBehaviour
{
    [Header("AI Settings")]
    public GameObject aiPrefab;
    public int totalAIToSpawn = 10;

    [Header("Spawn Locations")]
    public Transform[] spawnPoints;

    // Called on the server when the match starts. Initializes AI spawning.
    public override void OnStartServer()
    {
        SpawnAllAI();
    }

    // Loops through the required count and spawns AI at random designated points.
    [Server]
    private void SpawnAllAI()
    {
        if (aiPrefab == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("[LSAISpawner] Missing AI Prefab or Spawn Points!");
            return;
        }

        for (int i = 0; i < totalAIToSpawn; i++)
        {
            Transform randomPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

            // Instantiating the JUTPS AI Prefab
            GameObject aiInstance = Instantiate(aiPrefab, randomPoint.position, randomPoint.rotation);

            // Spawning across the network for all clients to see
            NetworkServer.Spawn(aiInstance);
        }
    }
}