using TMPro;
using UnityEngine;

public class LeaderBoardEntryUI : MonoBehaviour
{
    public TextMeshProUGUI PositionText;
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI LapText;

    public void SetEntry(int position, ulong playerId, int lap)
    {
        PositionText.text = $"#{position}";
        PlayerNameText.text = $"Player {playerId}";
        LapText.text = $"Lap {lap}";
    }
}