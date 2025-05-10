using Unity.Netcode;
using UnityEngine;

public class BananaTrap : NetworkBehaviour
{
    public ulong creatorClientId;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        var netObj = other.GetComponent<NetworkObject>();
        if (netObj == null || !netObj.IsPlayerObject) return;
        
        if (netObj.OwnerClientId == creatorClientId)
        {
            Debug.Log("🚫 Chuối bỏ qua chủ nhân.");
            return;
        }

        Debug.Log("🍌 Chuối va chạm với: " + netObj.name);
        SpinClientRpc(netObj.OwnerClientId);

        if (IsSpawned)
            NetworkObject.Despawn();
    }

    [ClientRpc]
    private void SpinClientRpc(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId != clientId) return;

        var spin = GetComponentInParent<PlayerSpinEffect>();
        if (spin != null)
        {
            spin.StartSpinning();
        }
    }
}