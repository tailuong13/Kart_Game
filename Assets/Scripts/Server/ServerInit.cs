using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class ServerInit : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[SERVER] Waiting for server start...");
        NetworkManager.Singleton.OnServerStarted += OnServerStarted;
    }

    private void OnServerStarted()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
        }
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnServerStarted -= OnServerStarted;
        }
    }
}