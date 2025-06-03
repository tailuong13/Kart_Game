using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ServerBootstrap : NetworkBehaviour
{
    public bool isServerInstance = false;

    private void Start()
    {
        if (isServerInstance && !NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            bool started = NetworkManager.Singleton.StartServer();
            Debug.Log("Tự động Start Server ở MutiplayerScene: " + started);
        }
    }
}