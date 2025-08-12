using System;
using FishNet;
using FishNet.Managing;
using UnityEngine;

public class MainMenuCleanUp : MonoBehaviour {
    private void Awake() {
        NetworkManager networkManager = InstanceFinder.NetworkManager;

        if (networkManager != null) {
            Destroy(networkManager.gameObject);
        }

        if (LobbyPlayerConnections.Instance != null) {
            Destroy(LobbyPlayerConnections.Instance.gameObject);
        }
    }
}
