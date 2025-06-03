using UnityEngine;

public class TempRoomData : MonoBehaviour
{
    public static TempRoomData Instance;

    public string RoomName;
    public ushort RoomPort;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(this);
    }
}