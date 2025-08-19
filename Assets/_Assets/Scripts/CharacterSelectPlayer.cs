using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPlayer : MonoBehaviour {
    [SerializeField] private int playerIndex;
    [SerializeField] private GameObject readyGameObject;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshPro playerNameText;

    void Awake() {
        kickButton.onClick.AddListener(() => {
            PlayerData playerData = NetworkConnections.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.playerId);
            NetworkConnections.Instance.KickPlayer(playerData.clientId);
        });
    }

    private void Start() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged += NetworkConnectionsOnPlayerDataSyncListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReadyOnReadyChanged;

        kickButton.gameObject.SetActive(KitchenGameLobby.Instance.IsLobbyHost());

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

            playerNameText.text = playerData.playerName;

            playerVisual.SetPlayerColor(NetworkConnections.Instance.GetPlayerColor(playerData.colorId));
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

    private void OnDestroy() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged -= NetworkConnectionsOnPlayerDataSyncListChanged;
    }
}
