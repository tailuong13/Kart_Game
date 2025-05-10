// GameManager.cs - Gợi ý các điểm cần thay đổi cho việc spawn nhiều xe client

using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [System.Serializable]
    public class PlayerCarOption
    {
        public string carId;                     
        public GameObject prefab;               
    }

    [Header("Car Prefabs")]
    public List<PlayerCarOption> carOptions = new();

    private Dictionary<ulong, string> clientCarChoice = new();     
    private List<Transform> spawnPoints = new();                   
    private HashSet<ulong> connectedClients = new();

    private void Start()
    {
        if (Instance == null) Instance = this;

        foreach (var car in carOptions)
        {
            if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(car.prefab))
            {
                NetworkManager.Singleton.AddNetworkPrefab(car.prefab);
            }
        }

        foreach (GameObject obj in GameObject.FindGameObjectsWithTag("SpawnPoint"))
        {
            spawnPoints.Add(obj.transform);
        }
        
        foreach (var car in carOptions)
        {
            Debug.Log($"[GameManager] Registering prefab: {car.carId}, null? {car.prefab == null}");
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsServer) return;

        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            ulong clientId = sceneEvent.ClientId;

            Debug.Log($"[Server] Client {clientId} scene ready — spawning player...");

            if (!connectedClients.Contains(clientId))
            {
                connectedClients.Add(clientId);
            }

            SpawnPlayer(clientId);
        }
    }
    
    private void OnPlayerDisconnected(ulong clientId)
    {
        Debug.Log($"[Server] Player {clientId} đã ngắt kết nối!");
        connectedClients.Remove(clientId);
        clientCarChoice.Remove(clientId);
    }
    private void OnPlayerConnected(ulong clientId)
    {
        if (!IsServer || connectedClients.Contains(clientId)) return;

        connectedClients.Add(clientId);
        Debug.Log($"[Server] Client {clientId} connected — scheduling spawn...");

        StartCoroutine(DelayedSpawn(clientId));
    }

    private IEnumerator DelayedSpawn(ulong clientId)
    {
        yield return null;
        yield return null;
        yield return null;

        if (!clientCarChoice.ContainsKey(clientId))
        {
            string defaultCarId = carOptions.Count > 0 ? carOptions[0].carId : "default";
            clientCarChoice[clientId] = defaultCarId;
        }
        SpawnPlayer(clientId);
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

        string carId = clientCarChoice.ContainsKey(clientId) ? clientCarChoice[clientId] : carOptions[0].carId;
        GameObject prefab = carOptions.Find(opt => opt.carId == carId)?.prefab;

        if (prefab == null)
        {
            Debug.LogError($"❌ Không tìm thấy prefab cho carId = {carId}");
            return;
        }

        GameObject player = Instantiate(prefab, spawnPoint.position, Quaternion.Euler(0, 90, 0));
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId);
    
        Debug.Log($"✅ [Server] Spawned {carId} for client {clientId} at {spawnPoint.position}");
    }
    
    [ServerRpc(RequireOwnership = false)]
    public void SetClientCarChoiceServerRpc(string carId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (carOptions.Exists(opt => opt.carId == carId))
        {
            clientCarChoice[clientId] = carId;
        }
        else
        {
            Debug.LogWarning($"⚠️ carId '{carId}' không hợp lệ, dùng mặc định");
            clientCarChoice[clientId] = carOptions[0].carId;
        }
    }
}
