using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CountDownUI : MonoBehaviour
{
    public GameObject countdownPanel;
    public TextMeshProUGUI countdownText;

    public void StartCountdown()
    {
        StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        countdownPanel.SetActive(true);

        string[] countTexts = { "3", "2", "1", "START!" };

        foreach (string text in countTexts)
        {
            countdownText.text = text;
            yield return new WaitForSeconds(1f);
        }

        countdownPanel.SetActive(false);
    }
}