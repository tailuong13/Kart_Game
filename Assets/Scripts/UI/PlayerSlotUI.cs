using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerSlotUI : MonoBehaviour
{
 public TextMeshProUGUI playerNameText;
    public TextMeshProUGUI statusText;
    public GameObject hostIcon; // Ẩn hiện icon host
    public TMP_Dropdown carDropdown;
    public TMP_Dropdown characterDropdown;
    public Button actionButton; // nút Ready/Start dùng chung

    private ulong clientId;
    private bool isLocalPlayer;
    private bool isLocalPlayerHost = false; // Đánh dấu client hiện tại có phải host hay không
    private bool isReady = false;

    public void Initialize(LobbyPlayerInfo playerInfo, bool isLocal, bool isLocalHost)
    {
        Debug.Log($"Initialize slot for ClientId {playerInfo.ClientId}, PlayerName: {playerInfo.PlayerName}, isLocal: " + isLocal + ", isLocalHost: " + isLocalHost + ", IsHost: " + playerInfo.IsHost + ", IsReady: " + playerInfo.IsReady);
        
        clientId = playerInfo.ClientId;
        playerNameText.text = playerInfo.PlayerName.ToString();
        hostIcon.SetActive(playerInfo.IsHost);
        
        actionButton.gameObject.SetActive(true);

        isLocalPlayer = isLocal;
        isLocalPlayerHost = isLocalHost;
        isReady = playerInfo.IsReady;

        UpdateStatusText();

        // 1. Local Host
        if (isLocalPlayer && playerInfo.IsHost)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                Debug.Log("🟢 StartButton clicked!");
                GameFlowManager.Instance.StartGame_ServerRpc();
            });

            statusText.text = "H";
        }
        // ----------------------------
        // 2. Local client thường
        else if (isLocalPlayer)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "N-Ready" : "Ready";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                Debug.Log("🟢 ReadyButton clicked!");
                GameFlowManager.Instance.ToggleReady_ServerRpc();
            });
            UpdateStatusText();
        }

        carDropdown.value = playerInfo.CarId;
        characterDropdown.value = playerInfo.CharacterId;

        carDropdown.interactable = IsLocalClient(playerInfo.ClientId);
        characterDropdown.interactable = IsLocalClient(playerInfo.ClientId);
    }

    private bool IsLocalClient(ulong playerClientId)
    {
        return playerClientId == NetworkManager.Singleton.LocalClientId;
    }

    private void UpdateStatusText()
    {
        if (isLocalPlayerHost && hostIcon.activeSelf)
        {
            statusText.text = "H"; 
        }
        else
        {
            statusText.text = isReady ? "R" : "NR";
        }
    }

    public void  UpdateSlot(LobbyPlayerInfo playerInfo)
    {
        isReady = playerInfo.IsReady;

        playerNameText.text = playerInfo.PlayerName.ToString();
        hostIcon.SetActive(playerInfo.IsHost);
        carDropdown.value = playerInfo.CarId;
        characterDropdown.value = playerInfo.CharacterId;

        // Host logic: người tạo phòng
        if (playerInfo.IsHost && isLocalPlayer)
        {
            statusText.text = "H";
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
        }
        else
        {
            statusText.text = isReady ? "R" : "NR";

            if (isLocalPlayer)
            {
                actionButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "N-Ready" : "Ready";
            }
        }
    }

    public void SetActive(bool value)
    {
        gameObject.SetActive(value);
    }
}
