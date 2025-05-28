using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance;

    [Serializable]
    public class PlayerCharacterOption
    {
        public string characterId;
        public GameObject prefab;
    }

    [Serializable]
    public class PlayerCarOption
    {
        public string carId;
        public GameObject prefab;
    }

    [Header("Car Prefabs")] public List<PlayerCarOption> carOptions = new();
    [Header("Character Prefabs")] public List<PlayerCharacterOption> characterOptions = new();

    private Dictionary<ulong, string> clientCharacterChoice = new();
    private Dictionary<ulong, string> clientCarChoice = new();
    private List<Transform> spawnPoints = new();
    private HashSet<ulong> connectedClients = new();
    private HashSet<ulong> readyClients = new();


    private void Awake()
    {
        Debug.Log(
            $"[GameManager] Awake in scene: {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}, hash: {this.GetHashCode()}");
    }

    private void Start()
    {
        if (Instance == null) Instance = this;

        foreach (var character in characterOptions)
        {
            if (!NetworkManager.Singleton.NetworkConfig.Prefabs.Contains(character.prefab))
            {
                NetworkManager.Singleton.AddNetworkPrefab(character.prefab);
            }
        }

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
        Debug.Log(
            $"IsServer={NetworkManager.Singleton.IsServer}, IsClient={NetworkManager.Singleton.IsClient}, IsHost={NetworkManager.Singleton.IsHost}, LocalClientId={NetworkManager.Singleton.LocalClientId}");
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += clientId =>
            {
                Debug.Log($"[Server] OnClientConnected: {clientId}");
            };
            NetworkManager.Singleton.SceneManager.OnSceneEvent += OnSceneEvent;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
        }
    }

    private void OnSceneEvent(SceneEvent sceneEvent)
    {
        if (!IsServer) return;

        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete)
        {
            spawnPoints.Clear();
            foreach (GameObject obj in GameObject.FindGameObjectsWithTag("SpawnPoint"))
            {
                spawnPoints.Add(obj.transform);
            }

            if (spawnPoints.Count == 0)
            {
                Debug.LogError("🚨 Không tìm thấy SpawnPoints trong scene!");
            }

            ulong clientId = sceneEvent.ClientId;

            Debug.Log($"[Server] Client {clientId} scene ready — spawning player...");

            if (!connectedClients.Contains(clientId))
            {
                connectedClients.Add(clientId);
            }

            if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId))
            {
                Debug.LogWarning($"❗️Client {clientId} không nằm trong ConnectedClients → BỎ spawn.");
                return;
            }

            SpawnPlayer(clientId);
        }
    }

    public IEnumerator StartCountdown()
    {
        Debug.Log("[Server] Bắt đầu đếm ngược...");
        yield return new WaitForSeconds(1f);

        Debug.Log("[Server] Gửi countdown đến client...");
        ShowCountdownClientRpc();
        yield return new WaitForSeconds(4f);

        Debug.Log("[Server] Cho phép xe chạy...");
        EnableCarControlClientRpc();
    }

    [ClientRpc]
    public void ShowCountdownClientRpc()
    {
        if (IsServer) return;

        CountDownUI ui = FindObjectOfType<CountDownUI>();
        if (ui != null)
        {
            ui.StartCountdown();
        }

        if (ui == null)
            Debug.LogWarning("[Client] Không tìm thấy CountDownUI");
    }

    [ClientRpc]
    private void EnableCarControlClientRpc()
    {
        if (IsServer) return;

        foreach (var car in FindObjectsOfType<KartController>())
        {
            if (car.IsOwner)
            {
                Debug.Log($"[Client] Cho phép xe {car.name} chạy");
                // car.canMove = true;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifyReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        int realClientCount = 0;
        foreach (var client in NetworkManager.Singleton.ConnectedClients)
        {
            if (client.Key != NetworkManager.Singleton.LocalClientId)
                realClientCount++;
        }

        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!readyClients.Contains(clientId))
        {
            readyClients.Add(clientId);
            Debug.Log($"[Server] Client {clientId} đã load xong Map");

            Debug.Log($"{readyClients.Count}, {realClientCount}");
            if (readyClients.Count == realClientCount)
            {
                Debug.Log("[Server] Tất cả đã sẵn sàng → bắt đầu đếm ngược");
                StartCoroutine(StartCountdown());
            }
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

        if (!clientCharacterChoice.ContainsKey(clientId))
        {
            string defaultCharId = characterOptions.Count > 0 ? characterOptions[0].characterId : "defaultChar";
            clientCharacterChoice[clientId] = defaultCharId;
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
        GameObject carPrefab = carOptions.Find(opt => opt.carId == carId)?.prefab;

        if (carPrefab == null)
        {
            Debug.LogError($"❌ Không tìm thấy prefab cho carId = {carId}");
            return;
        }

        string characterId = clientCharacterChoice.ContainsKey(clientId)
            ? clientCharacterChoice[clientId]
            : characterOptions[0].characterId;
        GameObject characterPrefab = characterOptions.Find(opt => opt.characterId == characterId)?.prefab;

        if (characterPrefab == null)
        {
            Debug.LogError($"❌ Không tìm thấy prefab cho characterId = {characterId}");
            return;
        }

        GameObject car = Instantiate(carPrefab, spawnPoint.position, Quaternion.Euler(0, 90, 0));
        NetworkObject carNetObj = car.GetComponent<NetworkObject>();
        if (carNetObj == null)
        {
            Debug.LogError("❌ Prefab xe không có NetworkObject.");
            Destroy(car);
            return;
        }

        carNetObj.SpawnAsPlayerObject(clientId);

        GameObject character = Instantiate(characterPrefab);

        NetworkObject characterNetObj = character.GetComponent<NetworkObject>();

        if (characterNetObj != null)
        {
            characterNetObj.Spawn(true);
            character.transform.SetParent(car.transform, worldPositionStays: false);

            CharacterIKSetup ik = character.GetComponent<CharacterIKSetup>();
            if (ik != null)
            {
                Debug.Log("Player sitting in car");
                ik.SitInCar(car);
            }
        }
        else
        {
            Debug.LogError("❌ Prefab character không có NetworkObject.");
            Destroy(character);
            return;
        }

        Debug.Log(
            $"✅ [Server] Spawned car {carId} + character {characterId} cho client {clientId} tại {spawnPoint.position}");
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

    [ServerRpc(RequireOwnership = false)]
    public void SetClientCharacterChoiceServerRpc(string characterId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (characterOptions.Exists(opt => opt.characterId == characterId))
        {
            clientCharacterChoice[clientId] = characterId;
        }
        else
        {
            Debug.LogWarning($"⚠️ characterId '{characterId}' không hợp lệ, dùng mặc định");
            clientCharacterChoice[clientId] = characterOptions[0].characterId;
        }
    }
}