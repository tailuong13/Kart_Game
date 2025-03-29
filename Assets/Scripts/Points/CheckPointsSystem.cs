using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Netcode;

public class CheckPointsSystem : NetworkBehaviour 
{
    [SerializeField] private List<NetworkObjectReference> carNetworkObjects = new();
    [SerializeField] private CheckPoint startPoint;
    
    private List<CheckPoint> _checkPointsList;
    private List<int> _nextCheckPointIndexList;
    private Dictionary<NetworkObjectReference, int> _lapCount;
    
    public static CheckPointsSystem Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null) Instance = this;

    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _checkPointsList = new List<CheckPoint>();
            foreach(Transform checkPointSingle in transform)
            {
                CheckPoint checkPoint = checkPointSingle.GetComponent<CheckPoint>();
                checkPoint.SetCheckPointsSystem(this);
                _checkPointsList.Add(checkPoint);
            }

            _nextCheckPointIndexList = new List<int>();
            _lapCount = new Dictionary<
                NetworkObjectReference, int>();
        }
        else if (IsClient)
        {
            RequestCarListServerRpc();
        }
    }
    
    public void AddPlayerToCheckpointSystem(NetworkObject car)
    {
        if (!IsServer) return;

        NetworkObjectReference carRef = car;
        if (!carNetworkObjects.Contains(carRef))
        {
            carNetworkObjects.Add(carRef);
            _nextCheckPointIndexList.Add(0);
            _lapCount[carRef] = 0;

            Debug.Log($"✅ Thêm xe {car.name} vào hệ thống checkpoint!");
            
            SyncCarListClientRpc(carNetworkObjects.ToArray());
        }
    }
    
    [ClientRpc]
    private void SyncCarListClientRpc(NetworkObjectReference[] carList)
    {
        carNetworkObjects.Clear();
        carNetworkObjects.AddRange(carList);
        
        _nextCheckPointIndexList.Clear();
        _lapCount.Clear();

        foreach (NetworkObjectReference carRef in carNetworkObjects)
        {
            if (carRef.TryGet(out NetworkObject carNetworkObject))
            {
                _nextCheckPointIndexList.Add(0);
                _lapCount[carRef] = 0;
            }
        }

        Debug.Log($"🔄 Đồng bộ danh sách {carList.Length} xe từ Server!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCarListServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        SyncCarListClientRpc(carNetworkObjects.ToArray());
    }
    
    public void PlayerThroughCheckPoint(CheckPoint checkPoint, NetworkObject carNetworkObject)
    {
        NetworkObjectReference carRef = carNetworkObject;
        if (!carNetworkObjects.Contains(carRef)) return;

        int carIndex = carNetworkObjects.IndexOf(carRef);
        int nextCheckPointIndex = _nextCheckPointIndexList[carIndex];

        if (_checkPointsList.IndexOf(checkPoint) == nextCheckPointIndex)
        {
            Debug.Log("✅ Đúng checkpoint");
            _nextCheckPointIndexList[carIndex] = (nextCheckPointIndex + 1) % _checkPointsList.Count;

            if (_nextCheckPointIndexList[carIndex] == 0)
            {
                _lapCount[carRef] += 1;
                Debug.Log($"🚗 Xe {carNetworkObject.name} hoàn thành vòng {_lapCount[carRef]}");

                if (_lapCount[carRef] >= 2)
                {
                    Debug.Log($"🏁 Xe {carNetworkObject.name} đã hoàn thành cuộc đua!");
                }
            }
        }
        else
        {
            Debug.Log("❌ Sai checkpoint");
        }
    }
}