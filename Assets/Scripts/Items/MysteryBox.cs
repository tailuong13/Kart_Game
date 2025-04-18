using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryBox : MonoBehaviour
{
    [Header("Rotation Settings")]
    public float rotationSpeed = 100f; 

    [Header("Debug")]
    public bool enableDebug = true; 
    
    private PowerUp parentGroup;
    
    public PowerUpRandom powerUpRandom;
    
    public AudioClip pickupSound;
    private AudioSource audioSource;
    
    
    private void Start()
    {
        parentGroup = GetComponentInParent<PowerUp>();
        if (parentGroup == null)
        {
            Debug.LogWarning("MysteryBox không tìm thấy PowerUps cha!");
        }
        
        if (powerUpRandom == null)
        {
            powerUpRandom = FindObjectOfType<PowerUpRandom>();
        }
    }
    
    private void Update()
    {
        RotateBox();
    }

    private void RotateBox()
    {
        transform.Rotate(0, rotationSpeed * Time.deltaTime, 0);
    }
    
    public void SetupAudio(AudioClip clip)
    {
        pickupSound = clip;
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();
        }

        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 1f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        if (parentGroup == null) return;

        if (parentGroup.CanPlayerReceive(other.transform))
        {
            if (enableDebug)
            {
                Debug.Log($"Player {other.name} nhận MysteryBox tại {transform.name}");
            }
            
            audioSource.PlayOneShot(pickupSound);
            PowerUpRandom powerUp = other.GetComponentInChildren<PowerUpRandom>();
            if (powerUp != null)
            {
                powerUp.RandomPowerUp();
            }
            else
            {
                Debug.LogError("❌ Không tìm thấy PowerUpRandom trong player!");
            }

            parentGroup.MarkPlayerReceived(other.transform);
            HandlePlayerCollision();
            StartCoroutine(DisableAfterSound());
        }
        else
        {
            if (enableDebug)
                Debug.Log($"Player {other.name} đã nhận MysteryBox tại cụm này rồi!");
        }
    }

    private void HandlePlayerCollision()
    {
        Debug.Log("Mystery Box effect activated!");
    }
    
    private IEnumerator DisableAfterSound()
    {
        yield return new WaitForSeconds(pickupSound.length); // chờ âm thanh phát xong
        gameObject.SetActive(false);
    }

    private void RespawnBox()
    {
        gameObject.SetActive(true);
    }
}
