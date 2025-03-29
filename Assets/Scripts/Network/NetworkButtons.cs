using Unity.Netcode;
using UnityEngine;

public class NetworkButtons : MonoBehaviour
{
    private void OnGUI()
    {
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontSize = 40, 
            fixedHeight = 100 
        };

        GUIStyle labelStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 50, 
            fontStyle = FontStyle.Bold
        };

        GUILayout.BeginArea(new Rect(100, 100, 600, 800)); 

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host", buttonStyle)) NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Server", buttonStyle)) NetworkManager.Singleton.StartServer();
            if (GUILayout.Button("Client", buttonStyle)) NetworkManager.Singleton.StartClient();
        }

        if (NetworkManager.Singleton.IsServer)
        {
            GUILayout.Label($"Player Count: {NetworkManager.Singleton.ConnectedClients.Count}", labelStyle);
        }

        GUILayout.EndArea();
    }
}