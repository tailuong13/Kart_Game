using Unity.Netcode;
using UnityEngine;

public class BananaTrap : NetworkBehaviour
{
    public NetworkVariable<ulong> creatorClientId = new NetworkVariable<ulong>(default,
        NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log($"[BananaTrap.OnNetworkSpawn] creatorClientId = {creatorClientId}");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var netObj = other.GetComponentInParent<NetworkObject>();;
        if (netObj == null || !netObj.IsPlayerObject) return;
        
        if (netObj.OwnerClientId == creatorClientId.Value)
        {
            Debug.Log("🚫 Chuối bỏ qua chủ nhân.");
            return;
        }

        var powerUpHandler = other.GetComponent<PowerUpRandom>();
        if (powerUpHandler != null && powerUpHandler.IsShieldActive())
        {
            Debug.Log("🛡️ Người chơi có Shield, không gây hiệu ứng Spin.");
            return;
        }

        Debug.Log("🍌 Chuối va chạm với: " + netObj.name);
        SpinClientRpc(netObj.OwnerClientId);
        
        Debug.Log($"🍌 TriggerEnter: {other.name} | NetObjId: {netObj.NetworkObjectId} | ClientId: {netObj.OwnerClientId}");

        if (IsSpawned)
            NetworkObject.Despawn();
    }

    [ClientRpc]
    private void SpinClientRpc(ulong targetClientId)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        Debug.Log("🎯 SpinClientRpc được gọi cho clientId: " + targetClientId);

        var localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();

        if (localPlayer != null)
        {
            var spin = localPlayer.GetComponent<PlayerSpinEffect>();
            if (spin != null)
            {
                Debug.Log("✅ Tìm thấy PlayerSpinEffect, bắt đầu spin");
                spin.StartSpinning();
            }
            else
            {
                Debug.LogWarning("❌ Không tìm thấy PlayerSpinEffect!");
            }
        }
    }
}