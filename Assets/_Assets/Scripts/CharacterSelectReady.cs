using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Connection;
using FishNet.Managing.Server;
using FishNet.Object;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour {
    public static CharacterSelectReady Instance { get; private set; }
    
    private Dictionary<int, bool> _playerReadyDictionary;
    
    private ServerManager _serverManager;

    private void Awake() {
        Instance = this;
        
        _playerReadyDictionary = new Dictionary<int, bool>();

        _serverManager = InstanceFinder.ServerManager;
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
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }
}
