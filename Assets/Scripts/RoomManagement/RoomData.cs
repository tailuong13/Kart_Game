using System.Collections.Generic;
using System.Linq;

public class RoomData
{
    public string RoomId;
    public ushort Port;
    
    public bool IsMatchmaking = false; 
    public ulong? HostClientId;
    public List<ulong> Players = new List<ulong>();
    public Dictionary<ulong, bool> ReadyStates = new Dictionary<ulong, bool>();
    public int MaxPlayers = 2;
    public enum RoomStatus { Waiting, Full, InGame }
    public RoomStatus Status = RoomStatus.Waiting;
    public int SelectedMapIndex = 0; 

    public bool IsFull => Players.Count >= MaxPlayers;

    public void AddPlayer(ulong clientId)
    {
        if (!Players.Contains(clientId))
        {
            Players.Add(clientId);
            ReadyStates[clientId] = false; 
        }
    }

    public void RemovePlayer(ulong clientId)
    {
        Players.Remove(clientId);
    }
    
    public bool AllReady => Players.Count > 0 && ReadyStates.Values.All(r => r);
}