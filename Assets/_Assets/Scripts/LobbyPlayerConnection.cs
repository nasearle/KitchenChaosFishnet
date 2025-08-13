using System;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;

public class LobbyPlayerConnection : MonoBehaviour {
    public static LobbyPlayerConnection Instance { get; private set; }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;

    private NetworkManager _networkManager;

    private void Awake() {
        Instance = this;
        
        _networkManager = InstanceFinder.NetworkManager;
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
        _networkManager.ClientManager.OnClientConnectionState -= ClientManagerOnClientConnectionState;
    }
}
