using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUI : MonoBehaviour
{
    public TMP_InputField usernameInput;

    public void OnSubmit()
    {
        string username = usernameInput.text.Trim();
        if (!string.IsNullOrEmpty(username))
        {
            PlayerSession.Instance.SetPlayerName(username);
            LoadMultiplayerScene();
        }
    }

    private void LoadMultiplayerScene()
    {
        SceneManager.LoadScene("MultiplayerScene", LoadSceneMode.Single);
    }
}