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

        if (NetworkConnections.Instance != null) {
            Destroy(NetworkConnections.Instance.gameObject);
        }

        if (KitchenGameLobby.Instance != null) {
            Destroy(KitchenGameLobby.Instance.gameObject);
        }

        if (KitchenGameDedicatedServer.Instance != null) {
            Destroy(KitchenGameDedicatedServer.Instance.gameObject);
        }
    }
}
