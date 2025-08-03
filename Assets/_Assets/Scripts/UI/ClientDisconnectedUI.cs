using System;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.UI;

public class ClientDisconnectedUI : MonoBehaviour {
    [SerializeField] private Button _playAgainButton;
    
    private NetworkManager _networkManager;

    private void Start() {
        _networkManager = InstanceFinder.NetworkManager;
        _networkManager.ClientManager.OnClientConnectionState += ClientManagerOnClientConnectionState;
        
        Hide();
    }

    private void ClientManagerOnClientConnectionState(ClientConnectionStateArgs connectionStateArgs) {
        if (connectionStateArgs.ConnectionState == LocalConnectionState.Stopped) {
            // Client is disconnected
            Show();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
