using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerUp : MonoBehaviour
{
    private HashSet<Transform> playersReceived = new();
    public AudioClip pickupSound;

    private void Start()
    {
        foreach (var box in GetComponentsInChildren<MysteryBox>())
        {
            box.SetupAudio(pickupSound);
        }
    }
    
    public bool CanPlayerReceive(Transform player)
    {
        return !playersReceived.Contains(player);
    }

    public void MarkPlayerReceived(Transform player)
    {
        if (!playersReceived.Contains(player))
        {
            playersReceived.Add(player);
        }
    }

    public void ResetGroup()
    {
        playersReceived.Clear();
        
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(true);
        }
        
        Debug.Log($"Reset PowerUp Group: {gameObject.name}");
    }
}
