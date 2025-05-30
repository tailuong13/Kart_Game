using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class MissileController : NetworkBehaviour
{
    private Vector3 direction;
    public float speed = 15f;
    public NetworkObject explosePrefab;
    
    private AudioSource audioSource;
    public AudioClip explosionSound;

    private bool isFired = false;

    public override void OnNetworkDespawn()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void SetDirection(Vector3 dir)
    {
        dir.y = 0f;
        isFired = true;
        direction = dir.normalized;
        Debug.Log($"🎯 Missile direction set to: {direction}");

        if(IsServer)
        {
            StartCoroutine(AutoDespawnCoroutine());
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || !isFired) return; 

        transform.position += direction * speed * Time.fixedDeltaTime;
    }
    
    private IEnumerator AutoDespawnCoroutine()
    {
        yield return new WaitForSeconds(5f);

        if (this != null && NetworkObject.IsSpawned)
        {
            Debug.Log("⏰ Missile expired after 5s without hitting anything.");
            ExplodeServerRpc();
            NetworkObject.Despawn();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || !isFired) return;
        if (other.CompareTag("Environment"))
        {
            Debug.Log("💥 Missile hit the environment!");
            ExplodeServerRpc();
            NetworkObject.Despawn(); 
            return;
        }

        if (other.CompareTag("Player"))
        {
            var player = other.gameObject;
            var powerUpRandom = player.GetComponent<PowerUpRandom>();
            if (other.TryGetComponent(out KartController kart))
            {
                if (powerUpRandom != null)
                {
                    if (powerUpRandom.IsShieldActive())
                    {
                        Debug.Log("🛡️ Missile hit but shield is active – no stun applied.");
                        NetworkObject.Despawn();
                        powerUpRandom.setDeactiveShield();
                    }
                }
                if (kart.OwnerClientId != OwnerClientId) 
                {
                    Debug.Log($"🚗 Missile hit player {kart.OwnerClientId}");
                    kart.ApplyStunServerRpc(1.5f);
                    ExplodeServerRpc();
                    NetworkObject.Despawn();
                }
                Debug.LogWarning("⚠️ No PowerUpRandom component found on player hit!");
            }
        }
        
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void ExplodeServerRpc()
    {
        var explosion = Instantiate(explosePrefab.gameObject, transform.position, Quaternion.identity);
        explosion.GetComponent<NetworkObject>().Spawn();
        
        if (audioSource != null && explosionSound != null)
        {
            audioSource.PlayOneShot(explosionSound);
            Debug.Log("🔊 Playing explosion sound");
        }
        
        Destroy(explosion, 3f);
    }
}
