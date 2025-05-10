using System;
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

    private NetworkObject heldPowerUp;

    public enum PowerUpType {  Banana,Lightning, Missile, Nitro, Oil,  Shield, None }
    public PowerUpType currentPowerUp = PowerUpType.None;

    private ulong heldId;

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[OnNetworkSpawn] isOwner={IsOwner}, isServer={IsServer}");

        if ((IsOwner || IsServer) && holdPoint == null)
        {
            holdPoint = transform.Find("HoldPoint1");
            Debug.Log($"[OnNetworkSpawn] HoldPoint auto-assigned: {holdPoint?.name}");
        }
        
        if ((IsOwner || IsServer) && holdPoint2 == null)
        {
            holdPoint2 = transform.Find("HoldPoint2");
            Debug.Log($"[OnNetworkSpawn] HoldPoint auto-assigned: {holdPoint2?.name}");
        }
        
        if ((IsOwner || IsServer) && holdPoint3 == null)
        {
            holdPoint2 = transform.Find("HoldPoint3");
            Debug.Log($"[OnNetworkSpawn] HoldPoint auto-assigned: {holdPoint3?.name}");
        }
    }

    private void Update()
    {
        if (IsServer && heldPowerUp != null)
        {
            UpdatePowerUpPosition();
        }
    }

    private void UpdatePowerUpPosition()
    {
        Transform targetHold = currentPowerUp == PowerUpType.Missile ? holdPoint2 : holdPoint;
        
        heldPowerUp.transform.position = targetHold.position;
        Quaternion rot = Quaternion.Euler(-90f, targetHold.rotation.eulerAngles.y, 0f);
        heldPowerUp.transform.rotation = rot;
    }

    public void RandomPowerUp()
    {
        int randomIndex = 5; // Random.Range(0, powerUpSprites.Length);
        powerUpImage.sprite = powerUpSprites[randomIndex];

        PowerUpType selected = (PowerUpType)randomIndex;
        currentPowerUp = selected;
        
        Debug.Log($"[RandomPowerUp] Selected: {selected}");

        SpawnPowerUpServerRpc(selected);
    }

    [ServerRpc]
    private void SpawnPowerUpServerRpc(PowerUpType type)
    {
        NetworkObject prefab = null;
        Transform targetHoldPoint = holdPoint;

        switch (type)
        {
            case PowerUpType.Banana:
                prefab = bananaPowerUpPrefab;
                break;
            case PowerUpType.Missile:
                prefab = missilePowerUpPrefab;
                targetHoldPoint = holdPoint2;
                break;
            case PowerUpType.Lightning:
                prefab = lightningPowerUpPrefab;
                break;
            case PowerUpType.Oil:
                prefab = oilbullterPowerUpPrefab;
                targetHoldPoint = holdPoint2;
                break;
            case PowerUpType.Shield:
                prefab = shieldPowerUpPrefab;
                targetHoldPoint = holdPoint3;
                break;
        }

        if (prefab == null)
        {
            Debug.LogWarning($"[SpawnPowerUpServerRpc] Prefab not found for {type}");
            return;
        }

        Quaternion rot = Quaternion.Euler(-90f, targetHoldPoint.rotation.eulerAngles.y, 0f);
        var instance = Instantiate(prefab, targetHoldPoint.position, rot);
        instance.SpawnWithOwnership(OwnerClientId);

        Debug.Log($"[SpawnPowerUpServerRpc] Spawn position: {targetHoldPoint.name} at {targetHoldPoint.position}");
        
        heldPowerUp = instance;
        heldId = instance.NetworkObjectId;

        Debug.Log($"[SpawnPowerUpServerRpc] Spawned {type} with NetId={heldId}");

        var rb = instance.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        var col = instance.GetComponent<Collider>();
        if (col) col.enabled = false;

        if (type == PowerUpType.Banana)
        {
            var trap = instance.GetComponent<BananaTrap>();
            if (trap != null) trap.creatorClientId = OwnerClientId;
        }

        AttachPowerUpClientRpc(instance.NetworkObjectId, (int)type);
    }

    [ClientRpc]
    private void AttachPowerUpClientRpc(ulong netId, int powerUpType)
    {
        if (!IsOwner) return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(netId, out var obj))
        {
            heldPowerUp = obj;
            heldId = netId;

            var col = heldPowerUp.GetComponent<Collider>();
            if (col) col.enabled = false;

            var rb = heldPowerUp.GetComponent<Rigidbody>();
            if (rb) rb.isKinematic = true;

            Debug.Log($"[AttachPowerUpClientRpc] Attached {((PowerUpType)powerUpType)} with NetId={netId}");
        }
        else
        {
            Debug.LogWarning($"[AttachPowerUpClientRpc] Could not find object with NetId={netId}");
        }
    }

    public void TryDropBanana()
    {
        if (heldPowerUp == null) return;

        Debug.Log($"[TryDropBanana] Dropping banana NetId={heldId}");

        heldPowerUp.ChangeOwnership(NetworkManager.ServerClientId);

        Vector3 pos = heldPowerUp.transform.position;
        Quaternion rot = Quaternion.Euler(-90f, holdPoint.rotation.eulerAngles.y, 0f);

        DropBananaServerRpc(pos, rot, heldId);
        heldPowerUp = null;
    }

    [ServerRpc]
    private void DropBananaServerRpc(Vector3 position, Quaternion rotation, ulong bananaId)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(bananaId, out var banana))
        {
            banana.transform.position = position;
            banana.transform.rotation = rotation;

            var rb = banana.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            var col = banana.GetComponent<Collider>();
            if (col) col.enabled = true;

            var trap = banana.GetComponent<BananaTrap>();
            if (trap) trap.enabled = true;

            Debug.Log($"[DropBananaServerRpc] Dropped Banana at {position}");
        }
        else
        {
            Debug.LogWarning($"[DropBananaServerRpc] ❌ Không tìm thấy Banana với NetId={bananaId}");
        }
    }

    public void UseLightning()
    {
        if (currentPowerUp != PowerUpType.Lightning || heldPowerUp == null) return;

        Debug.Log("[UseLightning] Lightning used & destroyed");
        DespawnHeldPowerUpServerRpc(heldId);
        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
    }

    public void FireMissile()
    {
        if (currentPowerUp != PowerUpType.Missile || heldPowerUp == null) return;

        Debug.Log("[FireMissile] Missile fired & destroyed");
        DespawnHeldPowerUpServerRpc(heldId);
        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
    }
    
    public void FireOilBullet()
    {
        if (currentPowerUp != PowerUpType.Oil || heldPowerUp == null) return;

        Debug.Log("[FireMissile] Missile fired & destroyed");
        DespawnHeldPowerUpServerRpc(heldId);
        heldPowerUp = null;
        currentPowerUp = PowerUpType.None;
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
            Debug.LogWarning($"[DespawnHeldPowerUpServerRpc] ❌ NetId={powerUpId} không tồn tại");
        }
    }
}
