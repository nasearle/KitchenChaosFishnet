using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishNet;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class KitchenGameLobby : MonoBehaviour {
    public const int MAX_PLAYER_AMOUNT = 4;

    public static KitchenGameLobby Instance { get; private set; }
    private const string PLAYER_PREFS_PLAYER_NAME = "PlayerName";

    // "External" events fired when lobby created, joined, or left
    public event EventHandler OnLobbyCreateStarted;
    public event EventHandler OnLobbyCreateFailed;
    public event EventHandler OnLobbyJoinStarted;
    public event EventHandler OnLobbyJoinSucceeded;
    public event EventHandler OnLobbyJoinFailed;
    public event EventHandler OnLobbyLeaveStarted;

    public event EventHandler OnLobbyLeaveSucceeded;

    // "Internal" events fired when data within the joined lobby changes
    public event EventHandler OnJoinedLobbyAnyChange;
    public event EventHandler OnJoinedLobbyTopLevelDataChange;
    public event EventHandler OnJoinedLobbyPlayerStatusChanged;

    // Event for local player data object that exists outside the lobby
    public event EventHandler OnPlayerDataChanged;
    public event EventHandler OnUnityGamingServicesInitialized;

    [SerializeField] private List<Color> playerColorList;

    private Lobby _joinedLobby;
    private Lobby _relayJoinCodeLobby;
    private ILobbyEvents _lobbyEvents;
    private LobbyEventCallbacks _lobbyEventCallbacks;
    private float _heartbeatTimer;
    private PlayerData _playerData;
    private NetworkManager _networkManager;
    private float _updateLobbyCooldownTimer = 0f;
    private float _updateLobbyCooldownTimerMax = 1.1f;
    private float _updatePlayersCooldownTimer = 0f;
    private float _updatePlayersCooldownTimerMax = 1.1f;


    public static class LobbyDataKeys {
        public const string ColorId = "ColorId";
        public const string PlayerName = "PlayerName";
        public const string MatchmakingStatus = "MatchmakingStatus";
        public const string ServerIp = "ServerIp";
        public const string ServerPort = "ServerPort";
        public const string RelayJoinCode = "RelayJoinCode";
    }

    public enum MatchmakingStatus {
        Waiting,
        Searching,
        Cancelling,
        MatchFound,
    }

    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _networkManager = InstanceFinder.NetworkManager;
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManagerOnRemoteConnectionState;

        _playerData = new PlayerData() {
            matchmakingStatus = MatchmakingStatus.Waiting.ToString()
        };

        _playerData.playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME, "PlayerName" + UnityEngine.Random.Range(100, 1000));
    }

    private void ServerManagerOnRemoteConnectionState(NetworkConnection connection, RemoteConnectionStateArgs connectionStateArgs) {
        if (connectionStateArgs.ConnectionState == RemoteConnectionState.Stopped) {
            // The server counts itself as a connected client for some reason, 
            // so subtract one to get the actual connected player count.
            int connectedPlayers = _networkManager.ServerManager.Clients.Count - 1;

            if (connectedPlayers == 0) {
                Debug.Log("GAME_MANAGER ServerManagerOnRemoteConnectionState ALL CLIENTS DISCONNECTED");
                ShutdownServer();
            }
        }
    }

    private void Start() {
        KitchenGameMatchmaker.Instance.OnFindMatchStarted += KitchenGameMatchmakerOnFindMatchStarted;
        KitchenGameMatchmaker.Instance.OnFindMatchFailed += KitchenGameMatchmakerOnFindMatchFailed;
        KitchenGameMatchmaker.Instance.OnCancelFindMatchStarted += KitchenGameMatchmakerOnCancelFindMatchStarted;
        KitchenGameMatchmaker.Instance.OnCancelFindMatchSucceeded += KitchenGameMatchmakerOnCancelFindMatchSucceeded;
        KitchenGameMatchmaker.Instance.OnCancelFindMatchFailed += KitchenGameMatchmakerOnCancelFindMatchFailed;

        if (UnityServices.State == ServicesInitializationState.Initialized) {
            InitializeUnityGamingServicesOnInitialized(this, EventArgs.Empty);
        } else {
            InitializeUnityGamingServices.Instance.OnInitialized += InitializeUnityGamingServicesOnInitialized;
        }
    }

    private void KitchenGameMatchmakerOnCancelFindMatchFailed(object sender, EventArgs e) {
        SetPlayerMatchmakingStatus(MatchmakingStatus.Searching);
    }

    private void KitchenGameMatchmakerOnCancelFindMatchSucceeded(object sender, EventArgs e) {
        SetPlayerMatchmakingStatus(MatchmakingStatus.Waiting);
    }

    private void KitchenGameMatchmakerOnCancelFindMatchStarted(object sender, EventArgs e) {
        SetPlayerMatchmakingStatus(MatchmakingStatus.Cancelling);
    }

    private void KitchenGameMatchmakerOnFindMatchFailed(object sender, EventArgs e) {
        SetPlayerMatchmakingStatus(MatchmakingStatus.Waiting);
    }

    private void KitchenGameMatchmakerOnFindMatchStarted(object sender, EventArgs e) {
        SetPlayerMatchmakingStatus(MatchmakingStatus.Searching);
    }

    private void InitializeUnityGamingServicesOnInitialized(object sender, EventArgs e) {
        OnUnityGamingServicesInitialized?.Invoke(this, EventArgs.Empty);
    }

    private void Update() {
#if !UNITY_SERVER
        HandleHeartbeat();

        if (_joinedLobby != null && SceneManager.GetActiveScene().name == Loader.Scene.LobbyScene.ToString()) {
            if (IsLocalPlayerLobbyHost()) {
                HandleLobbyUpdates();
            }
            
            HandlePlayerLobbyUpdates();
        }
#endif

#if UNITY_SERVER
        HandleRelayJoinCodeLobbyHeartbeat();

        if (KitchenGameDedicatedServer.serverQueryHandler != null) {
            if (_networkManager.IsServerStarted) {
                KitchenGameDedicatedServer.serverQueryHandler.CurrentPlayers = (ushort)_networkManager.ServerManager.Clients.Keys.Count;
            }
            KitchenGameDedicatedServer.serverQueryHandler.UpdateServerCheck();
        }
#endif
    }

    private void HandlePlayerLobbyUpdates() {
        _updatePlayersCooldownTimer -= Time.deltaTime;
        if (_updatePlayersCooldownTimer <= 0) {
            var updateLobbyData = new Dictionary<string, PlayerDataObject> { };

            Unity.Services.Lobbies.Models.Player lobbyPlayerData = GetLobbyPlayerDataForLocalPlayer();
            int lobbyPlayerColorId = LobbyPlayerDataConverter.GetPlayerDataValue<int>(lobbyPlayerData, LobbyDataKeys.ColorId);
            int playerDataColorId = GetPlayerColorId();

            if (playerDataColorId != lobbyPlayerColorId) {
                updateLobbyData.Add(LobbyDataKeys.ColorId, new PlayerDataObject (
                        visibility: PlayerDataObject.VisibilityOptions.Member, 
                        value: playerDataColorId.ToString()
                    )
                );
            }

            string lobbyPlayerName = LobbyPlayerDataConverter.GetPlayerDataValue(lobbyPlayerData, LobbyDataKeys.PlayerName);
            string playerDataPlayerName = GetPlayerName();

            if (playerDataPlayerName != lobbyPlayerName) {
                updateLobbyData.Add(LobbyDataKeys.PlayerName, new PlayerDataObject (
                        visibility: PlayerDataObject.VisibilityOptions.Member, 
                        value: playerDataPlayerName
                    )
                );
            }

            if (updateLobbyData.Count != 0) {
                SetLobbyPlayerData(updateLobbyData);
                _updatePlayersCooldownTimer = _updatePlayersCooldownTimerMax;
            } else {
                _updatePlayersCooldownTimer = 0f;
            }
        }
    }

    private void HandleLobbyUpdates() {
        _updateLobbyCooldownTimer -= Time.deltaTime;
        if (_updateLobbyCooldownTimer <= 0f) {
            string playerDataMatchmakingStatus = GetPlayerMatchmakingStatus();
            string lobbyMatchmakingStatus = LobbyPlayerDataConverter.GetLobbyDataValue(_joinedLobby, LobbyDataKeys.MatchmakingStatus);
            
            // If the lobby is in the MatchFound state, don't update it.
            if (playerDataMatchmakingStatus != lobbyMatchmakingStatus && lobbyMatchmakingStatus != MatchmakingStatus.MatchFound.ToString()) {
                SetLobbyMatchmakingStatus(playerDataMatchmakingStatus);
                _updateLobbyCooldownTimer = _updateLobbyCooldownTimerMax;
            } else {
                _updateLobbyCooldownTimer = 0f;
            }
        }
    }

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

    // Used on both the server and client
    private async Task SubscribeToLobbyEvents(string lobbyId) {
        _lobbyEventCallbacks = new LobbyEventCallbacks();
        _lobbyEventCallbacks.LobbyChanged += OnLobbyChanged;
        _lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobby;

        try {
            _lobbyEvents = await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobbyId, _lobbyEventCallbacks);
        }
        catch (LobbyServiceException ex)
        {
            switch (ex.Reason) {
                case LobbyExceptionReason.AlreadySubscribedToLobby: Debug.LogWarning($"Already subscribed to lobby[{lobbyId}]. We did not need to try and subscribe again. Exception Message: {ex.Message}"); break;
                case LobbyExceptionReason.SubscriptionToLobbyLostWhileBusy: Debug.LogError($"Subscription to lobby events was lost while it was busy trying to subscribe. Exception Message: {ex.Message}"); throw;
                case LobbyExceptionReason.LobbyEventServiceConnectionError: Debug.LogError($"Failed to connect to lobby events. Exception Message: {ex.Message}"); throw;
                default: throw;
            }
        }
    }

    private async Task UnsubscribeFromLobbyEvents() {
        _lobbyEventCallbacks.LobbyChanged -= OnLobbyChanged;
        _lobbyEventCallbacks.KickedFromLobby -= OnKickedFromLobby;
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

        UnsubscribeFromLobbyEvents();

        OnLobbyLeaveSucceeded?.Invoke(this, EventArgs.Empty);
    }

    private void OnLobbyChanged(ILobbyChanges changes) {
#if DEBUG_LOBBY_EVENTS
        if (changes.HostId.Value != null) {
            Debug.Log($"HostId changed: {changes.HostId.Value}");
        }
        
        if (changes.PlayerJoined.Value != null) {
            Debug.Log($"PlayerJoined: {changes.PlayerJoined.Value}");
        }
        
        if (changes.PlayerLeft.Value != null) {
            Debug.Log($"PlayerLeft: {changes.PlayerLeft.Value}");
        }

        if (changes.PlayerData.Value != null) {
            Debug.Log($"PlayerData: {changes.PlayerData.Value}");
        }
        
        if (changes.Data.Value != null) {
            Debug.Log($"Lobby Data changed: {changes.Data.Value}");
        }
#endif

#if UNITY_SERVER
        if (changes.PlayerLeft.Value != null) {
            Debug.Log("DEDICATED_SERVER Player left relay join code lobby");
            DeleteRelayJoinCodeLobby();
        }
#endif

#if !UNITY_SERVER
        if (changes.LobbyDeleted) {
            // Handle lobby being deleted
            // Calling changes.ApplyToLobby will log a warning and do nothing
        } else {
            if (_joinedLobby == null) {
                return;
            }

            changes.ApplyToLobby(_joinedLobby);

            // Used to update player avatar visibility, name, and color in the
            // lobby
            OnJoinedLobbyAnyChange?.Invoke(this, EventArgs.Empty);

            if (changes.HostId.Value != null ||
                changes.PlayerJoined.Value != null ||
                changes.PlayerLeft.Value != null) {

                // Used to show or hide the buttons above the player avatars
                // that control leaving the lobby, kicking other players, and
                // making someone else the host.
                OnJoinedLobbyPlayerStatusChanged?.Invoke(this, EventArgs.Empty);
            }

            if (changes.Data.Value != null) {
                OnJoinedLobbyTopLevelDataChange?.Invoke(this, EventArgs.Empty);
            }
        }
#endif
    }

