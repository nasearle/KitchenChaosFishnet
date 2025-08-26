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
        LobbyPlayerConnection.Instance.OnFailedToJoinGame += LobbyPlayerConnectionOnFailedToJoinGame;
        KitchenGameLobby.Instance.OnLobbyCreateStarted += KitchenGameLobbyOnLobbyCreateStarted;
        KitchenGameLobby.Instance.OnLobbyCreateFailed += KitchenGameLobbyOnLobbyCreateFailed;
        KitchenGameLobby.Instance.OnLobbyJoinStarted += KitchenGameLobbyOnLobbyJoinStarted;
        KitchenGameLobby.Instance.OnLobbyJoinFailed += KitchenGameLobbyOnLobbyJoinFailed;
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveStarted += KitchenGameLobbyOnLobbyLeaveStarted;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        
        Hide();
    }

    private void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        Hide();
    }

    private void KitchenGameLobbyOnLobbyLeaveStarted(object sender, EventArgs e) {
        ShowMessage("Leaving Lobby...");
    }

    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        Hide();
    }

    private void KitchenGameLobbyOnLobbyCreateFailed(object sender, EventArgs e) {
        ShowMessage("Failed to create Lobby!");
    }

    private void KitchenGameLobbyOnLobbyJoinStarted(object sender, EventArgs e) {
        ShowMessage("Joining Lobby...");
    }

    private void KitchenGameLobbyOnLobbyJoinFailed(object sender, EventArgs e) {
        ShowMessage("Failed to join Lobby!");
    }

    private void KitchenGameLobbyOnQuickJoinFailed(object sender, EventArgs e) {
        ShowMessage("Could not find a Lobby to Quick Join!");
    }

    private void KitchenGameLobbyOnLobbyCreateStarted(object sender, EventArgs e) {
        ShowMessage("Creating Lobby...");
    }

    private void LobbyPlayerConnectionOnFailedToJoinGame(object sender, EventArgs e) {
        // Fishnet doesn't have a way to provide a custom reason for getting kicked, so the client doesn't know if they
        // failed to connect because of a timeout or the game is full or already started.
        ShowMessage("Couldn't connect");
    }

    private void ShowMessage(string message) {
        Show();
        messageText.text = message;
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        LobbyPlayerConnection.Instance.OnFailedToJoinGame -= LobbyPlayerConnectionOnFailedToJoinGame;
        KitchenGameLobby.Instance.OnLobbyCreateStarted -= KitchenGameLobbyOnLobbyCreateStarted;
        KitchenGameLobby.Instance.OnLobbyCreateFailed -= KitchenGameLobbyOnLobbyCreateFailed;
        KitchenGameLobby.Instance.OnLobbyJoinStarted -= KitchenGameLobbyOnLobbyJoinStarted;
        KitchenGameLobby.Instance.OnLobbyJoinFailed -= KitchenGameLobbyOnLobbyJoinFailed;
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveStarted -= KitchenGameLobbyOnLobbyLeaveStarted;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
    }
}
