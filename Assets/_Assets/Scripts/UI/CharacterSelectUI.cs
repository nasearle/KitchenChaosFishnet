using System;
using FishNet;
using FishNet.Managing;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour {
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;

    private NetworkManager _networkManager;

    private void Awake() {
        _networkManager = InstanceFinder.NetworkManager;

        mainMenuButton.onClick.AddListener((() => {
            _networkManager.ClientManager.StopConnection();
            Loader.Load(Loader.Scene.MainMenuScene);
        }));
        readyButton.onClick.AddListener((() => {
            CharacterSelectReady.Instance.SetPlayerReady();
        }));
    }
}
