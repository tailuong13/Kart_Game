using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MatchmakingManager : NetworkBehaviour
{
    public static MatchmakingManager Instance;
    public int maxPlayers = 2;
    private List<ulong> queuedPlayers = new();
    private bool matchStarted = false;
    public MatchMakingUIController matchmakingUI;

    private void Awake()
    {
        Instance = this;
        matchmakingUI = FindObjectOfType<MatchMakingUIController>();
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
        NetworkManager.SceneManager.LoadScene("SelectCarScene", LoadSceneMode.Single);
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
            JoinMatchmakingServerRpc();
        }
    }
}