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
