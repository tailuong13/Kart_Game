using UnityEngine;
using Cinemachine;
using Unity.Netcode;

public class CinemachineFollowTarget : MonoBehaviour
{
    private CinemachineVirtualCamera _cinemachine;

    private void Start()
    {
        _cinemachine = GetComponent<CinemachineVirtualCamera>();
        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
        }
    }

    private void OnClientConnected(ulong clientId)
    {
        Invoke(nameof(AssignCameraTarget), 1f);
    }

    private void AssignCameraTarget()
    {
        foreach (NetworkObject netObj in FindObjectsOfType<NetworkObject>())
        {
            if (netObj.CompareTag("Player") && netObj.IsLocalPlayer)
            {
                _cinemachine.Follow = netObj.transform;
                _cinemachine.LookAt = netObj.transform;
                Debug.Log($"Cinemachine following: {netObj.name}");
                break;
            }
        }
    }
}