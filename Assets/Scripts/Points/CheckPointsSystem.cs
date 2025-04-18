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
            Debug.Log("🚀 Server khởi động CheckPointsSystem!");
            
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
            Debug.Log("📡 Client gửi RequestCarListServerRpc");
            RequestCarListServerRpc();
        }
    }
    
    public void AddPlayerToCheckpointSystem(NetworkObject car)
    {
        if (!IsServer) return;
    
        if (car == null)
        {
            Debug.LogError("🚨 Thử thêm một xe nhưng NetworkObject bị NULL!");
            return;
        }

        NetworkObjectReference carRef = car;

        if (carNetworkObjects == null)
        {
            carNetworkObjects = new List<NetworkObjectReference>();
        }

        if (!carNetworkObjects.Contains(carRef))
        {
            carNetworkObjects.Add(carRef);
            Debug.Log($"✅ Thêm xe {car.name} vào hệ thống checkpoint!");

            _nextCheckPointIndexList.Add(0);
            _lapCount[carRef] = 0;
        
            Debug.Log($"📡 Trước khi gửi ClientRpc, số lượng xe: {carNetworkObjects.Count}");

            // Kiểm tra dữ liệu trước khi gửi
            if (carNetworkObjects.Count > 0)
            {
                SyncCarListClientRpc(carNetworkObjects.ToArray());
            }
            else
            {
                Debug.LogError("🚨 Không thể gửi ClientRpc vì danh sách xe trống!");
            }
        }
    }
    
    [ClientRpc]
    private void SyncCarListClientRpc(NetworkObjectReference[] carList)
    {
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
                _lapCount[carRef] = 0;
            }
        }

        Debug.Log($"🔄 Đồng bộ danh sách {carList.Length} xe từ Server!");
    }

    [ServerRpc(RequireOwnership = false)]
    private void RequestCarListServerRpc(ServerRpcParams rpcParams = default)
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
        NetworkObjectReference carRef = carNetworkObject;
        if (carNetworkObjects == null || !carNetworkObjects.Contains(carRef))
        {
            Debug.Log("Trả về");
            return;
        }

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
                
                ResetAllPowerUps();

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

    private void ResetAllPowerUps()
    {
        PowerUp[] powerUps = FindObjectsOfType<PowerUp>();
        foreach (PowerUp powerUp in powerUps)
        {
            powerUp.ResetGroup();
        }
        Debug.Log("🔄 Tất cả PowerUps đã được reset!");
    }
}