using System.Collections.Generic;
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestJoinRoomServerRpc(ServerRpcParams rpcParams = default)
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

        SendRoomPortClientRpc(room.Port, CreateClientRpcParams(clientId));

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

        SendRoomPortClientRpc(room.Port, CreateClientRpcParams(clientId));

        if (room.Players.Count >= room.MaxPlayers)
        {
            Debug.Log($"[RoomManager] Phòng {room.RoomId} đủ người, chờ tất cả client kết nối...");
            NetworkManager.SceneManager.LoadScene("SelectCarScene", LoadSceneMode.Single);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void CreateRoomServerRpc(string roomName, ServerRpcParams rpcParams = default)
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
            MaxPlayers = 2
        };

        room.AddPlayer(clientId);
        rooms.Add(room);

        Debug.Log($"Client {clientId} tạo phòng {room.RoomId}");
        SendRoomPortClientRpc(room.Port, CreateClientRpcParams(clientId));
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinRoomServerRpc(string roomName, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        var room = rooms.Find(r => r.RoomId == roomName && !r.IsFull);

        if (room == null)
        {
            Debug.LogWarning($"Phòng {roomName} không tồn tại hoặc đã đầy.");
            return;
        }

        room.AddPlayer(clientId);
        Debug.Log($"Client {clientId} tham gia phòng {room.RoomId}");

        SendRoomPortClientRpc(room.Port, CreateClientRpcParams(clientId));

        if (room.IsFull)
        {
            Debug.Log($"Phòng {room.RoomId} đủ người, chờ ready...");
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestRoomListServerRpc(ServerRpcParams rpcParams = default)
    {
        List<FixedString64Bytes> roomSummaries = new List<FixedString64Bytes>();
        foreach (var room in rooms)
        {
            FixedString64Bytes summary = $"{room.RoomId} ({room.Players.Count}/{room.MaxPlayers})";
            roomSummaries.Add(summary);
        }

        SendRoomListClientRpc(roomSummaries.ToArray(), CreateClientRpcParams(rpcParams.Receive.SenderClientId));
    }

    [ClientRpc]
    private void SendRoomListClientRpc(FixedString64Bytes[] roomList, ClientRpcParams rpcParams = default)
    {
        Debug.Log("Danh sách phòng:");
        foreach (var room in roomList)
        {
            Debug.Log($"- {room}");
        }
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
    private void SendRoomPortClientRpc(ushort port, ClientRpcParams rpcParams = default)
    {
        Debug.Log($"Client nhận port phòng: {port}");
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
}
