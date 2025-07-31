using System;
using System.Collections.Generic;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class GameManager : NetworkBehaviour {
    public static GameManager Instance { get; private set; }
    
    public event EventHandler OnStateChanged;
    public event EventHandler OnGamePaused;
    public event EventHandler OnGameResumed;
    public event EventHandler OnLocalPlayerReadyChanged;
    
    private enum State {
        WaitingToStart,
        CountdownToStart,
        GamePlaying,
        GameOver,
    }

    private readonly SyncVar<State> _state = new SyncVar<State>(State.WaitingToStart);
    private bool _isLocalPlayerReady;
    private readonly SyncVar<float> _countdownToStartTimer = new SyncVar<float>(3f);
    private readonly SyncVar<float> _gamePlayingTimer =  new SyncVar<float>(0f);
    private float _gamePlayingTimerMax = 90f;
    private bool _isGamePaused = false;
    private Dictionary<int, bool> _playerReadyDictionary;

    private void Awake() {
        Instance = this;
        
        _playerReadyDictionary = new Dictionary<int, bool>();
    }

    private void Start() {
        GameInput.Instance.OnPauseAction += GameInputOnPauseAction;
        GameInput.Instance.OnInteractAction += GameInputOnInteractAction;
    }
    
    public override void OnStartNetwork() {
        _state.OnChange += StateOnChange;
    }

    private void StateOnChange(State prev, State next, bool asServer) {
        OnStateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void GameInputOnInteractAction(object sender, EventArgs e) {
        if (_state.Value == State.WaitingToStart) {
            _isLocalPlayerReady = true;
            OnLocalPlayerReadyChanged?.Invoke(this, EventArgs.Empty);
            
            SetPlayerReadyServerRpc();
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
        _isGamePaused = !_isGamePaused;
        if (_isGamePaused) {
            Time.timeScale = 0f;
            OnGamePaused?.Invoke(this, EventArgs.Empty);
        } else {
            Time.timeScale = 1f;
            OnGameResumed?.Invoke(this, EventArgs.Empty);
        }
    }
}
