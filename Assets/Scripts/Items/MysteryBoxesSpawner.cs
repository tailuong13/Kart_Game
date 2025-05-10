using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class MysteryBoxesSpawner : MonoBehaviour
{
   [SerializeField] private List<Transform> spawnPoints;
   [SerializeField] private GameObject powerUpsParent;
   [SerializeField] private GameObject mysteryBoxPrefab;
   
   private List<GameObject> spawnedPowerUps = new();         // Danh sách các PowerUps đã spawn

   private void Start()
   {
      StartCoroutine(WaitForNetworkAndSpawn());
   }
   
   private IEnumerator WaitForNetworkAndSpawn()
   {
      // Đợi đến khi NetworkManager hoạt động
      while (!NetworkManager.Singleton.IsListening || !NetworkManager.Singleton.IsServer)
         yield return null;

      SpawnPowerUps();
   }

   public void SpawnPowerUps()
   {
      ClearExisting();

      if (spawnPoints.Count == 0 || powerUpsParent == null)
      {
         Debug.LogWarning("⚠️ SpawnPoints hoặc PowerUps Prefab chưa được gán!");
         return;
      }

      foreach (Transform point in spawnPoints)
      {
         GameObject powerUps = Instantiate(powerUpsParent, point.position, point.rotation);
         var parentNetObj = powerUps.GetComponent<NetworkObject>();
         var groupScript = powerUps.GetComponent<PowerUp>();
         spawnedPowerUps.Add(powerUps);
         
         parentNetObj.Spawn();
         
         
         foreach (Transform child in powerUps.transform)
         {
            if (child.CompareTag("MysteryPlaceholder"))
            {
               GameObject box = Instantiate(mysteryBoxPrefab, child.position, child.rotation);
               var boxNetObj = box.GetComponent<NetworkObject>();
               var boxScript = box.GetComponent<MysteryBox>();

               // GÁN group quản lý để giữ logic nhận 1 box duy nhất
               boxScript.SetParentGroup(groupScript);

               boxNetObj.Spawn();

               // ✅ CHỈ set parent nếu parent là NetworkObject đã spawn
               box.transform.SetParent(powerUps.transform);

               Destroy(child.gameObject); // xoá placeholder
            }
         }
      }

      Debug.Log($"✅ Đã spawn {spawnedPowerUps.Count} nhóm MysteryBox.");
   }

   private void ClearExisting()
   {
      foreach (var go in spawnedPowerUps)
      {
         if (go != null) Destroy(go);
      }

      spawnedPowerUps.Clear();
   }
}
