using System;
using System.Collections;
using TMPro;
using Unity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class ClientLoadingUI : NetworkBehaviour
{
    public GameObject loadingUI;
    public Slider loadingSlider;
    public TextMeshProUGUI loadingText;

    private void Start()
    {
        if (!IsClient || IsServer)
        {
            gameObject.SetActive(false); // Server không cần loading UI
            return;
        }
        StartCoroutine(FakeLoadingProgress());
    }

    private void OnEnable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent += HandleSceneEvent;
        }
    }

    private void OnDisable()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.OnSceneEvent -= HandleSceneEvent;
        }
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (!NetworkManager.Singleton.IsClient || !IsLocalClient()) return;

        switch (sceneEvent.SceneEventType)
        {
            // case SceneEventType.Load:
            //     Debug.Log($"[Client] Bắt đầu loading scene {sceneEvent.SceneName}");
            //     StartCoroutine(FakeLoadingProgress());
            //     break;

            case SceneEventType.LoadEventCompleted:
                Debug.Log($"[Client] Đã load xong scene {sceneEvent.SceneName}");
                loadingUI.SetActive(false);
                StartCoroutine(WaitThenNotifyReady());
                break;
        }
    }
    
    private IEnumerator WaitThenNotifyReady()
    {
        yield return new WaitUntil(() => GameManager.Instance != null);
        yield return new WaitForSeconds(0.2f); 

        Debug.Log("[Client] Gửi NotifyReady đến server");
        GameManager.Instance.NotifyReadyServerRpc();
        Debug.Log($"[Client] NotifyReady → LocalClientId: {NetworkManager.Singleton.LocalClientId}");
    }

    private IEnumerator FakeLoadingProgress()
    {
        Debug.Log("Đã chạy FakeLoadingProgress");
        loadingUI.SetActive(true);
        float progress = 0f;
        while (progress < 1f)
        {
            progress += Time.deltaTime * 0.25f; // ~4s là full
            loadingSlider.value = progress;
            loadingText.text = $"{(int)(progress * 100)}%";
            yield return null;
        }
        loadingSlider.value = 1f;
        loadingText.text = $"100%";
    }

    private bool IsLocalClient()
    {
        return NetworkManager.Singleton.LocalClientId == NetworkManager.Singleton.LocalClientId;
    }
}