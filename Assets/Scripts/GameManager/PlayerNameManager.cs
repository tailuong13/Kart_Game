using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNameManager : NetworkBehaviour
{
    public static PlayerNameManager Instance { get; private set; }

    private Dictionary<ulong, string> playerNames = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }

    public void RegisterLocalPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            playerName = $"Player_{NetworkManager.Singleton.LocalClientId}";
        }

        SubmitNameServerRpc(playerName);
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SubmitNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (playerNames.ContainsKey(clientId)) return;

        playerNames[clientId] = name;
        Debug.Log($"📝 Đã lưu tên [{name}] cho clientId [{clientId}]");
    }
    
    public string GetPlayerName(ulong clientId)
    {
        if (playerNames.TryGetValue(clientId, out string name))
        {
            return name;
        }

        return $"Player_{clientId}";
    }
}