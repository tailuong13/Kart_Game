using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class MatchMakingUIController : MonoBehaviour
{
    public GameObject matchmakingPanel;
    public TextMeshProUGUI timerText;
    public Button cancelButton;

    private float elapsedTime;
    private bool isSearching = false;

    public System.Action OnCancelSearch; 

    private void Start()
    {
        cancelButton.onClick.AddListener(CancelSearch);
        matchmakingPanel.SetActive(false);
    }

    public void StartSearch()
    {
        elapsedTime = 0f;
        isSearching = true;
        matchmakingPanel.SetActive(true);
        
    }

    public void CancelSearch()
    {
        isSearching = false;
        matchmakingPanel.SetActive(false);
        OnCancelSearch?.Invoke(); 
    }

    private void Update()
    {
        if (isSearching)
        {
            elapsedTime += Time.deltaTime;
            int minutes = Mathf.FloorToInt(elapsedTime / 60f);
            int seconds = Mathf.FloorToInt(elapsedTime % 60f);
            timerText.text = $"Time: {minutes:00}:{seconds:00}";
        }
    }
}