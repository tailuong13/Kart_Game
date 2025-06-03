using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CreateRoomUI : MonoBehaviour
{
    public static CreateRoomUI Instance;

    public GameObject panel;
    public TMP_InputField roomNameInput;
    public Button createButton;
    public Button cancelButton;
    public ClientConnectManager clientConnectManager;

    private void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        if (clientConnectManager == null)
            clientConnectManager = FindObjectOfType<ClientConnectManager>();
        createButton.onClick.AddListener(OnCreateClicked);
        cancelButton.onClick.AddListener(() => panel.SetActive(false));
    }

    public void ShowPanel()
    {
        roomNameInput.text = "";
        panel.SetActive(true);
    }

    private void OnCreateClicked()
    {
        string roomName = roomNameInput.text.Trim();

        if (string.IsNullOrEmpty(roomName))
        {
            Debug.LogWarning("Tên phòng không được để trống.");
            return;
        }

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsHost)
        {
            clientConnectManager.ConnectToServer();
            bool clientStarted = NetworkManager.Singleton.IsClient;
            Debug.Log("StartClient() trước khi tạo phòng: " + clientStarted);

            if (clientStarted)
            {
                NetworkManager.Singleton.OnClientConnectedCallback += (clientId) =>
                {
                    if (NetworkManager.Singleton.LocalClientId == clientId)
                    {
                        RoomManager.Instance.CreateRoomServerRpc(roomName);
                        Debug.Log($"Gửi yêu cầu tạo phòng: {roomName}");
                        NetworkManager.Singleton.OnClientConnectedCallback -= null; // bỏ callback sau khi gọi
                    }
                };
            }
            else
            {
                Debug.LogError("Không thể start client!");
                return;
            }
        }
        else
        {
            // Đã là client hoặc host rồi, gọi luôn
            RoomManager.Instance.CreateRoomServerRpc(roomName);
            Debug.Log($"Gửi yêu cầu tạo phòng: {roomName}");
        }

        panel.SetActive(false);
    }
}