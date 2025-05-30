using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class PowerUpRandom : NetworkBehaviour
{
    public Image powerUpImage;
    public Sprite[] powerUpSprites;

    [Header("Prefab PowerUps")]
    public NetworkObject bananaPowerUpPrefab;
    public NetworkObject lightningPowerUpPrefab;
    public NetworkObject missilePowerUpPrefab;
    public NetworkObject oilbullterPowerUpPrefab;
    public NetworkObject shieldPowerUpPrefab;

    [Header("Hold Point")]
    public Transform holdPoint;
    public Transform holdPoint2;
    public Transform holdPoint3;
    
    [Header("Nitro Effects")]
    public ParticleSystem nitroEffectPrefab;
    public Transform nitroPointLeft;
    public Transform nitroPointRight;

    [Header("AudioClips")] 
    private AudioSource audioSource;
    public AudioClip nitroSound;
    
    [Header("UI")]
    public GameObject arrowUI;
    
    public float missileSpeed = 15f;
    
    private NetworkObject shieldInstance;
    private bool isShieldActive = false;

    private NetworkObject heldPowerUp;
    private NetworkVariable<ulong> heldPowerUpId = new NetworkVariable<ulong>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public enum PowerUpType { Banana, Lightning, Missile, Nitro, Oil, Shield, None }
    public PowerUpType currentPowerUp = PowerUpType.None;

    public bool isHoldingPowerUp = false;
    
    public KartController kartController; 

    public override void OnNetworkSpawn()
    {
        audioSource = GetComponent<AudioSource>();
        
        if (kartController == null)
        {
            kartController = GetComponentInParent<KartController>();
        }
        Debug.Log($"[OnNetworkSpawn] isOwner={IsOwner}, isServer={IsServer}");

        if ((IsOwner || IsServer) && holdPoint == null)
        {
            holdPoint = transform.Find("HoldPoint1");
            holdPoint2 = transform.Find("HoldPoint2");
            holdPoint3 = transform.Find("HoldPoint3");
        }
        
        if (nitroPointLeft == null)
            nitroPointLeft = transform.Find("NitroPointLeft");

        if (nitroPointRight == null)
            nitroPointRight = transform.Find("NitroPointRight");
    }

    private void Update()
    {
        if (IsServer && heldPowerUp != null && isHoldingPowerUp)
        {
            UpdatePowerUpPosition();
        }
    }

    private void UpdatePowerUpPosition()
    {
        Transform targetHold = currentPowerUp switch
        {
            PowerUpType.Missile => holdPoint2,
            PowerUpType.Oil => holdPoint2,
            PowerUpType.Shield => kartController.transform,
            _ => holdPoint
        };

        heldPowerUp.transform.position = targetHold.position;
        Quaternion rot = Quaternion.identity;
        if (currentPowerUp == PowerUpType.Missile)
        {
            rot = Quaternion.Euler(-90f, 0f, 0f);
        }
        else
        {
            rot = Quaternion.Euler(-90f, targetHold.rotation.eulerAngles.y, 0f);
        }
        heldPowerUp.transform.rotation = rot;
    }

    public void RandomPowerUp()
    {
        int randomIndex = 2; // Replace with Random.Range(0, powerUpSprites.Length) if needed
        powerUpImage.sprite = powerUpSprites[randomIndex];

        PowerUpType selected = (PowerUpType)randomIndex;
        currentPowerUp = selected;

        Debug.Log($"[RandomPowerUp] Selected: {selected}");
        if (selected != PowerUpType.Nitro && selected != PowerUpType.Shield)
        {
            SpawnPowerUpServerRpc(selected);
        }
    }
    
    public void ClearPowerUpUI()
    {
        if (powerUpImage != null)
        {
            powerUpImage.sprite = null;
            Debug.Log("[ClearPowerUpUI] ✅ PowerUp UI cleared");
        }
    }

    [ServerRpc]
    private void SpawnPowerUpServerRpc(PowerUpType type)
    {
        NetworkObject prefab = type switch
        {
            PowerUpType.Banana => bananaPowerUpPrefab,
            PowerUpType.Missile => missilePowerUpPrefab,
            PowerUpType.Lightning => lightningPowerUpPrefab,
            PowerUpType.Oil => oilbullterPowerUpPrefab,
            PowerUpType.Shield => shieldPowerUpPrefab,
            _ => null
        };

        Transform targetHoldPoint = type switch
        {
            PowerUpType.Missile => holdPoint2,
            PowerUpType.Oil => holdPoint2,
            PowerUpType.Shield => kartController.transform,
            _ => holdPoint
        };

        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnPowerUpServerRpc] Prefab not found for {type}");
            return;
        }

        Quaternion rot = Quaternion.identity;
        if(type == PowerUpType.Missile)
        {
            rot = Quaternion.Euler(-90f, 0f, 0f);
            Debug.Log("[SpawnPowerUpServerRpc] Missile rotation set to -90 degrees on X-axis");
        }
        else
        {
            rot = Quaternion.Euler(-90f, targetHoldPoint.rotation.eulerAngles.y, 0f);
            Debug.Log($"[SpawnPowerUpServerRpc] Rotation set to -90 degrees on X-axis, Y={targetHoldPoint.rotation.eulerAngles.y}");
        }
        var instance = Instantiate(prefab, targetHoldPoint.position, rot);
        instance.SpawnWithOwnership(OwnerClientId);

        heldPowerUp = instance;
        heldPowerUpId.Value = instance.NetworkObjectId;
        isHoldingPowerUp = true;
        currentPowerUp = type;

        var rb = instance.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var col = instance.GetComponent<Collider>();
        if (col) col.enabled = false;

        if (type == PowerUpType.Banana)
        {
            var trap = instance.GetComponentInChildren<BananaTrap>();
            if (trap != null)
                trap.creatorClientId.Value = OwnerClientId;
            else
                Debug.LogWarning($"[SpawnPowerUpServerRpc] No BananaTrap found in {type}");
        }
        
        // Gán parent nếu là shield
        if (type == PowerUpType.Shield)
        {
            instance.transform.SetParent(kartController.transform, false);
            instance.transform.localPosition = Vector3.zero;
            instance.transform.localRotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one * 2.5f;

            StartCoroutine(ShieldDurationCoroutine(instance));
        }

        if (type == PowerUpType.Missile)
        {
            //
            Debug.Log("[SpawnPowerUpServerRpc] ✅ Arrow UI enabled for Missile");
        }

        AttachPowerUpClientRpc(instance.NetworkObjectId, (int)type);
    }

    [ClientRpc]
    private void AttachPowerUpClientRpc(ulong netId, int powerUpType)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var obj))
        {
            if (obj.OwnerClientId != NetworkManager.LocalClientId) return;

            heldPowerUp = obj;
            isHoldingPowerUp = true;

            var rb = obj.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;

            var col = obj.GetComponent<Collider>();
            if (col) col.enabled = false;

            Debug.Log($"[AttachPowerUpClientRpc] Attached {(PowerUpType)powerUpType} with NetId={netId}");
        }
    }

    #region Banana - Effect
    public void TryDropBanana()
    {
        if (!IsOwner || heldPowerUp == null || currentPowerUp != PowerUpType.Banana) return;

        Debug.Log($"[TryDropBanana] Request drop NetId={heldPowerUp.NetworkObjectId}");
        DropBananaRequestServerRpc(); 
    }

    [ServerRpc]
    private void DropBananaRequestServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        var playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        var script = playerObj.GetComponent<PowerUpRandom>();

        ulong bananaId = script.heldPowerUpId.Value;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bananaId, out var banana))
        {
            banana.ChangeOwnership(NetworkManager.ServerClientId);

            Vector3 pos = banana.transform.position + Vector3.up * 0.3f;
            Quaternion rot = Quaternion.Euler(-90f, script.holdPoint.rotation.eulerAngles.y, 0f);

            banana.transform.position = pos;
            banana.transform.rotation = rot;

            Rigidbody rb = banana.GetComponentInChildren<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            Collider col = banana.GetComponentInChildren<Collider>();
            if (col) col.enabled = true;

            EnableBananaColliderClientRpc(banana.NetworkObjectId);
            
            script.heldPowerUp = null;
            script.heldPowerUpId.Value = 0;
            script.isHoldingPowerUp = false;
            script.currentPowerUp = PowerUpType.None;

            Debug.Log($"[DropBananaRequestServerRpc] Banana dropped by client {clientId}");
        }
        else
        {
            Debug.LogWarning($"[DropBananaRequestServerRpc] ❌ Banana not found for NetId={bananaId}");
        }
    }

    [ClientRpc]
    private void EnableBananaColliderClientRpc(ulong bananaId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bananaId, out var obj))
        {
            var col = obj.GetComponentInChildren<Collider>();
            if (col) col.enabled = true;

            var rb = obj.GetComponentInChildren<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            var trap = obj.GetComponent<BananaTrap>();
            if (trap) trap.enabled = true;

            Debug.Log($"[Client] ✅ Banana collider & rigidbody re-enabled for NetId={bananaId}");
        }
    }
    #endregion

    #region Nitro - Effect
    public void UseNitro()
    {
        if (currentPowerUp != PowerUpType.Nitro) return;

        Debug.Log("[UseNitro] ✅ Using Nitro");
        ClearPowerUpUI();
        currentPowerUp = PowerUpType.None;

        if (kartController != null && kartController.IsOwner)
        {
            kartController.StartCoroutine(StartNitroEffect());
        }
        else
        {
            Debug.LogWarning("[UseNitro] ❌ KartController not found or not owner");
        }
    }

    private IEnumerator StartNitroEffect()
    {
        float originalTorque = kartController.motorTorque;
        kartController.motorTorque *= 2f; // tăng tốc tạm thời

        Debug.Log("[StartNitroEffect] 🔥 Nitro boost started");
        if (nitroEffectPrefab != null && nitroPointLeft != null && nitroPointRight != null)
        {
            var left = Instantiate(nitroEffectPrefab, nitroPointLeft.position, nitroPointLeft.rotation);
            var right = Instantiate(nitroEffectPrefab, nitroPointRight.position, nitroPointRight.rotation);

            left.transform.parent = nitroPointLeft;
            right.transform.parent = nitroPointRight;

            left.Play();
            right.Play();
            if (audioSource != null && nitroSound != null)
            {
                audioSource.PlayOneShot(nitroSound);
                Debug.Log("[StartNitroEffect] 🔊 Nitro sound played");
            }

            Destroy(left.gameObject, 3f);
            Destroy(right.gameObject, 3f);
        }
        yield return new WaitForSeconds(3f);

        kartController.motorTorque = originalTorque;
        Debug.Log("[StartNitroEffect] 🧊 Nitro boost ended");
    }
    

    #endregion

    #region Shield - Effect
    public void UseShield()
    {
        if (currentPowerUp != PowerUpType.Shield || isShieldActive) return;

        Debug.Log("[UseShield] 🛡️ Shield activated");
        ClearPowerUpUI();
        currentPowerUp = PowerUpType.None;

        if (IsOwner)
        {
            SpawnPowerUpServerRpc(PowerUpType.Shield);
        }
    }
    
    private IEnumerator ShieldDurationCoroutine(NetworkObject shield)
    {
        isShieldActive = true;
        yield return new WaitForSeconds(5f);

        if (shield != null && shield.IsSpawned)
            shield.Despawn();

        isShieldActive = false;
        Debug.Log("[Shield] ⏱️ Shield expired");
    }

    public bool IsShieldActive()
    {
        return isShieldActive;
    }
    
    public void setDeactiveShield()
    {
        isShieldActive = false;
        if (shieldInstance != null && shieldInstance.IsSpawned)
        {
            shieldInstance.Despawn();
            Debug.Log("[setUnactiveShield] 🛡️ Shield deactivated");
        }
    }
    
    #endregion
    
    public void UseLightning()
    {
        if (currentPowerUp != PowerUpType.Lightning || heldPowerUp == null) return;
        DespawnHeldPowerUpServerRpc(heldPowerUpId.Value);
        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
        heldPowerUpId.Value = 0;
    }

    #region Missile
    
    // [ClientRpc]
    // private void EnableArrowUIClientRpc()
    // {
    //     arrowUI.SetActive(true);
    // }

    public void FireMissile(Vector3 fireDirection)
    {
        if (currentPowerUp != PowerUpType.Missile || heldPowerUp == null) return;

        ulong powerUpId = heldPowerUpId.Value;

        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
        isHoldingPowerUp = false;

        FireMissileServerRpc(powerUpId, fireDirection);
    }
    
    [ServerRpc]
    private void FireMissileServerRpc(ulong powerUpId, Vector3 fireDirection)
    {
        NetworkObject missileObj;
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(powerUpId, out missileObj))
        {
            Debug.LogWarning("Missile not found on server!");
            return;
        }
        
        missileObj.transform.parent = null;
        isHoldingPowerUp = false;

        Vector3 forwardOffset = transform.forward * 3f;
        Vector3 rayOrigin = transform.position + forwardOffset + Vector3.up * 5f; 

        float spawnHeightAboveGround = 1.5f;

        Vector3 finalSpawnPos = rayOrigin;

        if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, 10f, LayerMask.GetMask("Ground")))
        {
            finalSpawnPos = hit.point + Vector3.up * spawnHeightAboveGround;
        }
        else
        {
            finalSpawnPos = transform.position + forwardOffset + Vector3.up * spawnHeightAboveGround;
            Debug.LogWarning("Raycast không trúng terrain - dùng vị trí tạm");
        }

        fireDirection.y = 0f;
        Vector3 adjustedDirection = fireDirection.normalized;
        
        missileObj.transform.position = finalSpawnPos;
        missileObj.transform.rotation = Quaternion.LookRotation(adjustedDirection);

        Rigidbody rb = missileObj.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = false;
            rb.velocity = adjustedDirection * 15f;
        }

        MissileController missile = missileObj.GetComponent<MissileController>();
        if (missile != null)
        {
            missile.SetDirection(adjustedDirection);
        }

        Debug.Log($"🚀 Missile spawned at {finalSpawnPos} | Direction: {adjustedDirection}");
    }

    #endregion

    public void FireOilBullet()
    {
        if (currentPowerUp != PowerUpType.Oil || heldPowerUp == null) return;
        DespawnHeldPowerUpServerRpc(heldPowerUpId.Value);
        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
        heldPowerUpId.Value = 0;
    }

    [ServerRpc]
    private void DespawnHeldPowerUpServerRpc(ulong powerUpId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(powerUpId, out var obj))
        {
            obj.Despawn();
            Debug.Log($"[DespawnHeldPowerUpServerRpc] Despawned object with NetId={powerUpId}");
        }
        else
        {
            Debug.LogWarning($"[DespawnHeldPowerUpServerRpc] ❌ NetId={powerUpId} not found");
        }
    }
}
