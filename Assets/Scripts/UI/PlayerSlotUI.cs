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
        clientId = playerInfo.ClientId;
        playerNameText.text = playerInfo.PlayerName.ToString();
        hostIcon.SetActive(playerInfo.IsHost);

        isLocalPlayer = isLocal;
        isLocalPlayerHost = isLocalHost;
        isReady = playerInfo.IsReady;

        UpdateStatusText();

        if (isLocalPlayerHost && playerInfo.IsHost)
        {
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = "Start";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                GameFlowManager.Instance.StartGame_ServerRpc();
            });

            statusText.text = ""; 
        }
        else if (IsLocalClient(playerInfo.ClientId))
        {
            actionButton.GetComponentInChildren<TextMeshProUGUI>().text = isReady ? "N-Ready" : "Ready";
            actionButton.onClick.RemoveAllListeners();
            actionButton.onClick.AddListener(() =>
            {
                GameFlowManager.Instance.ToggleReady_ServerRpc();
            });
            UpdateStatusText();
        }
        else
        {
            actionButton.gameObject.SetActive(false);
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
        statusText.text = isReady ? "R" : "NR";
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
