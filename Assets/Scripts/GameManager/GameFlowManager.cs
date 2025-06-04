using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance;

    public NetworkVariable<FixedString32Bytes> SelectedMap = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public struct LobbyPlayerData
    {
        public ulong ClientId;
        public FixedString32Bytes PlayerName;
        public bool IsHost;
        public bool IsReady;
        public int CarId;
        public int CharacterId;
    }

    private Dictionary<ulong, LobbyPlayerData> lobbyPlayers = new();

    public ulong HostClientId;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log($"[GameFlowManager] Awake in scene: {SceneManager.GetActiveScene().name}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[OnNetworkSpawn] {(IsServer ? "Server" : "Client")} GameFlowManager loaded in scene {SceneManager.GetActiveScene().name}");
        
        if (IsServer)
        {
            HostClientId = NetworkManager.Singleton.LocalClientId;

            lobbyPlayers.Clear();

            var hostData = new LobbyPlayerData
            {
                ClientId = HostClientId,
                PlayerName = new FixedString32Bytes("Host"),
                IsHost = true,
                IsReady = false,
                CarId = 0,
                CharacterId = 0
            };

            lobbyPlayers[HostClientId] = hostData;

            SelectedMap.Value = new FixedString32Bytes("Map1Scene");

            UpdateLobbyClientRpc(GetLobbyPlayerList().Select(p => new LobbyPlayerInfo
            {
                ClientId = p.ClientId,
                PlayerName = p.PlayerName,
                IsHost = p.IsHost,
                IsReady = p.IsReady,
                CarId = p.CarId,
                CharacterId = p.CharacterId
            }).ToArray());
            
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
    }
    
    private void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        Debug.Log($"[OnClientConnected] Client {clientId} joined lobby");
        
        if (clientId == NetworkManager.ServerClientId)
        {
            Debug.Log("[OnClientConnected] Server connected, không thêm vào lobbyPlayers.");
            return;
        }
        
        bool isFirstPlayer = lobbyPlayers.Count == 0;

        if (!lobbyPlayers.ContainsKey(clientId))
        {
            lobbyPlayers[clientId] = new LobbyPlayerData
            {
                ClientId = clientId,
                PlayerName = new FixedString32Bytes($"Player{clientId}"),
                IsHost = false,
                IsReady = false,
                CarId = 0,
                CharacterId = 0
            };

            UpdateLobbyClientRpc(GetLobbyPlayerList().Select(p => new LobbyPlayerInfo
            {
                ClientId = p.ClientId,
                PlayerName = p.PlayerName,
                IsHost = isFirstPlayer,
                IsReady = p.IsReady,
                CarId = p.CarId,
                CharacterId = p.CharacterId
            }).ToArray());
        }
    }

    #region Lobby Player Join / Leave (thêm/xóa)

    
    public void RemovePlayerFromLobby(ulong clientId)
    {
        if (!IsServer) return;

        if (lobbyPlayers.ContainsKey(clientId))
        {
            lobbyPlayers.Remove(clientId);
            Debug.Log($"Player {clientId} left lobby.");

            if (clientId == HostClientId)
            {
                if (lobbyPlayers.Count > 0)
                {
                    HostClientId = ulong.MaxValue;
                    foreach (var kvp in lobbyPlayers)
                    {
                        if (kvp.Key < HostClientId)
                            HostClientId = kvp.Key;
                    }

                    var hostData = lobbyPlayers[HostClientId];
                    hostData.IsHost = true;
                    lobbyPlayers[HostClientId] = hostData;

                    Debug.Log($"New Host is {HostClientId}");
                }
                else
                {
                    Debug.Log("Lobby empty, no host.");
                }
            }

            UpdateLobbyClientRpc(GetLobbyPlayerList().Select(p => new LobbyPlayerInfo
            {
                ClientId = p.ClientId,
                PlayerName = p.PlayerName,
                IsHost = p.IsHost,
                IsReady = p.IsReady,
                CarId = p.CarId,
                CharacterId = p.CharacterId
            }).ToArray());
        }
    }

    #endregion

    #region ServerRpc: Các hàm client gọi

    [ServerRpc(RequireOwnership = false)]
    public void ToggleReady_ServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("CLient gửi ToggleReady_ServerRpc");
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log("HostClientId: " + HostClientId);

        if (lobbyPlayers.ContainsKey(clientId))
        {
            var playerData = lobbyPlayers[clientId];
            playerData.IsReady = !playerData.IsReady;
            lobbyPlayers[clientId] = playerData;

            Debug.Log($"Player {clientId} đã {(playerData.IsReady ? "Ready" : "Not Ready")}");

            UpdateLobbyClientRpc(GetLobbyPlayerList().Select(p => new LobbyPlayerInfo
            {
                ClientId = p.ClientId,
                PlayerName = p.PlayerName,
                IsHost = (p.ClientId == HostClientId),
                IsReady = p.IsReady,
                CarId = p.CarId,
                CharacterId = p.CharacterId
            }).ToArray());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateSelection_ServerRpc(int carId, int characterId, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (lobbyPlayers.ContainsKey(clientId))
        {
            var playerData = lobbyPlayers[clientId];
            playerData.CarId = carId;
            playerData.CharacterId = characterId;
            lobbyPlayers[clientId] = playerData;

            Debug.Log($"Player {clientId} chọn xe {carId}, nhân vật {characterId}");

            UpdateLobbyClientRpc(GetLobbyPlayerList().Select(p => new LobbyPlayerInfo
            {
                ClientId = p.ClientId,
                PlayerName = p.PlayerName,
                IsHost = p.IsHost,
                IsReady = p.IsReady,
                CarId = p.CarId,
                CharacterId = p.CharacterId
            }).ToArray());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void StartGame_ServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("CLient gửi StartGame_ServerRpc");
        
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log("ClientId: " + clientId);

        if (clientId != HostClientId)
        {
            Debug.LogWarning("Chỉ Host mới được phép bắt đầu game!");
            return;
        }
        

        foreach (var player in lobbyPlayers.Values)
        {
            Debug.Log($"[Check Ready] PlayerName: {player.PlayerName}, ClientId: {player.ClientId}, IsReady: {player.IsReady}");

            if (player.ClientId == 0)
            {
                Debug.Log("Host ảo, continue");
                continue;
            }
            
            if (player.ClientId == HostClientId)
            {
                Debug.Log($"[Check Ready] Bỏ qua Host: {player.PlayerName} (ClientId: {player.ClientId})");
                continue;
            }

            if (!player.IsReady)
            {
                Debug.LogWarning($"❌ Player chưa Ready: {player.PlayerName} (ClientId: {player.ClientId})");
                Debug.LogWarning("Không thể bắt đầu game khi còn người chơi chưa Ready.");
                return;
            }
        }

        Debug.Log("✅ Tất cả người chơi (trừ Host) đã Ready. Bắt đầu game...");

        Debug.Log("Host bắt đầu game...");
        StartGame();
    }

    #endregion

    #region ClientRpc: gửi dữ liệu lobby cho client

    [ClientRpc]
    private void UpdateLobbyClientRpc(LobbyPlayerInfo[] players)
    {
        Debug.Log($"Players count: {players.Length}");
        for (int i = 0; i < players.Length; i++)
        {
            Debug.Log($"Player {i}: {players[i].PlayerName} (ClientId: {players[i].ClientId})");
        }
        LobbyUIManager.Instance?.UpdatePlayerSlots(players);
    }
    #endregion

    #region Helper

    public LobbyPlayerData[] GetLobbyPlayerList()
    {
        return lobbyPlayers.Values
            .Where(p => p.ClientId != NetworkManager.ServerClientId)
            .ToArray();
    }

    public void StartGame()
    {
        if (!IsServer) return;

        if (string.IsNullOrEmpty(SelectedMap.Value.ToString()))
        {
            Debug.LogError("[Server] SelectedMap rỗng! Không thể load scene.");
            return;
        }

        Debug.Log($"[Server] Đang load map: {SelectedMap.Value}");

        StartCoroutine(LoadSceneWithDelay(SelectedMap.Value.ToString()));
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        Debug.Log($"[Server] Chờ client sẵn sàng để load scene: {sceneName}");

        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.Count == NetworkManager.Singleton.ConnectedClientsIds.Count);

        yield return new WaitForSeconds(1f);

        Debug.Log($"[Server] Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        foreach (var obj in FindObjectsOfType<NetworkObject>())
        {
            Debug.Log($"[Scene] NetworkObject {obj.name} | OwnerClientId: {obj.OwnerClientId} | IsSpawned: {obj.IsSpawned}");
        }

        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }

    #endregion
}


