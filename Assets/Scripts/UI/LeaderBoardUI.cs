using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Collections;
using Unity.Netcode;

public class LeaderBoardUI : NetworkBehaviour
{
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;

    private Dictionary<ulong, GameObject> playerEntries = new();

    private void Start()
    {
        InvokeRepeating(nameof(UpdateLeaderboardUI), 1f, 0.5f); 
    }

    public void UpdateLeaderboardUI(FixedList64Bytes<CheckPointsSystem.LeaderboardEntry> leaderboard)
    {
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        for (int i = 0; i < leaderboard.Length; i++)
        {
            var entryData = leaderboard[i];
            GameObject entry = Instantiate(entryPrefab, entryContainer);
            entry.GetComponent<LeaderBoardEntryUI>().SetEntry(i + 1, entryData.PlayerId, entryData.Lap);

            if (!playerEntries.ContainsKey(entryData.PlayerId))
                playerEntries.Add(entryData.PlayerId, entry);
        }
    }
}