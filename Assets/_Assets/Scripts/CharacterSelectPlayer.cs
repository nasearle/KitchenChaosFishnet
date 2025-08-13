using System;
using UnityEngine;

public class CharacterSelectPlayer : MonoBehaviour {
    [SerializeField] private int playerIndex;

    private void Start() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged += NetworkConnectionsOnPlayerDataSyncListChanged;
    
        UpdatePlayer();
    }

    private void NetworkConnectionsOnPlayerDataSyncListChanged(object sender, EventArgs e) {
        UpdatePlayer();
    }

    private void UpdatePlayer() {
        if (NetworkConnections.Instance.IsPlayerIndexConnected(playerIndex)) {
            Show();
        } else {
            Hide();
        }
    }

    public void Show() {
        gameObject.SetActive(true);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }
}
