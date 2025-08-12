using System;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Multiplayer.Playmode;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class TestingLobbyUI : MonoBehaviour {
    [SerializeField] private Button joinGameButton;
    
    private NetworkManager _networkManager;

    private void Awake() {
        _networkManager = InstanceFinder.NetworkManager;
        
        _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            Loader.LoadNetwork(Loader.Scene.CharacterSelectScene);
        }
    }

    private void Start() {
        if (CurrentPlayer.ReadOnlyTags().Length > 0 && CurrentPlayer.ReadOnlyTags()[0] == "server") {
            LobbyPlayerConnections.Instance.StartServer();
        }
        
        joinGameButton.onClick.AddListener(() => {
            LobbyPlayerConnections.Instance.StartClient();
        });
        
        // if (LobbyPlayerConnections.Instance != null) {
        //     joinGameButton.onClick.AddListener(() => {
        //         LobbyPlayerConnections.Instance.StartClient();
        //     });
        // } else {
        //     LobbyPlayerConnections.OnAnyLobbyPlayerConnectionSpawned += LobbyPlayerConnectionsOnAnyLobbyPlayerConnectionSpawned;
        // }
        
    }

    // private void LobbyPlayerConnectionsOnAnyLobbyPlayerConnectionSpawned(object sender, EventArgs e) {
    //     joinGameButton.onClick.AddListener(() => {
    //         LobbyPlayerConnections.Instance.StartClient();
    //     });
    // }
}
