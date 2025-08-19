using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour {
    public static CharacterSelectReady Instance { get; private set; }

    public event EventHandler OnReadyChanged;
    
    private readonly SyncDictionary<int, bool> _playerReadyDictionary = new();
    
    private ServerManager _serverManager;

    private void Awake() {
        Instance = this;

        _serverManager = InstanceFinder.ServerManager;

        _playerReadyDictionary.OnChange += PlayerReadyDictionaryOnChange;
    }

    private void PlayerReadyDictionaryOnChange(SyncDictionaryOperation op, int key, bool value, bool asServer) {
        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }

    public void SetPlayerReady() {
        SetPlayerReadyServerRpc();
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(NetworkConnection conn = null) {
        _playerReadyDictionary[conn.ClientId] = true;

        bool allClientsReady = true;
        foreach (int clientId in _serverManager.Clients.Keys) {
            if (!_playerReadyDictionary.ContainsKey(clientId) || !_playerReadyDictionary[clientId]) {
                //This player is NOT ready
                allClientsReady = false;
                break;
            }
        }

        if (allClientsReady) {
            KitchenGameLobby.Instance.DeleteLobby();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    public bool IsPlayerReady(int clientId) {
        return _playerReadyDictionary.ContainsKey(clientId) && _playerReadyDictionary[clientId];
    }
}
