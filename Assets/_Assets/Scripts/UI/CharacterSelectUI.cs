using System;
using FishNet;
using FishNet.Managing;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour {
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private NetworkManager _networkManager;

    private void Awake() {
        _networkManager = InstanceFinder.NetworkManager;

        if (_networkManager.IsClientStarted) {
            mainMenuButton.onClick.AddListener((() => {
                KitchenGameLobby.Instance.LeaveLobby();
                _networkManager.ClientManager.StopConnection();
                Loader.Load(Loader.Scene.MainMenuScene);
            }));
            readyButton.onClick.AddListener((() => {
                CharacterSelectReady.Instance.SetPlayerReady();
            }));
        }
    }

    void Start() {
        if (_networkManager.IsClientStarted) {
            Lobby lobby = KitchenGameLobby.Instance.GetLobby();

            lobbyNameText.text = "Lobby Name: " + lobby.Name;
            lobbyCodeText.text = "Lobby Code: " + lobby.LobbyCode;
        }
    }
}
