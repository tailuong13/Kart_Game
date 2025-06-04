using System.Collections;
using UnityEngine;
using Cinemachine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class CinemachineFollowTarget : NetworkBehaviour
{
    private CinemachineVirtualCamera _cinemachine;

    private void Awake()
    {
        _cinemachine = GetComponent<CinemachineVirtualCamera>();
        if (_cinemachine == null)
        {
            Debug.LogError("CinemachineVirtualCamera component not found on this GameObject.");
        }

        Debug.Log(
            $"ClientId: {NetworkManager.Singleton.LocalClientId} - IsOwner: {IsOwner} - IsLocalPlayer: {IsLocalPlayer} - ObjectName: {gameObject.name}");
    }

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
        StartCoroutine(AssignCameraAfterDelay());
    }

    private IEnumerator AssignCameraAfterDelay()
    {
        // Chờ tới khi playerObject đã có
        yield return new WaitUntil(() => NetworkManager.Singleton.LocalClient != null &&
                                         NetworkManager.Singleton.LocalClient.PlayerObject != null);

        Transform myTarget = NetworkManager.Singleton.LocalClient.PlayerObject.transform;

        _cinemachine.Follow = myTarget;
        _cinemachine.LookAt = myTarget;

        Debug.Log($"[Camera] Assigned follow to {myTarget.name} on client {NetworkManager.Singleton.LocalClientId}");
    }
}