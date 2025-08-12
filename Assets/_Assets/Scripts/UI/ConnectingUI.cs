using System;
using UnityEngine;

public class ConnectingUI : MonoBehaviour {
    private void Start() {
        LobbyPlayerConnections.Instance.OnTryingToJoinGame += LobbyPlayerConnectionsOnTryingToJoinGame;
        LobbyPlayerConnections.Instance.OnFailedToJoinGame += LobbyPlayerConnectionsOnFailedToJoinGame;
        
        Hide();
    }
    private void LobbyPlayerConnectionsOnFailedToJoinGame(object sender, EventArgs e) {
        Hide();
    }

    private void LobbyPlayerConnectionsOnTryingToJoinGame(object sender, EventArgs e) {
        Show();
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        LobbyPlayerConnections.Instance.OnTryingToJoinGame -= LobbyPlayerConnectionsOnTryingToJoinGame;
        LobbyPlayerConnections.Instance.OnFailedToJoinGame -= LobbyPlayerConnectionsOnFailedToJoinGame;
    }
}
