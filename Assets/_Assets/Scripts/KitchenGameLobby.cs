using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Multiplayer.Playmode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor.Rendering;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour {
    private const int MAX_PLAYER_AMOUNT = 4;

    public static KitchenGameLobby Instance { get; private set; }
    private const string PLAYER_PREFS_PLAYER_NAME = "PlayerName";

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnLobbyJoined;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnJoinFailed;
    public event EventHandler OnJoinedLobbyDataUpdated;
    public event EventHandler OnLobbyPlayerStatusChanged;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }
    

    [SerializeField] private List<Color> playerColorList;

    private NetworkManager _networkManager;
    private Lobby _joinedLobby;
    private ILobbyEvents _lobbyEvents;
    private float _heartbeatTimer;
    private float _listLobbiesTimer;
    private float _updateJoinedLobbyDataTimer;
    private string _playerName;

    void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _networkManager = InstanceFinder.NetworkManager;

        _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;

        _playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        InitializeUnityAuthentication();
    }

    public string GetPlayerName() {
        return _playerName;
    }

    public async Task SetPlayerName(string playerName) {
        _playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME, playerName);

        await SetLobbyPlayerName(playerName);
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            Loader.LoadNetwork(Loader.Scene.ManagersLoadingScene);
        }
    }

    private async void InitializeUnityAuthentication() {
        Debug.Log("InitializeUnityAuthentication");
        // TODO: remove this for production
        bool dedicatedServer = CurrentPlayer.ReadOnlyTags().Length > 0 && CurrentPlayer.ReadOnlyTags()[0] == "server";

        if (UnityServices.State != ServicesInitializationState.Initialized) {
            Debug.Log("InitializeUnityAuthentication initializing");            

            InitializationOptions initializationOptions = new InitializationOptions();

            if (!dedicatedServer) {
                initializationOptions.SetProfile(UnityEngine.Random.Range(0, 1000).ToString());
            }

            await UnityServices.InitializeAsync(initializationOptions);

            if (!dedicatedServer) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();              
            }

            if (dedicatedServer) {
                Debug.Log("InitializeUnityAuthentication dedicated server");
                // Triggers the server connection state listener above and moves
                // to the ManagersLoadingScene
                _networkManager.ServerManager.StartConnection();
            }
        }

        if (!dedicatedServer) {
            if (_joinedLobby == null) {
                CreateLobby("LobbyName", true);
            }
        }
    }

    void Update() {
        HandleHeartbeat();
        // HandlePeriodicListLobbies();
        // HandlePeriodicUpdateJoinedLobbyData();
    }

    // private async void HandlePeriodicUpdateJoinedLobbyData() {
    //     if (joinedLobby != null &&
    //         AuthenticationService.Instance.IsSignedIn &&
    //         SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString()) {
            
    //         _updateJoinedLobbyDataTimer -= Time.deltaTime;
    //         if (_updateJoinedLobbyDataTimer <= 0f) {
    //             float updateJoinedLobbyDataTimerMax = 3f;
    //             _updateJoinedLobbyDataTimer = updateJoinedLobbyDataTimerMax;

    //             try {
    //                 joinedLobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);

    //                 OnJoinedLobbyDataUpdated?.Invoke(this, EventArgs.Empty);
    //             } catch (LobbyServiceException e) {
    //                 Debug.Log(e);
    //             }
    //         }
    //     }
    // }

    private void HandleHeartbeat() {
        if (IsLocalPlayerLobbyHost()) {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f) {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }
    public bool IsLocalPlayerLobbyHost() {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public bool IsPlayerLobbyHost(Unity.Services.Lobbies.Models.Player player) {
        return _joinedLobby != null && _joinedLobby.HostId == player.Id;
    }

    public async Task SetLobbyHost(string newHostPlayerId) {
        if (_joinedLobby == null) {
            return;
        }

        if (!IsLocalPlayerLobbyHost()) {
            return;
        }

        try {
            UpdateLobbyOptions options = new UpdateLobbyOptions {
                HostId = newHostPlayerId
            };
            
            _joinedLobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, options);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update lobby host: {e}");
        }
    }

    public bool LobbyPlayerIsLocalPlayer(Unity.Services.Lobbies.Models.Player player) {
        return player.Id == AuthenticationService.Instance.PlayerId;
    }

    private async Task SubscribeToLobbyEvents() {
        var callbacks = new LobbyEventCallbacks();
        callbacks.LobbyChanged += OnLobbyChanged;
        callbacks.KickedFromLobby += OnKickedFromLobby;

        try {
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(_joinedLobby.Id, callbacks);
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason) {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{_joinedLobby.Id}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    private async Task UnsubscribeFromLobbyEvents() {
        if (_lobbyEvents != null) {
            try {
                await _lobbyEvents.UnsubscribeAsync();
                _lobbyEvents = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    private void OnKickedFromLobby() {
        _lobbyEvents = null;
        _joinedLobby = null;
        // Automatically create a new lobby
        CreateLobby("LobbyName", true);
    }

    private void OnLobbyChanged(ILobbyChanges changes) {
        if (changes.LobbyDeleted) {
            // Handle lobby being deleted
            // Calling changes.ApplyToLobby will log a warning and do nothing
        } else {
            changes.ApplyToLobby(_joinedLobby);

            OnJoinedLobbyDataUpdated?.Invoke(this, EventArgs.Empty);

            if (changes.HostId.Value != null ||
                changes.PlayerJoined.Value != null ||
                changes.PlayerLeft.Value != null) {
                OnLobbyPlayerStatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public async Task CreateLobby(string lobbyName, bool isPrivate) {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = isPrivate;
            options.Player = new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject> {
                    {"colorId", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetFirstUnusedColorId().ToString())},
                    {"playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerName())}
            });
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYER_AMOUNT, options);

            OnLobbyJoined?.Invoke(this, EventArgs.Empty);

            await SubscribeToLobbyEvents();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task JoinWithCode(string lobbyCode) {
        if (_joinedLobby != null) {
            await LeaveLobby();
        }

        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
            options.Player = new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject> {
                    // {"colorId", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetFirstUnusedColorId().ToString())},
                    {"playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerName())}
            });
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            await SetLobbyPlayerColor(GetFirstUnusedColorId());

            OnLobbyJoined?.Invoke(this, EventArgs.Empty);

            await SubscribeToLobbyEvents();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void DeleteLobby() {
        if (_joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(_joinedLobby.Id);

                _joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    // Note the lobby is automatically deleted when the last player leaves the lobby
    public async Task LeaveLobby() {
        await UnsubscribeFromLobbyEvents();
        if (_joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                _joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLocalPlayerLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public Lobby GetLobby() {
        return _joinedLobby;
    }

    // TODO: limit the calls to this function otherwise will run into lobby
    // rate limits
    public async Task SetLobbyPlayerName(string playerName) {   
        if (_joinedLobby == null) {
            return;
        }

        try {
            UpdatePlayerOptions options = new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                    {"playerName", new PlayerDataObject (
                        visibility: PlayerDataObject.VisibilityOptions.Member, 
                        value: playerName
                    )}
                }
            };
            
            await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, 
                AuthenticationService.Instance.PlayerId, 
                options);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update player name: {e}");
        }
    }

    // TODO: limit the calls to this function otherwise will run into lobby
    // rate limits
    public async Task SetLobbyPlayerColor(int colorId) {      
        if (_joinedLobby == null) {
            return;
        }

        if (!IsColorAvailable(colorId)) {
            return;
        }

        try {
            UpdatePlayerOptions options = new UpdatePlayerOptions {
                Data = new Dictionary<string, PlayerDataObject> {
                    {"colorId", new PlayerDataObject (
                        visibility: PlayerDataObject.VisibilityOptions.Member, 
                        value: colorId.ToString()
                    )}
                }
            };
            
            await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, 
                AuthenticationService.Instance.PlayerId, 
                options);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update player color: {e}");
        }
    }

    public bool IsPlayerIndexConnected(int playerIndex) {
        return _joinedLobby.Players.Count > playerIndex;
    }

    public Unity.Services.Lobbies.Models.Player GetPlayerDataFromPlayerIndex(int playerIndex) {
        // TODO: test that this stays in the same order. The lobby might not
        // keep the same order when players join and leave.
        return _joinedLobby.Players[playerIndex];
    }

    public Unity.Services.Lobbies.Models.Player GetPlayerDataFromPlayerId() {
        if (_joinedLobby != null) {
            foreach (Unity.Services.Lobbies.Models.Player playerData in _joinedLobby.Players) {
                if (playerData.Id == AuthenticationService.Instance.PlayerId) {
                    return playerData;
                }
            }
        }
        return default;
    }

    public Unity.Services.Lobbies.Models.Player GetPlayerData() {
        return GetPlayerDataFromPlayerId();
    }

    public Color GetPlayerColor(int colorId) {
        return playerColorList[colorId];
    }

    private bool IsColorAvailable(int colorId) {
        if (_joinedLobby == null) {
            return true;
        }

        foreach (Unity.Services.Lobbies.Models.Player playerData in _joinedLobby.Players) {
            if (LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, "colorId") == colorId) {
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
}
