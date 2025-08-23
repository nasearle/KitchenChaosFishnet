using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {
    
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private LobbyCreateUI lobbyCreateUI;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Start() {
        mainMenuButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        joinCodeButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });

        KitchenGameLobby.Instance.OnLobbyJoined += KitchenGameLobbyOnLobbyJoined;

        playerNameInputField.text = KitchenGameLobby.Instance.GetPlayerName();
        playerNameInputField.onDeselect.AddListener(async (string newText) => {
            string currentName = KitchenGameLobby.Instance.GetPlayerName();
            
            if (newText != currentName) {
                await KitchenGameLobby.Instance.SetPlayerName(newText);
            }
        });
    }

    private void KitchenGameLobbyOnLobbyJoined(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        lobbyCodeText.text = "Lobby Code: " + joinedLobby.LobbyCode;
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyJoined -= KitchenGameLobbyOnLobbyJoined;
    }
}
