using System;
using UnityEngine;

public class CharacterSelectPlayer : MonoBehaviour {
    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyGameObject;

    private void Start() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged += NetworkConnectionsOnPlayerDataSyncListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReadyOnReadyChanged;

        UpdatePlayer();
    }

    private void CharacterSelectReadyOnReadyChanged(object sender, EventArgs e) {
        UpdatePlayer();
    }

    private void NetworkConnectionsOnPlayerDataSyncListChanged(object sender, EventArgs e) {
        UpdatePlayer();
    }

    private void UpdatePlayer() {
        if (NetworkConnections.Instance.IsPlayerIndexConnected(playerIndex)) {
            Show();

            PlayerData playerData = NetworkConnections.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));
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
