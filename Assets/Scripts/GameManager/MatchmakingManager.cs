using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MatchmakingManager : NetworkBehaviour
{
    public static MatchmakingManager Instance;
    private List<ulong> queuedPlayers = new();
    private bool matchStarted = false;
    public int maxPlayers = 2; // Số lượng người chơi tối đa trong một trận đấu
    public MatchMakingUIController matchmakingUI;

    private void Awake()
    {
        matchmakingUI = FindObjectOfType<MatchMakingUIController>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("MatchmakingManager OnNetworkSpawn. IsServer: " + IsServer + ", IsClient: " + IsClient);
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinMatchmakingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (matchStarted || queuedPlayers.Contains(clientId)) return;
    
        queuedPlayers.Add(clientId);
        Debug.Log($"Player {clientId} joined matchmaking: {queuedPlayers.Count}/{maxPlayers}");
    
        if (queuedPlayers.Count >= maxPlayers)
        {
            matchStarted = true;
            StartMatch();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveMatchmakingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (queuedPlayers.Contains(clientId))
        {
            queuedPlayers.Remove(clientId);
            Debug.Log($"Player {clientId} canceled matchmaking.");
        }
    }

    private void StartMatch()
    {
        Debug.Log("Đủ người trong hàng đợi ➜ gửi sang RoomManager để tạo phòng");

        foreach (ulong clientId in queuedPlayers)
        {
            SendJoinRoomToRoomManager(clientId);
        }

        queuedPlayers.Clear(); 
        matchStarted = false; 
    }
    
    private void SendJoinRoomToRoomManager(ulong clientId)
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager.Instance is NULL. Không thể gửi yêu cầu tạo phòng.");
            return;
        }
        
        RoomManager.Instance.AddPlayerToRoomFromMatchmaking(clientId);
    }

    public void OnFindMatchClicked()
    {
        matchmakingUI.StartSearch();
        matchmakingUI.OnCancelSearch = () =>
        {
            Debug.Log("Player canceled search (from UI).");
            if (NetworkManager.Singleton.IsClient)
            {
                LeaveMatchmakingServerRpc();
            }
        };

        if (NetworkManager.Singleton.IsClient)
        {
            if (Instance != null)
                JoinMatchmakingServerRpc();
            else
                Debug.LogError("MatchmakingManager is null. Maybe not spawned yet?");
        }
    }
}