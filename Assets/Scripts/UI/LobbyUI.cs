// using System.Collections.Generic;
// using Unity.Netcode;
// using UnityEngine;
// using UnityEngine.UI;
// using TMPro;
// using Unity.Collections;
//
// public class LobbyUIManager : NetworkBehaviour
// {
//     public static LobbyUIManager Instance;
//
//     [Header("Room Info")]
//     public TMP_Text roomNameText;
//
//     [Header("Map Selection")]
//     public Image mapImage;
//     public TMP_Text mapNameText;
//     public Button prevMapBtn;
//     public Button nextMapBtn;
//
//     [Header("Player Slots")]
//     public List<PlayerSlotUI> playerSlots; // Player1 -> Player6
//
//     [Header("Buttons")]
//     public Button readyButton;
//     public Button startButton;
//
//     private string[] mapNames = { "Map1", "Map2", "Map3" };
//     private int currentMapIndex = 0;
//
//     private bool isReady = false;
//
//     private void Awake()
//     {
//         if (Instance == null) Instance = this;
//         else Destroy(gameObject);
//     }
//
//     private void Start()
//     {
//         prevMapBtn.onClick.AddListener(OnPrevMapClicked);
//         nextMapBtn.onClick.AddListener(OnNextMapClicked);
//         readyButton.onClick.AddListener(OnReadyClicked);
//         startButton.onClick.AddListener(OnStartGameClicked);
//
//         UpdateMapUI();
//         startButton.gameObject.SetActive(false);
//
//         if (!IsHost)
//         {
//             prevMapBtn.gameObject.SetActive(false);
//             nextMapBtn.gameObject.SetActive(false);
//             startButton.gameObject.SetActive(false);
//         }
//     }
//
//     public void SetRoomName(string roomName)
//     {
//         roomNameText.text = roomName;
//     }
//
//     public void UpdatePlayerSlot(int index, string playerName, bool ready, string car, string character)
//     {
//         if (index < 0 || index >= playerSlots.Count) return;
//         playerSlots[index].SetPlayer(playerName, ready, car, character);
//     }
//
//     public void UpdateMapUI()
//     {
//         mapNameText.text = mapNames[currentMapIndex];
//         // mapImage.sprite = your map sprite logic here
//     }
//
//     private void OnPrevMapClicked()
//     {
//         currentMapIndex = (currentMapIndex - 1 + mapNames.Length) % mapNames.Length;
//         UpdateMapUI();
//         UpdateMapServerRpc(currentMapIndex);
//     }
//
//     private void OnNextMapClicked()
//     {
//         currentMapIndex = (currentMapIndex + 1) % mapNames.Length;
//         UpdateMapUI();
//         UpdateMapServerRpc(currentMapIndex);
//     }
//
//     private void OnReadyClicked()
//     {
//         isReady = !isReady;
//         readyButton.GetComponentInChildren<TMP_Text>().text = isReady ? "Unready" : "Ready";
//         SetReadyServerRpc(isReady);
//     }
//
//     private void OnStartGameClicked()
//     {
//         StartGameServerRpc();
//     }
//
//     [ServerRpc(RequireOwnership = false)]
//     private void SetReadyServerRpc(bool ready, ServerRpcParams rpcParams = default)
//     {
//         RoomManager.Instance.SetPlayerReadyState(rpcParams.Receive.SenderClientId, ready);
//     }
//
//     [ServerRpc(RequireOwnership = false)]
//     private void UpdateMapServerRpc(int newMapIndex)
//     {
//         RoomManager.Instance.UpdateRoomMapIndex(newMapIndex);
//     }
//
//     [ServerRpc(RequireOwnership = false)]
//     private void StartGameServerRpc()
//     {
//         RoomManager.Instance.TryStartGame();
//     }
//
//     [ClientRpc]
//     public void ReceiveAllPlayerInfoClientRpc(FixedString64Bytes[] names, bool[] readyStates, FixedString64Bytes[] cars, FixedString64Bytes[] characters)
//     {
//         for (int i = 0; i < names.Length && i < playerSlots.Count; i++)
//         {
//             UpdatePlayerSlot(i, names[i].ToString(), readyStates[i], cars[i].ToString(), characters[i].ToString());
//         }
//     }
//
//     [ClientRpc]
//     public void UpdateMapClientRpc(int index)
//     {
//         currentMapIndex = index;
//         UpdateMapUI();
//     }
//
//     [ClientRpc]
//     public void EnableStartButtonClientRpc(bool enable)
//     {
//         startButton.gameObject.SetActive(enable);
//     }
// }  
//
// [System.Serializable]
// public class PlayerSlotUI
// {
//     public GameObject root;
//     public TMP_Text nameText;
//     public TMP_Text statusText;
//     public TMP_Dropdown carDropdown;
//     public TMP_Dropdown characterDropdown;
//
//     public void SetPlayer(string playerName, bool isReady, string car, string character)
//     {
//         nameText.text = playerName;
//         statusText.text = isReady ? "Ready" : "Non-Ready";
//         carDropdown.value = carDropdown.options.FindIndex(opt => opt.text == car);
//         characterDropdown.value = characterDropdown.options.FindIndex(opt => opt.text == character);
//     }
// }

using UnityEngine;
using UnityEngine.UI;  // Nếu dùng UnityEngine.UI.Text
using TMPro;           // Nếu dùng TextMeshProUGUI

public class LobbyUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI roomNameText;        
    
    private void Start()
    {
        string roomName = TempRoomData.Instance.RoomName;
        
        if (roomNameText != null)
        {
            roomNameText.text = $"Room: {roomName}";
        }
        else
        {
            Debug.LogWarning("RoomNameText chưa được gán trong Inspector.");
        }
    }
}
