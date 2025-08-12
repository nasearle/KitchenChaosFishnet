using System;
using FishNet;
using FishNet.Managing;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Transporting;
using UnityEngine;

// This script is needed because fishnet doesn't allow you to simply mark a NetworkBehaviour as DontDestroyOnLoad, and
// instead you have to mark it as IsGlobal and then spawn it from the server. So any scripts that need access to a
// global NetworkBehaviour have to listen for its spawn event before they can use it.
public class SpawnLobbyPlayerConnections : MonoBehaviour {
    [SerializeField] private NetworkObject lobbyPlayerConnectionsPrefab;
    
    private NetworkManager _networkManager;

    private void Start() {
        _networkManager = InstanceFinder.NetworkManager;
        
        _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started && LobbyPlayerConnections.Instance == null) {
            NetworkObject lobbyPlayerConnectionsNetworkObject = _networkManager.GetPooledInstantiated(lobbyPlayerConnectionsPrefab, true);
            
            _networkManager.ServerManager.Spawn(lobbyPlayerConnectionsNetworkObject);
        }
    }
}
