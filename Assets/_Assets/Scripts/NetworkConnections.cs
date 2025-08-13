using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;

public class NetworkConnections : NetworkBehaviour {
    private const int MAX_PLAYER_AMOUNT = 4;
    
    public static NetworkConnections Instance { get; private set; }

    private NetworkManager _networkManager;
    
    private void Awake() {
        Instance = this;

        _networkManager = InstanceFinder.NetworkManager;
        
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManagerOnRemoteConnectionState;
    }
    
    private void ServerManagerOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == RemoteConnectionState.Started) {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString()) {
                Debug.Log("Not connecting because the game has already started");
                conn.Kick(KickReason.Unset);
                return;
            }

            if (_networkManager.ServerManager.Clients.Keys.Count > MAX_PLAYER_AMOUNT) {
                Debug.Log("Not connecting because the game is full");
                conn.Kick(KickReason.Unset);
                return;
            }
        }
    }

    private void OnDestroy() {
        _networkManager.ServerManager.OnRemoteConnectionState -= ServerManagerOnRemoteConnectionState;
    }
}
