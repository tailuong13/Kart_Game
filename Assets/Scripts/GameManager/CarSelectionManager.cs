using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

public class CarSelectionManager : NetworkBehaviour
{
    public static CarSelectionManager Instance;
    private Dictionary<ulong, int> playerCarChoices = new();

    private void Awake() => Instance = this;

    public void PlayerSelectedCar(ulong clientId, int carId)
    {
        if (playerCarChoices.ContainsKey(clientId)) return;

        playerCarChoices[clientId] = 1; //carId
        Debug.Log($"Player {clientId} chọn xe {carId}");

        if (playerCarChoices.Count >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            Debug.Log("Tất cả đã chọn xe → chuyển map");
            string[] maps = { "Map1Scene" };
            string chosenMap = maps[Random.Range(0, maps.Length)];
            StartCoroutine(WaitThenStartGame(chosenMap));
        }
    }
    
    

    private IEnumerator WaitThenStartGame(string choosenMap)
    {
        yield return new WaitForSeconds(1f); // chờ đồng bộ
        Debug.Log("✅ Start game now!");
        GameFlowManager.Instance.SelectedMap.Value = new FixedString32Bytes(choosenMap);
        GameFlowManager.Instance.StartGame();
    }
}