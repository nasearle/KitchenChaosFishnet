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
public class SpawnNetworkConnections : MonoBehaviour {
#if UNITY_SERVER
    [SerializeField] private NetworkObject networkConnectionsPrefab;
    
    private NetworkManager _networkManager;

    private void SpawnNetworkConnectionPrefab() {
        Debug.Log("DEDICATED_SERVER SpawnNetworkConnectionPrefab");
        if (NetworkConnections.Instance != null) {
            return;
        }
        
        NetworkObject networkConnectionsNetworkObject = _networkManager.GetPooledInstantiated(networkConnectionsPrefab, true);
        
        Debug.Log("DEDICATED_SERVER SpawnNetworkConnectionPrefab spawning...");
        _networkManager.ServerManager.Spawn(networkConnectionsNetworkObject);
    }

    private void Start() {
        _networkManager = InstanceFinder.NetworkManager;

        if (_networkManager.IsServerStarted) {
            Debug.Log("DEDICATED_SERVER SpawnNetworkConnections server already started");
            SpawnNetworkConnectionPrefab();
        } else {        
            _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;
        }
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        Debug.Log("DEDICATED_SERVER ServerManagerOnServerConnectionState");
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            SpawnNetworkConnectionPrefab();
        }
    }

    private void OnDestroy() {
        _networkManager.ServerManager.OnServerConnectionState -= ServerManagerOnServerConnectionState;
    }
#endif
}
