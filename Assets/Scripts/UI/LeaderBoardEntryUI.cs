using TMPro;
using Unity.Collections;
using UnityEngine;

public class LeaderBoardEntryUI : MonoBehaviour
{
    public TextMeshProUGUI PositionText;
    public TextMeshProUGUI PlayerNameText;
    public TextMeshProUGUI LapText;

    public void SetEntry(int position, FixedString32Bytes playerName, int lap)
    {
        PositionText.text = $"#{position}";
        PlayerNameText.text = $"{playerName}";
        LapText.text = $"{lap}";
    }
}