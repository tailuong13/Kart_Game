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
            if (networkObject != null)
            {
                _checkPointsSystem.PlayerThroughCheckPoint(this, networkObject);
            }
            else
            {
                Debug.LogError($"🚨 Player {other.name} không có NetworkObject!");
            }
        }
    }
    
    public void SetCheckPointsSystem(CheckPointsSystem checkPointsSystem)
    {
        this._checkPointsSystem = checkPointsSystem;
    }
}
