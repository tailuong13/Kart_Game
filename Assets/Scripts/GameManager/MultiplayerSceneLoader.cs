using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class MultiplayerSceneLoader : MonoBehaviour
{
    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            Debug.Log("🔁 MultiplayerScene vừa được load ➜ bắt đầu StartServer()");
            NetworkManager.Singleton.StartServer();
        }
        else
        {
            Debug.Log("✅ MultiplayerScene đã được load bởi Client hoặc Server đã chạy.");
        }
        
        if (NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            string playerName = PlayerSession.Instance?.GetPlayerName();

            if (string.IsNullOrWhiteSpace(playerName))
            {
                playerName = $"Player_{NetworkManager.Singleton.LocalClientId}";
            }
            Debug.Log("PlayerName " + playerName);

            PlayerNameManager.Instance.RegisterLocalPlayerName(playerName);
        }
    }
}