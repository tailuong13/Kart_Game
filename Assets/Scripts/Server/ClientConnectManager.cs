using UnityEngine;
using Unity.Netcode;

public class ClientConnectManager : MonoBehaviour
{
    public void ConnectToServer(System.Action onConnected = null)
    {
        if (NetworkManager.Singleton.IsClient) return;
        
        NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
        {
            Debug.Log($"Đã kết nối với server, client ID: {clientId}");
            onConnected?.Invoke();
        };
        
        NetworkManager.Singleton.StartClient();
    }
}