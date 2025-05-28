using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WelcomePanerUI : MonoBehaviour
{
    public TextMeshProUGUI welcomeText;  

    void Start()
    {
        string playerName = "Player"; 

        if (PlayerSession.Instance != null && !string.IsNullOrEmpty(PlayerSession.Instance.PlayerName))
        {
            playerName = PlayerSession.Instance.GetPlayerName();
        }

        welcomeText.text = $"Welcome, {playerName}!";
    }
}