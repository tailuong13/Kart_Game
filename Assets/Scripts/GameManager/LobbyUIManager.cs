using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

[Serializable]
public struct LobbyPlayerInfo : INetworkSerializable
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public bool IsReady;
    public bool IsHost;
    public int CarId;
    public int CharacterId;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref IsReady);
        serializer.SerializeValue(ref IsHost);
        serializer.SerializeValue(ref CarId);
        serializer.SerializeValue(ref CharacterId);
    }
}

public class LobbyUIManager : MonoBehaviour
{
    public static LobbyUIManager Instance;
    [SerializeField] private List<PlayerSlotUI> playerSlots;

    private void Awake()
    {
        Instance = this;
    }
    
    private void Start()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Requesting lobby data...");
            RequestLobbyData_ServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void RequestLobbyData_ServerRpc(ServerRpcParams rpcParams = default)
    {
        Debug.Log("Server received request for lobby data");

        ulong senderClientId = rpcParams.Receive.SenderClientId;
        var players = RoomManager.Instance.GetLobbyPlayerInfos();

        SendLobbyDataToClientClientRpc(players, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { senderClientId }
            }
        });
    }

    [ClientRpc]
    private void SendLobbyDataToClientClientRpc(LobbyPlayerInfo[] players, ClientRpcParams rpcParams = default)
    {
        Debug.Log("Client received lobby data");
        UpdatePlayerSlots(players);
    }

    public void UpdatePlayerSlots(LobbyPlayerInfo[] players)
    {
        Debug.Log($"Updating player slots for {players.Length} players.");
        for (int i = 0; i < playerSlots.Count; i++)
        {
            if (i < players.Length)
            {
                Debug.Log($"Setting slot {i} to {players[i].PlayerName}");
                bool isLocal = players[i].ClientId == NetworkManager.Singleton.LocalClientId;
                bool isHost = players[i].IsHost;
                playerSlots[i].SetActive(true);
                playerSlots[i].Initialize(players[i], isLocal, isHost);
            }
            else
            {
                playerSlots[i].SetActive(false);
            }
        }
    }
    
    
}