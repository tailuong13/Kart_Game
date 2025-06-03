using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MatchmakingManager : NetworkBehaviour
{
    public static MatchmakingManager Instance;
    private List<ulong> queuedPlayers = new();
    private bool matchStarted = false;
    public int maxPlayers = 2; // Số lượng người chơi tối đa trong một trận đấu
    public MatchMakingUIController matchmakingUI;
    public ClientConnectManager clientConnectManager;
    
    //Button
    public Button matchingBtn;
    
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindMatchingButton();
    }

    private void Start()
    {
        FindMatchingButton();
    }

    private void Awake()
    {
        matchmakingUI = FindObjectOfType<MatchMakingUIController>();
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        
        if (clientConnectManager == null)
            clientConnectManager = FindObjectOfType<ClientConnectManager>();

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }
    
    private void FindMatchingButton()
    {
        matchingBtn = GameObject.Find("MatchingBtn")?.GetComponent<Button>();
        if (matchingBtn == null)
        {
            Debug.Log("Không tìm thấy MatchingBtn trong scene.");
            return;
        }

        matchingBtn.onClick.RemoveAllListeners();
        matchingBtn.onClick.AddListener(OnFindMatchClicked);
        Debug.Log("Đã gán OnFindMatchClicked cho MatchingBtn");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log("MatchmakingManager OnNetworkSpawn. IsServer: " + IsServer + ", IsClient: " + IsClient);
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void JoinMatchmakingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (matchStarted || queuedPlayers.Contains(clientId)) return;
    
        queuedPlayers.Add(clientId);
        Debug.Log($"Player {clientId} joined matchmaking: {queuedPlayers.Count}/{maxPlayers}");
    
        if (queuedPlayers.Count >= maxPlayers)
        {
            matchStarted = true;
            StartMatch();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void LeaveMatchmakingServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (queuedPlayers.Contains(clientId))
        {
            queuedPlayers.Remove(clientId);
            Debug.Log($"Player {clientId} canceled matchmaking.");
        }
    }

    private void StartMatch()
    {
        Debug.Log("Đủ người trong hàng đợi ➜ gửi sang RoomManager để tạo phòng");

        foreach (ulong clientId in queuedPlayers)
        {
            SendJoinRoomToRoomManager(clientId);
        }

        queuedPlayers.Clear(); 
        matchStarted = false; 
    }
    
    private void SendJoinRoomToRoomManager(ulong clientId)
    {
        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager.Instance is NULL. Không thể gửi yêu cầu tạo phòng.");
            return;
        }
        
        RoomManager.Instance.AddPlayerToRoomFromMatchmaking(clientId);
    }

    public void OnFindMatchClicked()
    {
        matchmakingUI.StartSearch();
        matchmakingUI.OnCancelSearch = () =>
        {
            Debug.Log("Player canceled search (from UI).");
            if (NetworkManager.Singleton.IsClient)
            {
                LeaveMatchmakingServerRpc();
            }
        };

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            clientConnectManager.ConnectToServer();
            bool clientStarted = NetworkManager.Singleton.IsClient;
            Debug.Log("StartClient(): " + clientStarted);

            if (clientStarted)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += ClientConnected;
            }
            else
            {
                Debug.LogError("Không thể start client!");
            }
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            if (Instance != null)
                JoinMatchmakingServerRpc();
            else
                Debug.LogError("MatchmakingManager chưa sẵn sàng.");
        }
    }
    
    private void ClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            JoinMatchmakingServerRpc();

            NetworkManager.Singleton.OnClientConnectedCallback -= ClientConnected;
        }
    }
}