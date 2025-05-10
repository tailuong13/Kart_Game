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
    private Dictionary<ulong, int> _lapCount;
    
    private List<int> _previousCheckpointIndexList;
    
    public static CheckPointsSystem Instance { get; private set; }


    private void Awake()
    {
        if (Instance == null) Instance = this;

    }

    public override void OnNetworkSpawn()
    {
        _checkPointsList = new List<CheckPoint>();
        foreach(Transform checkPointSingle in transform)
        {
            CheckPoint checkPoint = checkPointSingle.GetComponent<CheckPoint>();
            checkPoint.SetCheckPointsSystem(this);
            _checkPointsList.Add(checkPoint);
        }
        
        if (IsServer)
        {
            Debug.Log("🚀 Server khởi động CheckPointsSystem!");

            _nextCheckPointIndexList = new List<int>();
            _lapCount = new Dictionary<ulong, int>();
            _previousCheckpointIndexList = new List<int>();
        }
        else if (IsClient)
        {
            Debug.Log("📡 Client gửi RequestCarListServerRpc");

            _nextCheckPointIndexList = new List<int>();  
            _lapCount = new Dictionary<ulong, int>();    

            //RequestCarListServerRpc(); (nếu cần tự gọi sau khi spawn)
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
            _lapCount[car.NetworkObjectId] = 0;
            _previousCheckpointIndexList.Add(-1);

            Debug.Log("📡 Gửi danh sách xe cho tất cả client sau khi thêm!");
            SyncCarListClientRpc(carNetworkObjects.ToArray());
        }
    }
    
    [ClientRpc]
    private void SyncCarListClientRpc(NetworkObjectReference[] carList)
    {
        if (_nextCheckPointIndexList == null) _nextCheckPointIndexList = new List<int>();
        if (_lapCount == null) _lapCount = new Dictionary<ulong, int>();
        
        if (carList == null)
        {
            Debug.LogError("🚨 Lỗi: carList nhận được là NULL!");
            return;
        }
        if (carList.Length == 0)
        {
            Debug.LogError("🚨 Lỗi: carList nhận được rỗng!");
            return;
        }
        
        carNetworkObjects.Clear();
        carNetworkObjects.AddRange(carList);
        
        _nextCheckPointIndexList.Clear();
        _lapCount.Clear();

        foreach (NetworkObjectReference carRef in carNetworkObjects)
        {
            if (carRef.TryGet(out NetworkObject carNetworkObject))
            {
                _nextCheckPointIndexList.Add(0);
                _lapCount[carNetworkObject.NetworkObjectId] = 0;
            }
        }

        Debug.Log($"🔄 Đồng bộ danh sách {carList.Length} xe từ Server!");
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestCarListServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        Debug.Log($"📡 Nhận RequestCarListServerRpc từ client {clientId}");

        if (carNetworkObjects == null || carNetworkObjects.Count == 0)
        {
            Debug.LogError("🚨 Server: Không có xe nào để gửi về client!");
            return;
        }

        Debug.Log($"🚀 Server gửi danh sách {carNetworkObjects.Count} xe cho client {clientId}");
        SyncCarListClientRpc(carNetworkObjects.ToArray());
    }
    
    public void PlayerThroughCheckPoint(CheckPoint checkPoint, NetworkObject carNetworkObject)
    {
        if (!IsServer) return;
        
        ulong id = carNetworkObject.NetworkObjectId;
        NetworkObjectReference carRef = carNetworkObject;

        if (carNetworkObjects == null || !carNetworkObjects.Contains(carRef)) return;

        int carIndex = carNetworkObjects.IndexOf(carRef);
        int expectedIndex = _nextCheckPointIndexList[carIndex];
        int currentIndex = _checkPointsList.IndexOf(checkPoint);

        if (currentIndex == expectedIndex)
        {
            Debug.Log($"✅ Xe {carNetworkObject.name} qua đúng checkpoint {currentIndex}");
            
            if (currentIndex == 0 && _previousCheckpointIndexList[carIndex] == _checkPointsList.Count - 1)
            {
                _lapCount[id] += 1;
                Debug.Log($"🏁 Xe {carNetworkObject.name} hoàn thành vòng {_lapCount[id]}");

                KartController kart = carNetworkObject.GetComponent<KartController>();
                if (kart != null)
                {
                    kart.lapCount.Value = _lapCount[id];
                }

                ResetAllPowerUps();
            }
            
            _previousCheckpointIndexList[carIndex] = currentIndex;
            _nextCheckPointIndexList[carIndex] = (expectedIndex + 1) % _checkPointsList.Count;
        }
        else
        {
            Debug.Log($"❌ Xe {carNetworkObject.name} sai checkpoint! Expected: {expectedIndex}, Got: {currentIndex}");
        }
    }

    private void ResetAllPowerUps()
    {
        PowerUp[] powerUps = FindObjectsOfType<PowerUp>();
        foreach (PowerUp powerUp in powerUps)
        {
            powerUp.ResetGroup();
        }
        Debug.Log("🔄 Tất cả PowerUps đã được reset!");
    }
    
    public CheckPoint GetCheckpointByIndex(int index)
    {
        if (_checkPointsList != null && index >= 0 && index < _checkPointsList.Count)
            return _checkPointsList[index];
    
        return null;
    }
}