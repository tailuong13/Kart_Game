using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    private CheckPointsSystem _checkPointsSystem;
    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            _checkPointsSystem.PlayerThroughCheckPoint(this, other.transform);
        }
    }
    
    public void SetCheckPointsSystem(CheckPointsSystem checkPointsSystem)
    {
        this._checkPointsSystem = checkPointsSystem;
    }
}
