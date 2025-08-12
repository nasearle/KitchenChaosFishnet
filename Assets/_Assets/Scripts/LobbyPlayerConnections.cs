using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;

public class LobbyPlayerConnections : MonoBehaviour {
    private const int MAX_PLAYER_AMOUNT = 4;
    
    public static LobbyPlayerConnections Instance { get; private set; }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;

    private NetworkManager _networkManager;
    
    private void Awake() {
        Instance = this;
        
        DontDestroyOnLoad(gameObject);

        _networkManager = InstanceFinder.NetworkManager;
        
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManagerOnRemoteConnectionState;
    }
    
    private void ServerManagerOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == RemoteConnectionState.Started) {
            Debug.Log("ServerManagerOnRemoteConnectionState");
            Debug.Log(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
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
    
    public void StartServer() {
        _networkManager.ServerManager.StartConnection();
    }

    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
        
        _networkManager.ClientManager.OnClientConnectionState += ClientManagerOnClientConnectionState;
        _networkManager.ClientManager.StartConnection();
    }

    private void ClientManagerOnClientConnectionState(ClientConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Stopped) {
            OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDestroy() {
        _networkManager.ServerManager.OnRemoteConnectionState -= ServerManagerOnRemoteConnectionState;
        _networkManager.ClientManager.OnClientConnectionState -= ClientManagerOnClientConnectionState;
    }
}
