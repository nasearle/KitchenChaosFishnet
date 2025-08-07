using System;
using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour {
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    
    private void Start() {
        createGameButton.onClick.AddListener(() => {
            // LobbyPlayerConnections.Instance.StartServer();
            // Need to wait for server to be active before trying to change the scene.
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        });
        
        joinGameButton.onClick.AddListener(() => {
            LobbyPlayerConnections.Instance.StartClient();
        });
    }
}
