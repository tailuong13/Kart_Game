using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class GameFlowManager : NetworkBehaviour
{
    public static GameFlowManager Instance;
    public NetworkVariable<FixedString32Bytes> SelectedMap = new(
        default,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log($"[GameFlowManager] Awake in scene: {SceneManager.GetActiveScene().name}");
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[OnNetworkSpawn] {(IsServer ? "Server" : "Client")} GameFlowManager loaded in scene {SceneManager.GetActiveScene().name}");
        Debug.Log($"IsServer={NetworkManager.Singleton.IsServer}, IsClient={NetworkManager.Singleton.IsClient}, IsHost={NetworkManager.Singleton.IsHost}, LocalClientId={NetworkManager.Singleton.LocalClientId}");
    }
    
    public void StartGame()
    {
        if (IsServer)
        {
            if (string.IsNullOrEmpty(SelectedMap.Value.ToString()))
            {
                Debug.LogError($"[Server] SelectedMap rỗng! Không thể load scene.");
                return;
            }
            Debug.Log($"[Server] Đang load map: {SelectedMap.Value}");
            StartCoroutine(LoadSceneWithDelay(SelectedMap.Value.ToString()));
        }
    }

    private IEnumerator LoadSceneWithDelay(string sceneName)
    {
        Debug.Log($"[Server] Chờ client sẵn sàng để load scene: {sceneName}");
        yield return new WaitUntil(() => NetworkManager.Singleton.ConnectedClients.Count == NetworkManager.Singleton.ConnectedClientsIds.Count);
        yield return new WaitForSeconds(1f);
        Debug.Log($"[Server] Connected clients: {NetworkManager.Singleton.ConnectedClients.Count}");
        foreach (var obj in FindObjectsOfType<NetworkObject>())
        {
            Debug.Log($"[Scene] NetworkObject {obj.name} | OwnerClientId: {obj.OwnerClientId} | IsSpawned: {obj.IsSpawned}");
        }
        NetworkManager.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
    
    
}