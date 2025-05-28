using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerSession : MonoBehaviour
{
    public static PlayerSession Instance { get; private set; }

    public string PlayerName { get; set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); 
    }
    
    public void SetPlayerName(string name)
    {
        PlayerName = name;
    }

    public string GetPlayerName()
    {
        return PlayerName;
    }
}
