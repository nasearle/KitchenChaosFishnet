using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Object.Synchronizing;

public class NetworkConnections : NetworkBehaviour {
    private const int MAX_PLAYER_AMOUNT = 4;
    
    public static NetworkConnections Instance { get; private set; }

    public event EventHandler OnPlayerDataSyncListChanged;

    private NetworkManager _networkManager;
    private readonly SyncList<PlayerData> _playerDataSyncList = new SyncList<PlayerData>();
    
    private void Awake() {
        Instance = this;

        _networkManager = InstanceFinder.NetworkManager;
        
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManagerOnRemoteConnectionState;
        _playerDataSyncList.OnChange += PlayerDataSyncListOnChange;
    }

    public override void OnStartNetwork() {
        if (!IsServerStarted) {
            ClientLoadedManagerScripts();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ClientLoadedManagerScripts(NetworkConnection conn = null) {
        Loader.LoadSingleClientNetwork(Loader.Scene.CharacterSelectScene, conn);
    }

    private void PlayerDataSyncListOnChange(SyncListOperation op, int index, PlayerData oldItem, PlayerData newItem, bool asServer) {
        OnPlayerDataSyncListChanged?.Invoke(this, EventArgs.Empty);
    }
    
    private void ServerManagerOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == RemoteConnectionState.Started) {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != Loader.Scene.ManagersLoadingScene.ToString()) {
                Debug.Log("Not connecting because the game has already started");
                conn.Kick(KickReason.Unset);
                return;
            }

            if (_networkManager.ServerManager.Clients.Keys.Count > MAX_PLAYER_AMOUNT) {
                Debug.Log("Not connecting because the game is full");
                conn.Kick(KickReason.Unset);
                return;
            }

            _playerDataSyncList.Add(new PlayerData {
                clientId = conn.ClientId
            });
        }
    }

    public bool IsPlayerIndexConnected(int playerIndex) {
        return _playerDataSyncList.Count > playerIndex;
    }

    private void OnDestroy() {
        _networkManager.ServerManager.OnRemoteConnectionState -= ServerManagerOnRemoteConnectionState;
    }
}
