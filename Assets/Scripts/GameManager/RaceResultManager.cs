using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RaceResultManager : MonoBehaviour
{
    public static RaceResultManager Instance;

    public class RaceResult
    {
        public string PlayerName;
        public float FinishTime;
        public int CarId;
        public int Rank;
    }

    public List<RaceResult> Results = new();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void ClearResults() => Results.Clear();
}

