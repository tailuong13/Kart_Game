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
            if (NetworkManager.Singleton.LocalClientId == clientId)
            {
                if (PlayerSession.Instance != null && PlayerNameManager.Instance != null)
                {
                    PlayerNameManager.Instance.RegisterLocalPlayerName(PlayerSession.Instance.PlayerName);
                }

            }
            onConnected?.Invoke();
        };
        
        NetworkManager.Singleton.StartClient();
    }
}