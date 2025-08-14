using System;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Server;
using UnityEngine;
using FishNet.Object;
using FishNet.Transporting;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;

public class NetworkConnections : NetworkBehaviour {
    private const int MAX_PLAYER_AMOUNT = 4;
    
    public static NetworkConnections Instance { get; private set; }

    public event EventHandler OnPlayerDataSyncListChanged;

    [SerializeField] private List<Color> playerColorList;

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
                clientId = conn.ClientId,
                colorId = GetFirstUnusedColorId(),
            });
        }

        if (stateArgs.ConnectionState == RemoteConnectionState.Stopped) {
            for (int i = 0; i < _playerDataSyncList.Count; i++) {
                PlayerData playerData = _playerDataSyncList[i];
                if (playerData.clientId == conn.ClientId) {
                    // Disconnected!
                    _playerDataSyncList.RemoveAt(i);
                }

            }
        }
    }

    public bool IsPlayerIndexConnected(int playerIndex) {
        return _playerDataSyncList.Count > playerIndex;
    }

    public int GetPlayerDataIndexFromClientId(int clientId) {
        for (int i = 0; i < _playerDataSyncList.Count ; i++) {
            if (_playerDataSyncList[i].clientId == clientId) {
                return i;
            }
        }
        return -1;
    }

    public PlayerData GetPlayerDataFromClientId(int clientId) {
        foreach (PlayerData playerData in _playerDataSyncList) {
            if (playerData.clientId == clientId) {
                return playerData;
            }
        }
        return default;
    }

    public PlayerData GetPlayerData() {
        return GetPlayerDataFromClientId(_networkManager.ClientManager.Connection.ClientId);
    }

    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex) {
        return _playerDataSyncList[playerIndex];
    }

    public Color GetPlayerColor(int colorId) {
        return playerColorList[colorId];
    }

    public void ChangePlayerColor(int colorId) {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, NetworkConnection conn = null) {
        if (!IsColorAvailable(colorId)) {
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(conn.ClientId);

        PlayerData playerData = _playerDataSyncList[playerDataIndex];

        playerData.colorId = colorId;

        _playerDataSyncList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId) {
        foreach (PlayerData playerData in _playerDataSyncList) {
            if (playerData.colorId == colorId) {
                return false; // Color is already taken
            }
        }        
        return true; // Color is available
    }

    private int GetFirstUnusedColorId() {
        for (int i = 0; i < playerColorList.Count; i++) {
            if (IsColorAvailable(i)) {
                return i;
            }
        }
        return -1;
    }

    private void OnDestroy() {
        _networkManager.ServerManager.OnRemoteConnectionState -= ServerManagerOnRemoteConnectionState;
    }
}
