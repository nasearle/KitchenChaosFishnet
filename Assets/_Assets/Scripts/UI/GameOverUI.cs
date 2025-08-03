using System;
using FishNet;
using FishNet.Managing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI recipesDeliveredText;
    [SerializeField] private Button playAgainButton;
    
    private NetworkManager _networkManager;

    private void Awake() {
        _networkManager = InstanceFinder.NetworkManager;
        
        playAgainButton.onClick.AddListener(() => {
            _networkManager.ClientManager.StopConnection();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
    }

    private void Start() {
        GameManager.Instance.OnStateChanged += GameManagerOnStateChanged;

        Hide();
    }

    private void GameManagerOnStateChanged(object sender, EventArgs e) {
        if (GameManager.Instance.IsGameOver()) {
            Show();
            recipesDeliveredText.text = DeliveryManager.Instance.GetSuccessfulRecipesAmount().ToString();
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
        
        playAgainButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
