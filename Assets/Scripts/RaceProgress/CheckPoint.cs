using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private CheckPointsSystem _checkPointsSystem;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            NetworkObject networkObject = other.GetComponent<NetworkObject>();
            if (networkObject == null)
            {
                Debug.LogWarning($"⛔ Không tìm thấy NetworkObject trên {other.name}");
            }
            else
            {
                Debug.Log($"🔍 NetworkObject của {other.name}: IsOwner = {networkObject.IsOwner}");
            }
            SendCheckpointTriggerServerRpc(_checkPointsSystem.GetCheckpointIndex(this), networkObject.NetworkObjectId);
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SendCheckpointTriggerServerRpc(int checkPointIndex, ulong networkObjectId)
    {
        Debug.Log($"📨 ServerRpc nhận từ checkpointIndex: {checkPointIndex}, objectId: {networkObjectId}");
        CheckPoint checkPoint = _checkPointsSystem.GetCheckpointByIndex(checkPointIndex);
        NetworkObject carNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.ContainsKey(networkObjectId))
        {
            Debug.LogWarning($"❌ Không tìm thấy objectId {networkObjectId} trong SpawnedObjects!");
        }
        
        if (checkPoint != null && carNetworkObject != null)
        {
            Debug.Log($"✅ Gửi thông tin checkpoint {checkPointIndex} cho xe {carNetworkObject.name}");
            _checkPointsSystem.PlayerThroughCheckPoint(checkPoint, carNetworkObject);
        }
    }

    public void SetCheckPointsSystem(CheckPointsSystem checkPointsSystem)
    {
        this._checkPointsSystem = checkPointsSystem;
        Debug.Log($"SetCheckPointsSystem cho {name}");
    }
}
