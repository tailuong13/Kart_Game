using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    public NetworkVariable<string> playerName = new NetworkVariable<string>(
        value: "Unknown",
        readPerm: NetworkVariableReadPermission.Everyone,
        writePerm: NetworkVariableWritePermission.Server);
    
    [ServerRpc]
    public void SetPlayerNameServerRpc(string name, ServerRpcParams rpcParams = default)
    {
        playerName.Value = name;
        Debug.Log($"Player {OwnerClientId} set name to: {name}");
    }
    
    private void OnPlayerNameChanged(string oldValue, string newValue)
    {
        Debug.Log($"PlayerName updated from '{oldValue}' to '{newValue}'");
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            string myName = PlayerSession.Instance != null ? PlayerSession.Instance.PlayerName : "Player" + OwnerClientId;
            SetPlayerNameServerRpc(myName);
        }
        
        playerName.OnValueChanged += OnPlayerNameChanged;
    }

    public override void OnNetworkDespawn()
    {
        playerName.OnValueChanged -= OnPlayerNameChanged;
    }
}