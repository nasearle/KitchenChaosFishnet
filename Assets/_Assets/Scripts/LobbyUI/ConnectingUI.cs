using System;
using UnityEngine;

public class ConnectingUI : MonoBehaviour {
    private void Start() {
        LobbyPlayerConnection.Instance.OnTryingToJoinGame += LobbyPlayerConnectionOnTryingToJoinGame;
        LobbyPlayerConnection.Instance.OnFailedToJoinGame += LobbyPlayerConnectionOnFailedToJoinGame;
        
        Hide();
    }
    private void LobbyPlayerConnectionOnFailedToJoinGame(object sender, EventArgs e) {
        Hide();
    }

    private void LobbyPlayerConnectionOnTryingToJoinGame(object sender, EventArgs e) {
        Show();
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        LobbyPlayerConnection.Instance.OnTryingToJoinGame -= LobbyPlayerConnectionOnTryingToJoinGame;
        LobbyPlayerConnection.Instance.OnFailedToJoinGame -= LobbyPlayerConnectionOnFailedToJoinGame;
    }
}
