using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuMusicManager : MonoBehaviour
{
    public static MenuMusicManager Instance { get; private set; }
    
    public AudioSource menuMusic; 

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        PlayMenuMusic();
        DontDestroyOnLoad(gameObject); 
    }

    private void PlayMenuMusic()
    {
        if (menuMusic != null)
        {
            menuMusic.Play();
        }
        else
        {
            Debug.LogWarning("Menu music AudioSource is not assigned.");
        }
    }

    private void OnDestroy()
    {
        if (menuMusic != null)
        {
            menuMusic.Stop();
        }
    }
}
