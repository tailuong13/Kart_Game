using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MysteryBox : NetworkBehaviour
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
        
        SetupAudio(pickupSound);
    }
    
    public override void OnNetworkSpawn()
    {
        if (IsServer && !NetworkObject.IsSpawned)
        {
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
        Debug.Log($"🔥 Trigger by {other.name}, IsServer: {IsServer}");
        if (!IsServer) return;
        if (!other.CompareTag("Player")) return;
        if (parentGroup == null) return;

        if (parentGroup.CanPlayerReceive(other.transform))
        {
            if (enableDebug)
            {
                Debug.Log($"Player {other.name} nhận MysteryBox tại {transform.name}");
            }
            
            audioSource.PlayOneShot(pickupSound);
            KartController kart = other.GetComponent<KartController>();
            if (kart != null)
            {
                kart.RequestRandomPowerUpClientRpc(new ClientRpcParams
                {
                    Send = new ClientRpcSendParams
                    {
                        TargetClientIds = new[] { kart.OwnerClientId }
                    }
                });
            }
            
            parentGroup.MarkPlayerReceived(other.transform);
            HandlePlayerCollision();
            StartCoroutine(HideBoxAfterDelay());
        }
        else
        {
            if (enableDebug)
                Debug.Log($"Player {other.name} đã nhận MysteryBox tại cụm này rồi!");
        }
    }
    
    public void SetParentGroup(PowerUp group)
    {
        parentGroup = group;
    }

    private void HandlePlayerCollision()
    {
        Debug.Log("Mystery Box effect activated!");
    }
    
    private IEnumerator HideBoxAfterDelay()
    {
        yield return new WaitForSeconds(pickupSound != null ? pickupSound.length : 0.5f);

        if (IsServer && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
            Debug.Log($"☠️ MysteryBox {name} đã bị ẩn.");
        }
    }

    private void RespawnBox()
    {
        gameObject.SetActive(true);
    }
}
