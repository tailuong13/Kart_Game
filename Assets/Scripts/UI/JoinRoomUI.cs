using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class JoinRoomUI : MonoBehaviour
{
    public static JoinRoomUI Instance;

    public GameObject panel;
    public GameObject roomEntryPrefab;
    public Transform roomListContainer;
    public Button refreshButton;
    public Button backBtn;
    public ClientConnectManager clientConnectManager;
    public RoomManager roomManager;

    private void Awake()
    {
        Instance = this;
        refreshButton.onClick.AddListener(RequestRoomList);
        
        if (clientConnectManager == null)
            clientConnectManager = FindObjectOfType<ClientConnectManager>();
        if (roomManager == null)
            roomManager = FindObjectOfType<RoomManager>();
    }

    public void ShowPanel(bool show)
    {
        panel.SetActive(show);
        if (show) RequestRoomList();
    }

    public void UpdateRoomList(RoomManager.RoomInfo[] rooms)
    {
        foreach (Transform child in roomListContainer)
            Destroy(child.gameObject);

        foreach (var room in rooms)
        {
            GameObject entry = Instantiate(roomEntryPrefab, roomListContainer);
            string label = $"{room.RoomId} ({room.CurrentPlayers}/{room.MaxPlayers}) - Port: {room.Port}";
            entry.GetComponentInChildren<TMP_Text>().text = label;

            string roomName = room.RoomId.ToString();

            entry.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
                {
                    clientConnectManager.ConnectToServer();
                    bool clientStarted = NetworkManager.Singleton.IsClient;
                    Debug.Log("StartClient() trước khi join phòng: " + clientStarted);

                    if (clientStarted)
                    {
                        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
                        {
                            if (NetworkManager.Singleton.LocalClientId == clientId)
                            {
                                if (roomManager == null)
                                    roomManager = FindObjectOfType<RoomManager>();
                                roomManager.JoinRoomServerRpc(roomName);
                                ShowPanel(false);
                                NetworkManager.Singleton.OnClientConnectedCallback -= null; // bỏ callback sau khi gọi
                            }
                        };
                    }
                    else
                    {
                        Debug.LogError("Không thể start client!");
                    }
                }
                else
                {
                    if (roomManager == null)
                        roomManager = FindObjectOfType<RoomManager>();
                    roomManager.JoinRoomServerRpc(roomName);
                    ShowPanel(false);
                }
            });
        }
    }

    private void RequestRoomList()
    {
        if (roomManager == null)
            roomManager = FindObjectOfType<RoomManager>();
        roomManager.RequestRoomListServerRpc();
    }
}