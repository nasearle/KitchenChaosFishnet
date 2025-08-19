using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Multiplayer.Playmode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEditor.SearchService;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour {

    public static KitchenGameLobby Instance { get; private set; }
    private const string PLAYER_PREFS_PLAYER_NAME = "PlayerName";

    public event EventHandler OnCreateLobbyStarted;
    public event EventHandler OnCreateLobbyFailed;
    public event EventHandler OnJoinStarted;
    public event EventHandler OnQuickJoinFailed;
    public event EventHandler OnJoinFailed;
    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs {
        public List<Lobby> lobbyList;
    }

    private NetworkManager _networkManager;
    private Lobby joinedLobby;
    private float _heartbeatTimer;
    private float _listLobbiesTimer;
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

    public void SetPlayerName(string playerName) {
        _playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME, playerName);
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            Loader.LoadNetwork(Loader.Scene.ManagersLoadingScene);
        }
    }

    private async void InitializeUnityAuthentication() {
        Debug.Log("InitializeUnityAuthentication");
        if (UnityServices.State != ServicesInitializationState.Initialized) {
            Debug.Log("InitializeUnityAuthentication initializing");
            // TODO: remove this for production
            bool dedicatedServer = CurrentPlayer.ReadOnlyTags().Length > 0 && CurrentPlayer.ReadOnlyTags()[0] == "server";

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
                _networkManager.ServerManager.StartConnection();
            }
        }
    }

    void Update() {
        HandleHeartbeat();
        HandlePeriodicListLobbies();
    }

    private void HandlePeriodicListLobbies() {
        // TODO: remember to change the scene comparison if this script is moved.
        if (joinedLobby == null &&
            AuthenticationService.Instance.IsSignedIn &&
            SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString()) {
            _listLobbiesTimer -= Time.deltaTime;
            if (_listLobbiesTimer <= 0f) {
                float listLobbiesTimerMax = 3f;
                _listLobbiesTimer = listLobbiesTimerMax;
                ListLobbies();
            }
        }
    }

    private void HandleHeartbeat() {
        if (IsLobbyHost()) {
            _heartbeatTimer -= Time.deltaTime;
            if (_heartbeatTimer <= 0f) {
                float heartbeatTimerMax = 15f;
                _heartbeatTimer = heartbeatTimerMax;

                LobbyService.Instance.SendHeartbeatPingAsync(joinedLobby.Id);
            }
        }
    }

    public bool IsLobbyHost() {
        return joinedLobby != null && joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    public async void ListLobbies() {
        try {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT)
                }
            };

            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs {
                lobbyList = queryResponse.Results
            });
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void CreateLobby(string lobbyName, bool isPrivate) {
        OnCreateLobbyStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, NetworkConnections.MAX_PLAYER_AMOUNT, new CreateLobbyOptions {
                IsPrivate = isPrivate,
            });

            LobbyPlayerConnection.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnCreateLobbyFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void QuickJoin() {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.QuickJoinLobbyAsync();

            LobbyPlayerConnection.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnQuickJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void JoinWithId(string lobbyId) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);

            LobbyPlayerConnection.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void JoinWithCode(string lobbyCode) {
        OnJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode);

            LobbyPlayerConnection.Instance.StartClient();
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async void DeleteLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.DeleteLobbyAsync(joinedLobby.Id);

                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void LeaveLobby() {
        if (joinedLobby != null) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                joinedLobby = null;
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId) {
        if (IsLobbyHost()) {
            try {
                await LobbyService.Instance.RemovePlayerAsync(joinedLobby.Id, playerId);
            } catch (LobbyServiceException e) {
                Debug.Log(e);
            }
        }
    }

    public Lobby GetLobby() {
        return joinedLobby;
    }
}
