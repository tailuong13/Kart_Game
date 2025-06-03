using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MultiplayerSceneUI : MonoBehaviour
{
    [SerializeField] private CreateRoomUI _createRoomUI;
    [SerializeField] private JoinRoomUI _joinRoomUI;

    public ClientConnectManager clientConnectManager;
    
    private void Awake()
    {
        if (_createRoomUI == null)
        {
            _createRoomUI = FindObjectOfType<CreateRoomUI>();
        }

        if (_joinRoomUI == null)
        {
            _joinRoomUI = FindObjectOfType<JoinRoomUI>();
        }
        
        if (clientConnectManager == null)
        {
            clientConnectManager = FindObjectOfType<ClientConnectManager>();
        }
        
    }
    
    public void OnCreateRoomClicked()
    {
        _createRoomUI.ShowPanel();
    }
    
    public void OnJoinRoomClicked()
    {
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            clientConnectManager.ConnectToServer();
            Debug.Log("StartClient() trước khi join phòng: " + NetworkManager.Singleton.IsClient);

            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        }
        else
        {
            _joinRoomUI.ShowPanel(true);
            Debug.Log("Join Room button clicked. Implement room joining logic.");
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.LocalClientId == clientId)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            StartCoroutine(WaitForRoomManagerAndShowPanel());
        }
    }

    private IEnumerator WaitForRoomManagerAndShowPanel()
    {
        // Đợi RoomManager.Instance tồn tại, tối đa 5 giây
        float timeout = 5f;
        while (RoomManager.Instance == null && timeout > 0f)
        {
            yield return null;
            timeout -= Time.deltaTime;
        }

        if (RoomManager.Instance == null)
        {
            Debug.LogError("RoomManager chưa được spawn trên client sau khi kết nối!");
            yield break;
        }

        Debug.Log("RoomManager đã sẵn sàng trên client, show panel.");
        _joinRoomUI.ShowPanel(true);
    }

}
