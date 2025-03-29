using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;
    public GameObject playerPrefab;
    private List<Transform> spawnPoints = new List<Transform>();
    private HashSet<ulong> connectedClients = new HashSet<ulong>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        
        // Tìm tất cả SpawnPoints trong scene
        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("SpawnPoint"))
        {
            spawnPoints.Add(obj.transform);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (!IsServer || connectedClients.Contains(clientId)) return;
        connectedClients.Add(clientId);
        
        Debug.Log($"[Server] Player {clientId} connected. Total players: {connectedClients.Count}");
        SpawnPlayer(clientId);
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        Debug.Log($"[Server] Player {clientId} đã ngắt kết nối!");
        connectedClients.Remove(clientId);
    }

    private void SpawnPlayer(ulong clientId)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogError("🚨 Không tìm thấy SpawnPoints trong scene!");
            return;
        }

        int spawnIndex = (int)(clientId % (ulong)spawnPoints.Count);
        Transform spawnPoint = spawnPoints[spawnIndex];
        
        GameObject player = Instantiate(playerPrefab, spawnPoint.position, Quaternion.Euler(0, 90, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);

        Debug.Log($"✅ [Server] Player {clientId} spawned at {spawnPoint.position}");
    }
}
