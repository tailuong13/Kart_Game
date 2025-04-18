using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MysteryBoxesSpawner : MonoBehaviour
{
   [SerializeField] private List<Transform> spawnPoints;
   [SerializeField] private GameObject powerUpsParent;
   
   private List<GameObject> spawnedPowerUps = new();         // Danh sách các PowerUps đã spawn

   private void Start()
   {
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
         spawnedPowerUps.Add(powerUps);
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
