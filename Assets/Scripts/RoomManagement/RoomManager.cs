    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using Unity.Netcode;
    using Unity.Collections;
    using UnityEngine;
    using UnityEngine.SceneManagement;

    public class RoomManager : NetworkBehaviour
    {
        public static RoomManager Instance;

        private List<RoomData> rooms = new List<RoomData>();
        private ushort basePort = 7777;
        private int playersReady = 0;
        private int requiredPlayers = 2;

        public bool HasJoinedRoom { get; private set; } = false;
        public string CurrentRoomName { get; private set; } = "";
        public LobbyUIManager lobbyUIManager;
        
        
        public struct RoomInfo : INetworkSerializable
        {
            public FixedString64Bytes RoomId;
            public ushort Port;
            public int CurrentPlayers;
            public int MaxPlayers;

            public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
            {
                serializer.SerializeValue(ref RoomId);
                serializer.SerializeValue(ref Port);
                serializer.SerializeValue(ref CurrentPlayers);
                serializer.SerializeValue(ref MaxPlayers);
            }
        }
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(this);
            
            if(lobbyUIManager == null)
                lobbyUIManager = FindObjectOfType<LobbyUIManager>();
            
        }
        
        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == "LobbyScene")
            {
                lobbyUIManager = FindObjectOfType<LobbyUIManager>();
                Debug.Log($"[RoomManager] LobbyUIManager được tìm thấy sau khi load scene: {lobbyUIManager != null}");
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestJoinRoomServerRpc(string roomName, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;
            Debug.Log($"Client {clientId} yêu cầu join phòng");

            RoomData room = FindRoomWithSpace();

            if (room == null)
            {
                room = CreateNewRoom();
                Debug.Log($"Tạo phòng mới {room.RoomId} với port {room.Port}");
            }

            room.Players.Add(clientId);
            Debug.Log($"Client {clientId} vào phòng {room.RoomId}");

            SendRoomPortClientRpc(room.Port, roomName, CreateClientRpcParams(clientId));

            if (room.Players.Count >= room.MaxPlayers)
            {
                Debug.Log($"Phòng {room.RoomId} đủ người, bắt đầu chuyển scene chọn xe");
                NetworkManager.SceneManager.LoadScene("SelectCarScene", LoadSceneMode.Single);
            }
        }

        private RoomData FindRoomWithSpace()
        {
            foreach (var room in rooms)
            {
                if (!room.IsFull)
                    return room;
            }
            return null;
        }

        private RoomData CreateNewRoom()
        {
            var room = new RoomData
            {
                RoomId = $"Room{rooms.Count + 1}",
                Port = (ushort)(basePort + rooms.Count),
                MaxPlayers = 2
            };
            rooms.Add(room);
            return room;
        }

        public void AddPlayerToRoomFromMatchmaking(ulong clientId)
        {
            Debug.Log($"[RoomManager] Nhận yêu cầu từ Matchmaking: Thêm player {clientId}");

            RoomData room = FindRoomWithSpace();

            if (room == null)
            {
                room = CreateNewRoom();
                Debug.Log($"[RoomManager] Tạo phòng mới {room.RoomId} với port {room.Port}");
            }

            room.Players.Add(clientId);
            Debug.Log($"[RoomManager] Player {clientId} được thêm vào phòng {room.RoomId}");

            SendRoomPortClientRpc(room.Port, room.RoomId, CreateClientRpcParams(clientId));

            if (room.Players.Count >= room.MaxPlayers)
            {
                Debug.Log($"[RoomManager] Phòng {room.RoomId} đủ người, chờ tất cả client kết nối...");
                NetworkManager.SceneManager.LoadScene("SelectCarScene", LoadSceneMode.Single);
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void CreateRoomServerRpc(string roomName, bool isMatchmaking = false, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            if (rooms.Exists(r => r.RoomId == roomName))
            {
                Debug.LogWarning($"Phòng {roomName} đã tồn tại.");
                return;
            }

            var room = new RoomData
            {
                RoomId = roomName,
                Port = (ushort)(basePort + rooms.Count),
                MaxPlayers = 2,
                IsMatchmaking = isMatchmaking
            };

            room.HostClientId = clientId;
            room.AddPlayer(clientId);
            rooms.Add(room);
            GameFlowManager.Instance.HostClientId = clientId;
            Debug.Log($"Client {clientId} tạo phòng {room.RoomId}, {room.RoomId} có host là {room.HostClientId}");
            
            PlayerJoinedRoom(room.RoomId, clientId);

            SendRoomPortClientRpc(room.Port, room.RoomId, CreateClientRpcParams(clientId));
            
        }

        [ServerRpc(RequireOwnership = false)]
        public void JoinRoomServerRpc(string roomName, ServerRpcParams rpcParams = default)
        {
            ulong clientId = rpcParams.Receive.SenderClientId;

            var room = rooms.Find(r => r.RoomId == roomName && !r.IsFull);

            if (room == null)
            {
                Debug.LogWarning($"Phòng {roomName} không tồn tại hoặc đã đầy.");
                SendJoinResultClientRpc(false, room.RoomId, room.Port, CreateClientRpcParams(clientId));
                return;
            }

            if (room.Players.Contains(clientId))
            {
                Debug.LogWarning($"Client {clientId} đã ở trong phòng {roomName}.");
                return;
            }

            room.AddPlayer(clientId);
            Debug.Log($"Client {clientId} tham gia phòng {room.RoomId}");
            PlayerJoinedRoom(room.RoomId, clientId);
            SendJoinResultClientRpc(true, room.RoomId, room.Port, CreateClientRpcParams(clientId));

            if (room.IsFull)
            {
                Debug.Log($"Phòng {room.RoomId} đủ người, chờ ready...");
            }
        }
        
        [ClientRpc]
        private void SendJoinResultClientRpc(bool success, FixedString64Bytes roomId, ushort port, ClientRpcParams rpcParams = default)
        {
            HasJoinedRoom = success;
        
            if (success)
            {
                TempRoomData.Instance.RoomName = roomId.ToString();
                TempRoomData.Instance.RoomPort = port;
            
                SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
            }
            else
            {
                Debug.Log("Join phòng thất bại");
                // Hiển thị thông báo lỗi cho người chơi
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestRoomListServerRpc(ServerRpcParams rpcParams = default)
        {
            List<RoomInfo> roomInfos = new List<RoomInfo>();
            foreach (var room in rooms)
            {
                var info = new RoomInfo
                {
                    RoomId = room.RoomId,
                    Port = room.Port,
                    CurrentPlayers = room.Players.Count,
                    MaxPlayers = room.MaxPlayers
                };
                roomInfos.Add(info);
            }

            SendRoomListClientRpc(roomInfos.ToArray(), CreateClientRpcParams(rpcParams.Receive.SenderClientId));
        }

        [ClientRpc]
        private void SendRoomListClientRpc(RoomInfo[] roomList, ClientRpcParams rpcParams = default)
        {
            JoinRoomUI.Instance.UpdateRoomList(roomList);
        }

        private ClientRpcParams CreateClientRpcParams(ulong clientId)
        {
            return new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            };
        }

        [ClientRpc]
        private void SendRoomPortClientRpc(ushort port, FixedString64Bytes roomName, ClientRpcParams rpcParams = default)
        {
            Debug.Log($"Client nhận port phòng: {port}, tên phòng: {roomName}");

            TempRoomData.Instance.RoomName = roomName.ToString();
            TempRoomData.Instance.RoomPort = port;
            
            SceneManager.LoadScene("LobbyScene", LoadSceneMode.Single);
        }

        public void SetPlayerReadyState(ulong clientId, bool isReady)
        {
            RoomData room = GetRoomOfClient(clientId);
            if (room == null) return;

            if (room.ReadyStates.ContainsKey(clientId))
            {
                room.ReadyStates[clientId] = isReady;
                Debug.Log($"Client {clientId} đã {(isReady ? "Ready" : "Not Ready")} trong {room.RoomId}");
            }
        }

        private RoomData GetRoomOfClient(ulong clientId)
        {
            return rooms.Find(room => room.Players.Contains(clientId));
        }

        public void UpdateRoomMapIndex(int newMapIndex)
        {
            if (rooms.Count == 0) return;

            RoomData hostRoom = rooms[0];
            hostRoom.SelectedMapIndex = newMapIndex;

            Debug.Log($"Map của phòng {hostRoom.RoomId} được chuyển sang index {newMapIndex}");
        }

        public void TryStartGame()
        {
            foreach (var room in rooms)
            {
                if (room.IsFull && room.AllReady)
                {
                    Debug.Log($"Tất cả người chơi trong phòng {room.RoomId} đã Ready. Bắt đầu game!");
                    room.Status = RoomData.RoomStatus.InGame;

                    string mapSceneName = GetSceneNameByIndex(room.SelectedMapIndex);
                    NetworkManager.SceneManager.LoadScene(mapSceneName, LoadSceneMode.Single);
                }
                else
                {
                    Debug.Log($"Không thể bắt đầu game: {(room.IsFull ? "có người chưa Ready" : "phòng chưa đủ người")}");
                }
            }
        }

        private string GetSceneNameByIndex(int index)
        {
            switch (index)
            {
                case 0: return "Map1Scene";
                case 1: return "MapScene2";
                case 2: return "MapScene3";
                default: return "MapScene1";
            }
        }
        
        // [ServerRpc(RequireOwnership = false)]
        // public void JoinRoomByPortServerRpc(ushort port, ServerRpcParams rpcParams = default)
        // {
        //     ulong clientId = rpcParams.Receive.SenderClientId;
        //     RoomData room = rooms.Find(r => r.Port == port && !r.IsFull);
        //
        //     if (room == null)
        //     {
        //         Debug.LogWarning($"Không tìm thấy phòng với port {port} hoặc đã đầy.");
        //         return;
        //     }
        //
        //     room.AddPlayer(clientId);
        //     Debug.Log($"Client {clientId} đã tham gia phòng {room.RoomId} với port {port}");
        //
        //     SendRoomPortClientRpc(room.Port, CreateClientRpcParams(clientId));
        // }
        
        public LobbyPlayerInfo[] GetLobbyPlayerInfos()
        {
            Debug.Log($"GetLobbyPlayerInfos called. Rooms count: {rooms.Count}");
            foreach(var r in rooms)
            {
                Debug.Log($"Room {r.RoomId} has {r.Players.Count} players.");
            }
    
            var infos = rooms.SelectMany(room => room.Players.Select(cid => new LobbyPlayerInfo
            {
                ClientId = cid,
                PlayerName = PlayerSession.Instance?.GetPlayerName() ?? $"Player {cid}",
                IsHost = cid == room.HostClientId,
                IsReady = room.ReadyStates.ContainsKey(cid) ? room.ReadyStates[cid] : false,
                CarId = 0,
                CharacterId = 0
            })).ToArray();

            Debug.Log($"Returning {infos.Length} players' info.");
            return infos;
        }
        
        public static void PlayerJoinedRoom(string roomId, ulong clientId)
        {
            Debug.Log("[RoomManager] PlayerJoinedRoom called for room " + roomId + " with clientId " + clientId);
            var room = Instance.rooms.Find(r => r.RoomId == roomId);
            if (room != null)
            {
                room.AddPlayer(clientId);
                
                var infos = room.Players.Select(cid => new LobbyPlayerInfo
                {
                    ClientId = cid,
                    PlayerName = PlayerNameManager.Instance.GetPlayerName(cid),
                    IsHost = cid == room.HostClientId,
                    IsReady = room.ReadyStates[cid],
                    CarId = 0,
                    CharacterId = 0
                }).ToArray();

                SendLobbyInfosToClients(infos, room.Players);
                Debug.Log($"AddPlayer: clientId = {clientId}, isHost = {clientId == room.HostClientId}");
                Debug.Log("[RoomManager] Gửi thông tin lobby đến tất cả người chơi trong phòng " + roomId);
            }
        }

        private static void SendLobbyInfosToClients(LobbyPlayerInfo[] infos, List<ulong> players)
        {
            Debug.Log("[RoomManager] Chạy SendLobbyInfosToClients với " + infos.Length + " người chơi");
            foreach (var clientId in players)
            {
                var targetClient = NetworkManager.Singleton.ConnectedClients[clientId];
                RoomManager.Instance.UpdateLobbyClientRpc(infos);
                Debug.Log($"[RoomManager] Gửi thông tin lobby đến client {clientId}");
            }
        }
        
        [ClientRpc]
        public void UpdateLobbyClientRpc(LobbyPlayerInfo[] infos)
        {
            StartCoroutine(WaitForLobbyUIAndUpdate(infos));
        }

        private IEnumerator WaitForLobbyUIAndUpdate(LobbyPlayerInfo[] infos)
        {
            // Đợi đến khi lobbyUIManager được gán
            while (lobbyUIManager == null)
                yield return null;

            Debug.Log($"[LobbyUIManager] Received {infos.Length} player(s) to update");

            lobbyUIManager.UpdatePlayerSlots(infos); 
        }
    }
