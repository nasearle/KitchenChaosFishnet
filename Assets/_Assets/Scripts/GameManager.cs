using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FishNet.Component.Spawning;
using FishNet.Connection;
using FishNet.Managing;
using FishNet.Managing.Scened;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using FishNet.Transporting;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.Serialization;

public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }
    
    public event EventHandler OnStateChanged;
    public event EventHandler OnLocalGamePaused;
    public event EventHandler OnLocalGameResumed;
    public event EventHandler OnMultiplayerGamePaused;
    public event EventHandler OnMultiplayerGameResumed;
    public event EventHandler OnLocalPlayerReadyChanged;
    
    private enum State {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }
    
    [SerializeField] private NetworkObject playerPrefab;
    [SerializeField] private Transform[] playerSpawnPoints;

    private readonly SyncVar<State> _state = new SyncVar<State>(State.WaitingToStart);
    private bool _isLocalPlayerReady;
    private readonly SyncVar<float> _countdownToStartTimer = new SyncVar<float>(3f);
    private readonly SyncVar<float> _gamePlayingTimer =  new SyncVar<float>(0f);
    private float _gamePlayingTimerMax = 90f;
    private bool _isLocalGamePaused = false;
    private readonly SyncVar<bool> _isGamePaused = new SyncVar<bool>(false);
    private Dictionary<int, bool> _playerReadyDictionary;
    private Dictionary<int, bool> _playerPausedDictionary;
    private bool _autoTestGamePauseState;
    private int _nextSpawn;

    private void Awake() {
        Instance = this;
        
        _playerReadyDictionary = new Dictionary<int, bool>();
        _playerPausedDictionary = new Dictionary<int, bool>();
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInputOnPauseAction;
        GameInput.Instance.OnInteractAction += GameInputOnInteractAction;
#if UNITY_SERVER
        Camera.main.enabled = false;
#endif
    }
    
    public override void OnStartNetwork() {
        _state.OnChange += StateOnChange;
        _isGamePaused.OnChange += IsGamePausedOnChange;

        if (IsServerStarted) {
            ServerManager.OnRemoteConnectionState += ServerManagerOnRemoteConnectionState;
            SceneManager.OnClientPresenceChangeEnd += SceneManagerOnClientPresenceChangeEnd;
        }
    }

    private void SceneManagerOnClientPresenceChangeEnd(ClientPresenceChangeEventArgs eventArgs) {
        if (eventArgs.Added) {
            if (playerPrefab == null) {
                NetworkManagerExtensions.LogWarning($"Player prefab is empty and cannot be spawned");
                return;
            }
            
            SetSpawn(playerPrefab.transform, out Vector3 position, out Quaternion rotation);
            NetworkObject playerNetworkObject = NetworkManager.GetPooledInstantiated(playerPrefab, position, rotation, true);
            Spawn(playerNetworkObject, eventArgs.Connection);
        }
    }

    private void ServerManagerOnRemoteConnectionState(NetworkConnection conn, RemoteConnectionStateArgs connectionStateArgs) {
        if (connectionStateArgs.ConnectionState == RemoteConnectionState.Stopped) {
            _autoTestGamePauseState = true;
        }
    }

    private void IsGamePausedOnChange(bool prev, bool next, bool asServer) {
        if (_isGamePaused.Value) {
            Time.timeScale = 0f;
            
            OnMultiplayerGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            Time.timeScale = 1f;
            
            OnMultiplayerGameResumed?.Invoke(this, EventArgs.Empty);
        }
    }

    private void StateOnChange(State prev, State next, bool asServer) {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GameInputOnInteractAction(object sender, EventArgs e) {
        if (_state.Value == State.WaitingToStart) {
            _isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            
            // Note: This will never be called from the server because it's a client-server game so the server will
            // never be a player. So we don't need do the check and run locally dance.
            if (IsClientStarted) {
                SetPlayerReadyServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(NetworkConnection conn = null) {
        _playerReadyDictionary[conn.ClientId] = true;

        bool allClientsReady = true;
        foreach (int clientId in ServerManager.Clients.Keys) {
            if (!_playerReadyDictionary.ContainsKey(clientId) || !_playerReadyDictionary[clientId]) {
                //This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady) {
            _state.Value = State.CountdownToStart;

#if UNITY_SERVER
            MultiplayService.Instance.UnreadyServerAsync();
#endif
        }
    }

    private void GameInputOnPauseAction(object sender, EventArgs e) {
        TogglePauseGame();
    }

    private void Update() {
        if (!IsServerStarted) {
            return;
        }
        
        switch (_state.Value) {
            case State.WaitingToStart:
                break;
            case State.CountdownToStart:
                _countdownToStartTimer.Value -= Time.deltaTime;
                if (_countdownToStartTimer.Value < 0f) {
                    _gamePlayingTimer.Value = _gamePlayingTimerMax;
                    _state.Value = State.GamePlaying;
                }
                break;
            case State.GamePlaying:
                _gamePlayingTimer.Value -= Time.deltaTime;
                if (_gamePlayingTimer.Value < 0f) {
                    _state.Value = State.GameOver;
                }
                break;
            case State.GameOver:
                break;
        }
    }

    private void LateUpdate() {
        if (_autoTestGamePauseState) {
            _autoTestGamePauseState = false;
            TestGamePausedState();
        }
    }

    public bool IsGamePlaying() {
        return _state.Value == State.GamePlaying;
    }
    
    public bool IsCountdownToStartActive() {
        return _state.Value == State.CountdownToStart;
    }

    public float GetCountdownToStartTimer() {
        return _countdownToStartTimer.Value;
    }
    
    public bool IsGameOver() {
        return _state.Value == State.GameOver;
    }

    public bool IsLocalPlayerReady() {
        return _isLocalPlayerReady;
    }

    public float GetGamePlayingTimerNormalized() {
        return 1 - (_gamePlayingTimer.Value / _gamePlayingTimerMax);
    }

    public void TogglePauseGame() {
        _isLocalGamePaused = !_isLocalGamePaused;
        if (_isLocalGamePaused) {
            PauseGameServerRpc();
            
            OnLocalGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            UnpauseGameServerRpc();
            
            OnLocalGameResumed?.Invoke(this, EventArgs.Empty);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PauseGameServerRpc(NetworkConnection conn = null) {
        _playerPausedDictionary[conn.ClientId] = true;

        TestGamePausedState();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void UnpauseGameServerRpc(NetworkConnection conn = null) {
        _playerPausedDictionary[conn.ClientId] = false;

        TestGamePausedState();
    }

    private void TestGamePausedState() {
        foreach (int cliendId in ServerManager.Clients.Keys) {
            if (_playerPausedDictionary.ContainsKey(cliendId) && _playerPausedDictionary[cliendId]) {
                // This player is paused
                _isGamePaused.Value = true;
                return;
            }
        }
        
        // All players are unpaused
        _isGamePaused.Value = false;
    }
    
    private void SetSpawn(Transform prefab, out Vector3 pos, out Quaternion rot) {
        // No spawns specified.
        if (playerSpawnPoints.Length == 0) {
            SetSpawnUsingPrefab(prefab, out pos, out rot);
            return;
        }

        Transform spawnPoint = playerSpawnPoints[_nextSpawn];
        if (spawnPoint == null) {
            SetSpawnUsingPrefab(prefab, out pos, out rot);
        } else {
            pos = spawnPoint.position;
            rot = spawnPoint.rotation;
        }

        // Increase next spawn and reset if needed.
        _nextSpawn++;
        if (_nextSpawn >= playerSpawnPoints.Length) {
            _nextSpawn = 0;
        }
    }
    
    private void SetSpawnUsingPrefab(Transform prefab, out Vector3 pos, out Quaternion rot) {
        pos = prefab.position;
        rot = prefab.rotation;
    }
}
