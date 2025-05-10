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
            if (networkObject != null && networkObject.IsOwner)
            {
                SendCheckpointTriggerServerRpc(_checkPointsSystem.transform.GetSiblingIndex(), networkObject.NetworkObjectId);
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SendCheckpointTriggerServerRpc(int checkPointIndex, ulong networkObjectId)
    {
        CheckPoint checkPoint = _checkPointsSystem.GetCheckpointByIndex(checkPointIndex);
        NetworkObject carNetworkObject = NetworkManager.Singleton.SpawnManager.SpawnedObjects[networkObjectId];
        
        if (checkPoint != null && carNetworkObject != null)
        {
            _checkPointsSystem.PlayerThroughCheckPoint(checkPoint, carNetworkObject);
        }
    }

    public void SetCheckPointsSystem(CheckPointsSystem checkPointsSystem)
    {
        this._checkPointsSystem = checkPointsSystem;
        Debug.Log($"SetCheckPointsSystem cho {name}");
    }
}
