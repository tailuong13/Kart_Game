using UnityEngine;
using TMPro;
using Unity.Netcode;

public class RaceProgressUI : NetworkBehaviour
{
    [Header("HUD UI References")]
    [SerializeField] private TextMeshProUGUI lapTimeText;
    [SerializeField] private TextMeshProUGUI totalTimeText;

    // Thêm UI countdown (Text hoặc panel)
    [SerializeField] private GameObject countdownPanel;
    [SerializeField] private TextMeshProUGUI countdownText;

    private float currentLapTime = 0f;
    private float totalRaceTime = 0f;
    private bool finished = false;

    private bool countdownActive = false;
    private float countdownTime = 0f;
    
    public bool raceStarted = false;

    public static float FinalTime { get; private set; } 

    private void Start()
    {
        if (IsOwner)
        {
            if (countdownPanel != null)
                countdownPanel.SetActive(false);
        }
    }

    private void Update()
    {   
        if (!IsOwner) return;

        if (!finished && raceStarted)
        {
            currentLapTime += Time.deltaTime;
            totalRaceTime += Time.deltaTime;

            lapTimeText.text = $"Lap Time: {FormatTime(currentLapTime)}";
            totalTimeText.text = $"Total: {FormatTime(totalRaceTime)}";
        }
        UpdateCountdown();
    }
    
    public void StartRace()
    {
        raceStarted = true;
        Debug.Log("🚦 Cuộc đua bắt đầu! Bắt đầu tính giờ.");
    }

    // Cập nhật hiển thị countdown
    private void UpdateCountdown()
    {
        if (CheckPointsSystem.Instance == null)
        {
            Debug.LogWarning("CheckPointsSystem.Instance is null. Countdown cannot be updated.");
            return;
        }

        bool isActive = CheckPointsSystem.Instance.GetCountdownActive();
        float timeLeft = CheckPointsSystem.Instance.GetCountdownTime();

        if (isActive && !countdownActive)
        {
            countdownActive = true;
            if (countdownPanel != null)
            {
                countdownPanel.SetActive(true);
                Debug.Log("Countdown started!");
            }
        }

        if (countdownActive)
        {
            countdownTime = timeLeft;
            if (countdownText != null)
                countdownText.text = $"Race ends in: {Mathf.CeilToInt(countdownTime)}s";

            if (countdownTime <= 0f)
            {
                countdownActive = false;
                if (countdownPanel != null)
                    countdownPanel.SetActive(false);
            }
        }
    }

    [ClientRpc]
    public void ResetLapTimerClientRpc()
    {
        if (!IsOwner) return;
        currentLapTime = 0f;
    }

    [ClientRpc]
    public void MarkFinishClientRpc()
    {
        if (!IsOwner) return;
        finished = true;
        Debug.Log("[RaceProgressUI] 🏁 Đã hoàn thành cuộc đua!");
        FinalTime = totalRaceTime;
        Debug.Log($"🏁 Final Time của {OwnerClientId} là {FormatTime(FinalTime)}");
        SubmitFinalTimeServerRpc(FinalTime);
    }

    private string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        int millis = Mathf.FloorToInt((time * 1000f) % 1000f);
        return $"{minutes:00}:{seconds:00}.{millis:000}";
    }
    
    [ServerRpc]
    public void SubmitFinalTimeServerRpc(float time, ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        Debug.Log($"✅ Nhận final time từ client {clientId}: {time}");

        //TODO: Lưu vào hệ thống xếp hạng
    }

    public float GetTotalTime() => totalRaceTime;
}