#if UNITY_SERVER
    public async Task CreateRelayJoinCodeLobby(string lobbyName, string relayJoinCode) {
        Debug.Log("DEDICATED_SERVER Creating relay join code lobby");

        try {
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = false;
            options.Data = new Dictionary<string, DataObject> {
                { LobbyDataKeys.RelayJoinCode , new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
            };

            _relayJoinCodeLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYER_AMOUNT, options);
            
            Debug.Log("DEDICATED_SERVER Created relay join code lobby");

            await SubscribeToLobbyEvents(_relayJoinCodeLobby.Id);

            Debug.Log("DEDICATED_SERVER Subscribed to relay join code lobby events");
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async void DeleteRelayJoinCodeLobby() {
        if (_relayJoinCodeLobby == null) {
            return;
        }

        Debug.Log("DEDICATED_SERVER Deleting relay join code lobby");
        await UnsubscribeFromLobbyEvents();

        try {
            await LobbyService.Instance.DeleteLobbyAsync(_relayJoinCodeLobby.Id);

            _relayJoinCodeLobby = null;

            Debug.Log("DEDICATED_SERVER Deleted relay join code lobby");
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    // This is actually not really needed since the lobby timeout is set to 30s 
    // and the lobby is only needed for a few seconds to pass the relay join
    // code to the client.
    private void HandleRelayJoinCodeLobbyHeartbeat() {
        if (_relayJoinCodeLobby == null) {
            return;
        }

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer <= 0f) {
            float heartbeatTimerMax = 15f;
            _heartbeatTimer = heartbeatTimerMax;

            LobbyService.Instance.SendHeartbeatPingAsync(_relayJoinCodeLobby.Id);
        }
    }
#endif

    public async Task CreateLobby(string lobbyName, bool isPrivate) {
        // Only create a lobby if we're not already in one.
        if (_joinedLobby != null) {
            return;
        }


        OnLobbyCreateStarted?.Invoke(this, EventArgs.Empty);
        try {
            CreateLobbyOptions options = new CreateLobbyOptions();
            options.IsPrivate = isPrivate;
            options.Data = new Dictionary<string, DataObject> {
                {LobbyDataKeys.MatchmakingStatus, new DataObject(DataObject.VisibilityOptions.Member, MatchmakingStatus.Waiting.ToString())}
            };
            options.Player = new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject> {
                    {LobbyDataKeys.ColorId, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerColorId().ToString())},
                    {LobbyDataKeys.PlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerName())}
            });
            _joinedLobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, MAX_PLAYER_AMOUNT, options);

            OnLobbyJoinSucceeded?.Invoke(this, EventArgs.Empty);

            await SubscribeToLobbyEvents(_joinedLobby.Id);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            OnLobbyCreateFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task JoinWithCode(string lobbyCode) {
        if (_joinedLobby != null) {
            if (_joinedLobby.LobbyCode == lobbyCode.ToUpper()) {
                // Already in the lobby we are trying to join!
                return;
            }
            await LeaveLobby();
        }

        OnLobbyJoinStarted?.Invoke(this, EventArgs.Empty);
        try {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions();
            options.Player = new Unity.Services.Lobbies.Models.Player(
                id: AuthenticationService.Instance.PlayerId,
                data: new Dictionary<string, PlayerDataObject> {
                    {LobbyDataKeys.ColorId, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerColorId().ToString())},
                    {LobbyDataKeys.PlayerName, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member, GetPlayerName())}
            });
            _joinedLobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            await SubscribeToLobbyEvents(_joinedLobby.Id);

            if (MoreThanOnePlayerHasColor(GetPlayerColorId())) {
                SetPlayerColor(GetFirstUnusedColorId());
            }            

            OnLobbyJoinSucceeded?.Invoke(this, EventArgs.Empty);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            // TODO: could pass e.Message to the event to tell the player the
            // reason for the failure. E.g. "lobby is full"
            OnLobbyJoinFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    public async Task<Lobby> JoinWithId(string lobbyId) {
        try {
            return await LobbyService.Instance.JoinLobbyByIdAsync(lobbyId);
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    public async Task LeaveLobbyWithId(string lobbyId) {
        try {
            await LobbyService.Instance.RemovePlayerAsync(lobbyId, AuthenticationService.Instance.PlayerId);
            Debug.Log("Left the relay join code lobby");
        } catch (LobbyServiceException e) {
            Debug.Log(e);
        }
    }

    public async Task<QueryResponse> QueryForRelayJoinCodeLobby(string lobbyName) {
        try {
            QueryLobbiesOptions queryLobbiesOptions = new QueryLobbiesOptions {
                Count = 1,
                Filters = new List<QueryFilter> {
                    new QueryFilter(QueryFilter.FieldOptions.Name, lobbyName, QueryFilter.OpOptions.EQ)
                }
            };
            QueryResponse queryResponse = await LobbyService.Instance.QueryLobbiesAsync(queryLobbiesOptions);
            return queryResponse;
        } catch (LobbyServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    public async void DeleteLobby() {
        if (_joinedLobby != null && IsLocalPlayerLobbyHost()) {
            await UnsubscribeFromLobbyEvents();

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
            OnLobbyLeaveStarted?.Invoke(this, EventArgs.Empty);
            try {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                _joinedLobby = null;

                OnLobbyLeaveSucceeded?.Invoke(this, EventArgs.Empty);
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

    public PlayerData GetPlayerData() {
        return _playerData;
    }

    public string GetPlayerName() {
        return _playerData.playerName;
    }

    public int GetPlayerColorId() {
        return _playerData.colorId;
    }

    public string GetPlayerMatchmakingStatus() {
        return _playerData.matchmakingStatus;
    }

    public void SetPlayerName(string playerName) {
        _playerData.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME, playerName);

        OnPlayerDataChanged?.Invoke(this, EventArgs.Empty);
    }    

    public void SetPlayerColor(int colorId) {
        if (!IsColorAvailable(colorId)) {
            return;
        }

        _playerData.colorId = colorId;

        OnPlayerDataChanged?.Invoke(this, EventArgs.Empty);
    }    

    public void SetPlayerMatchmakingStatus(MatchmakingStatus status) {
        _playerData.matchmakingStatus = status.ToString();

        OnPlayerDataChanged?.Invoke(this, EventArgs.Empty);
    }

    private async Task SetLobbyPlayerData(Dictionary<string, PlayerDataObject> data) {
        if (_joinedLobby == null) {
            return;
        }

        try {
            UpdatePlayerOptions options = new UpdatePlayerOptions {
                Data = data
            };
            
            await LobbyService.Instance.UpdatePlayerAsync(_joinedLobby.Id, 
                AuthenticationService.Instance.PlayerId, 
                options);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update player color: {e}");
        }
    }

    public async Task SetLobbyMatchmakingStatus(string status) {
        if (_joinedLobby == null || !IsLocalPlayerLobbyHost()) {
            return;
        }

        try {
            UpdateLobbyOptions options = new UpdateLobbyOptions {
                Data = new Dictionary<string, DataObject> {
                    {LobbyDataKeys.MatchmakingStatus, new DataObject(DataObject.VisibilityOptions.Member, status)}
                }
            };

            await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, options);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update lobby with match data: {e}");
        }
    }

    public async void SetLobbyMatchFoundDetails(string relayJoinCode) {
        if (_joinedLobby == null || !IsLocalPlayerLobbyHost()) {
            return;
        }
        
        try {
            // Update lobby with connection information
            var lobbyData = new Dictionary<string, DataObject> {
                {LobbyDataKeys.MatchmakingStatus, new DataObject(DataObject.VisibilityOptions.Member, MatchmakingStatus.MatchFound.ToString())},
                {LobbyDataKeys.RelayJoinCode, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode)},
                // {LobbyDataKeys.ServerIp, new DataObject(DataObject.VisibilityOptions.Member, assignment.Ip)},
                // {LobbyDataKeys.ServerPort, new DataObject(DataObject.VisibilityOptions.Member, assignment.Port.ToString())},
                // {"matchId", new DataObject(DataObject.VisibilityOptions.Member, assignment.MatchId)},
                // {"gameServerUrl", new DataObject(DataObject.VisibilityOptions.Member, $"{assignment.Ip}:{assignment.Port}")},
            };
            
            UpdateLobbyOptions options = new UpdateLobbyOptions {
                Data = lobbyData
            };
            
            await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, options);

            SetPlayerMatchmakingStatus(MatchmakingStatus.MatchFound);

            OnPlayerDataChanged?.Invoke(this, EventArgs.Empty);
        } catch (LobbyServiceException e) {
            Debug.LogError($"Failed to update lobby with match data: {e}");
        }
    }

    public bool IsPlayerIndexConnected(int playerIndex) {
        return _joinedLobby.Players.Count > playerIndex;
    }

    public Unity.Services.Lobbies.Models.Player GetLobbyPlayerDataFromPlayerIndex(int playerIndex) {
        // TODO: test that this stays in the same order. The lobby might not
        // keep the same order when players join and leave.
        return _joinedLobby.Players[playerIndex];
    }

    public Unity.Services.Lobbies.Models.Player GetLobbyPlayerDataForLocalPlayer() {
        if (_joinedLobby != null) {
            foreach (Unity.Services.Lobbies.Models.Player playerData in _joinedLobby.Players) {
                if (playerData.Id == AuthenticationService.Instance.PlayerId) {
                    return playerData;
                }
            }
        }
        return default;
    }

    public Color GetPlayerColorByColorId(int colorId) {
        return playerColorList[colorId];
    }

    private bool MoreThanOnePlayerHasColor(int colorId) {
        if (_joinedLobby == null) {
            return false;
        }

        int count = 0;
        foreach (Unity.Services.Lobbies.Models.Player playerData in _joinedLobby.Players) {
            if (LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, LobbyDataKeys.ColorId) == colorId) {
                count++;
                if (count > 1) {
                    return true;
                }
            }
        }

        return false;
    }

    private bool IsColorAvailable(int colorId) {
        if (_joinedLobby == null) {
            return true;
        }

        foreach (Unity.Services.Lobbies.Models.Player playerData in _joinedLobby.Players) {
            if (LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, LobbyDataKeys.ColorId) == colorId) {
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

    public void ShutdownServer() {
#if UNITY_SERVER
        Debug.Log("KITCHEN_GAME_LOBBY SHUTTING DOWN SERVER");
        KitchenGameDedicatedServer.serverQueryHandler = null;
        _networkManager.ServerManager.StopConnection(false);
        Application.Quit();
#endif
    }

    private void OnDestroy() {
        InitializeUnityGamingServices.Instance.OnInitialized += InitializeUnityGamingServicesOnInitialized;
    }
}
