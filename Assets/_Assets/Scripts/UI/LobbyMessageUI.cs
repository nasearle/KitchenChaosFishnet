using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyMessageUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private Button closeButton;

    private void Awake() {
        closeButton.onClick.AddListener(Hide);
    }

    private void Start() {
        LobbyPlayerConnections.Instance.OnFailedToJoinGame += LobbyPlayerConnectionsOnFailedToJoinGame;
        
        Hide();
    }

    private void LobbyPlayerConnectionsOnFailedToJoinGame(object sender, EventArgs e) {
        Show();

        // Fishnet doesn't have a way to provide a custom reason for getting kicked, so the client doesn't know if they
        // failed to connect because of a timeout or the game is full or already started.
        messageText.text = "Couldn't connect";
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        LobbyPlayerConnections.Instance.OnFailedToJoinGame -= LobbyPlayerConnectionsOnFailedToJoinGame;
    }
}
