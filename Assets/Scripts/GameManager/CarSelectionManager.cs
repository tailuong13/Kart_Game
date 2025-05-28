using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using Unity.Netcode;

public class CarSelectionManager : NetworkBehaviour
{
    public struct PlayerSelection
    {
        public int CarId;
        public int CharacterId;
    }
    public static CarSelectionManager Instance;
    private Dictionary<ulong, PlayerSelection> playerSelections = new();

    private void Awake() => Instance = this;

    public void PlayerSelected(ulong clientId, int carId, int characterId)
    {
        if (playerSelections.ContainsKey(clientId)) return;

        playerSelections[clientId] = new PlayerSelection
        {
            CarId = carId,
            CharacterId = characterId
        };

        Debug.Log($"Player {clientId} chọn xe {carId}, nhân vật {characterId}");

        if (playerSelections.Count >= NetworkManager.Singleton.ConnectedClients.Count)
        {
            Debug.Log("Tất cả đã chọn xe & nhân vật → chuyển map");
            string[] maps = { "Map1Scene" };
            string chosenMap = maps[Random.Range(0, maps.Length)];
            StartCoroutine(WaitThenStartGame(chosenMap));
        }
    }
    
    public PlayerSelection GetPlayerSelection(ulong clientId)
    {
        if (playerSelections.TryGetValue(clientId, out var selection))
            return selection;
        else
            return new PlayerSelection { CarId = 0, CharacterId = 0 };
    }
    

    private IEnumerator WaitThenStartGame(string choosenMap)
    {
        yield return new WaitForSeconds(1f); 
        Debug.Log("✅ Start game now!");
        GameFlowManager.Instance.SelectedMap.Value = new FixedString32Bytes(choosenMap);
        GameFlowManager.Instance.StartGame();
    }
}