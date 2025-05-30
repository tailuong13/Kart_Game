using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Serialization;
using Unity.Netcode;

public class CheckPointsSystem : NetworkBehaviour 
{
    [SerializeField] private List<NetworkObjectReference> carNetworkObjects = new();
    [SerializeField] private CheckPoint startPoint;
    
    private List<CheckPoint> _checkPointsList;
    [SerializeField]private List<int> _previousCheckpointIndexList;
    [SerializeField]private List<int> _nextCheckPointIndexList;
    private Dictionary<ulong, int> _lapCount;
    public Dictionary<ulong, PlayerRaceProgress> _raceProgress;
    private Dictionary<ulong, RaceProgressUI> _raceProgressUIMap = new Dictionary<ulong, RaceProgressUI>();
    public List<ulong> CurrentLeaderboard { get; private set; } = new();
    
    private bool raceFinished = false;
    private float countdownTime = 10f;
    
    private NetworkVariable<float> CountdownTimer = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private NetworkVariable<bool> IsCountdownActive = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    
    public int maxLap = 1;
    
    public static CheckPointsSystem Instance { get; private set; }
    
    public struct LeaderboardEntry : INetworkSerializable
    {
        public ulong PlayerId;
        public int Lap;

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref PlayerId);
            serializer.SerializeValue(ref Lap);
        }
    }
    
    private void Awake()
    {
        if (Instance == null) Instance = this;
    }
    
    private void Update()
    {
        if (!IsServer) return;

        if (raceFinished)
        {
            if (countdownTime > 0f)
            {
                countdownTime -= Time.deltaTime;
                if (countdownTime < 0f) countdownTime = 0f;
                
                CountdownTimer.Value = countdownTime;
                IsCountdownActive.Value = true;
            }
            else
            {
                IsCountdownActive.Value = false;
                // TODO: Kết thúc race, gửi event hoặc xử lý tiếp
                Debug.Log("Race kết thúc sau countdown");
            }
        }
        
        
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
            
            _raceProgress ??= new Dictionary<ulong, PlayerRaceProgress>();
            _raceProgress[car.NetworkObjectId] = new PlayerRaceProgress
            {
                Lap = 0,
                CheckpointIndex = 0,
                TimeStamp = Time.time
            };
            
            Debug.Log($"RaceProgress count: {_raceProgress?.Count ?? 0}");

            Debug.Log("📡 Gửi danh sách xe cho tất cả client sau khi thêm!");
            SyncCarListClientRpc(carNetworkObjects.ToArray());
            UpdateLeaderboard();
            FixedList64Bytes<LeaderboardEntry> lbList = new FixedList64Bytes<LeaderboardEntry>();
            foreach (var id in CurrentLeaderboard)
            {
                if (_raceProgress.TryGetValue(id, out var progress))
                {
                    lbList.Add(new LeaderboardEntry
                    {
                        PlayerId = id,
                        Lap = progress.Lap
                    });
                }
            }

            UpdateLeaderboardUIClientRpc(new ForceNetworkSerializeByMemcpy<FixedList64Bytes<LeaderboardEntry>>(lbList));
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

        if (carNetworkObjects == null)
        {
            Debug.LogWarning("⛔ carNetworkObjects null");
            return;
        }

        if (!carNetworkObjects.Contains(carRef))
        {
            Debug.LogWarning($"⛔ Không tìm thấy {carRef} trong danh sách carNetworkObjects");
            return;
        }

        int carIndex = carNetworkObjects.IndexOf(carRef);
        int expectedIndex = _nextCheckPointIndexList[carIndex];
        int currentIndex = _checkPointsList.IndexOf(checkPoint);
        
        Debug.Log($"🔍 currentIndex = {_checkPointsList.IndexOf(checkPoint)}, expectedIndex = {_nextCheckPointIndexList[carIndex]}");

        if (currentIndex == expectedIndex)
        {
            Debug.Log($"✅ Xe {carNetworkObject.name} qua đúng checkpoint {currentIndex}");
            
            if (currentIndex == 0 && _previousCheckpointIndexList[carIndex] == _checkPointsList.Count - 1)
            {
                _lapCount[id] += 1;
                Debug.Log($"🏁 Xe {carNetworkObject.name} hoàn thành vòng {_lapCount[id]}");
                if (_raceProgressUIMap.TryGetValue(id, out RaceProgressUI raceProgressUI))
                {
                    raceProgressUI.ResetLapTimerClientRpc();
                }
                else
                {
                    Debug.LogWarning($"⚠️ Không tìm thấy RaceProgressUI cho Client {id}");
                }

                KartController kart = carNetworkObject.GetComponent<KartController>();
                if (kart != null)
                {
                    kart.lapCount.Value = _lapCount[id];
                    
                    //finish count
                    if (!raceFinished && _lapCount[id] >= maxLap)
                    {
                        raceFinished = true;
                        if (_raceProgressUIMap.TryGetValue(id, out RaceProgressUI progressUI))
                        {
                            progressUI.MarkFinishClientRpc();
                        }
                        else
                        {
                            Debug.LogWarning($"⚠️ Không tìm thấy RaceProgressUI cho Client {id}");
                        }
                        countdownTime = 10f;
                        Debug.Log("🏁 Xe đầu tiên hoàn thành! Bắt đầu đếm ngược 10 giây cuối.");
                    }
                }
                _raceProgress[id].Lap = _lapCount[id];

                ResetAllPowerUps();
            }
            
            //leaderBoard
            _raceProgress[id].CheckpointIndex = currentIndex;
            _raceProgress[id].TimeStamp = Time.time;
            UpdateLeaderboard();
            FixedList64Bytes<LeaderboardEntry> lbList = new FixedList64Bytes<LeaderboardEntry>();
            foreach (var playerId in CurrentLeaderboard)
            {
                if (_raceProgress.TryGetValue(id, out var progress))
                {
                    lbList.Add(new LeaderboardEntry
                    {
                        PlayerId = playerId,
                        Lap = progress.Lap
                    });
                }
            }

            UpdateLeaderboardUIClientRpc(new ForceNetworkSerializeByMemcpy<FixedList64Bytes<LeaderboardEntry>>(lbList));
            
            _previousCheckpointIndexList[carIndex] = currentIndex;
            _nextCheckPointIndexList[carIndex] = (expectedIndex + 1) % _checkPointsList.Count;
        }
        else
        {
            Debug.Log($"❌ Xe {carNetworkObject.name} sai checkpoint! Expected: {expectedIndex}, Got: {currentIndex}");
        }
    }
    
    public float GetCountdownTime() => CountdownTimer.Value;
    public bool GetCountdownActive() => IsCountdownActive.Value;

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
    
    public int GetPlayerRank(ulong playerId)
    {
        if (CurrentLeaderboard == null || !CurrentLeaderboard.Contains(playerId))
            return -1; // Không có trong bảng xếp hạng

        return CurrentLeaderboard.IndexOf(playerId) + 1; // Vị trí tính từ 1
    }

    
    private void UpdateLeaderboard()
    {
        CurrentLeaderboard = new List<ulong>(_raceProgress.Keys);
    
        CurrentLeaderboard.Sort((a, b) =>
        {
            var pA = _raceProgress[a];
            var pB = _raceProgress[b];

            if (pA.Lap != pB.Lap)
                return pB.Lap.CompareTo(pA.Lap); 
            if (pA.CheckpointIndex != pB.CheckpointIndex)
                return pB.CheckpointIndex.CompareTo(pA.CheckpointIndex); 
            return pA.TimeStamp.CompareTo(pB.TimeStamp); 
        });

        Debug.Log("📊 Cập nhật bảng xếp hạng: " + string.Join(", ", CurrentLeaderboard));
    }
    
    [ClientRpc]
    private void UpdateLeaderboardUIClientRpc(ForceNetworkSerializeByMemcpy<FixedList64Bytes<LeaderboardEntry>> leaderboardWrapped)
    {
        var leaderboard = leaderboardWrapped.Value;

        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer == null) return;

        var ui = localPlayer.GetComponentInChildren<LeaderBoardUI>();
        if (ui != null)
        {
            ui.UpdateLeaderboardUI(leaderboard);
        }
    }
    
    public int GetCheckpointIndex(CheckPoint checkPoint)
    {
        return _checkPointsList.IndexOf(checkPoint);
    }
    
    public void RegisterRaceProgressUI(ulong clientId, RaceProgressUI ui)
    {
        if (!_raceProgressUIMap.ContainsKey(clientId))
        {
            _raceProgressUIMap.Add(clientId, ui);
            Debug.Log($"✅ Đã đăng ký UI cho Client {clientId}");
        }
    }
}