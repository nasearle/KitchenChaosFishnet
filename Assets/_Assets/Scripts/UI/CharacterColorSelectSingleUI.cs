using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour {
    [SerializeField] private int colorId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    void Start() {
        GetComponent<Button>().onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.SetPlayerColor(colorId);
        });

        KitchenGameLobby.Instance.OnLobbyJoined += KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated += KitchenGameLobbyOnJoinedLobbyDataUpdated;
        KitchenGameLobby.Instance.OnPlayerDataUpdated += KitchenGameLobbyOnPlayerDataUpdated;

        image.color = KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId);

        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnPlayerDataUpdated(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            return;
        }
        
        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnLobbyJoined(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnJoinedLobbyDataUpdated(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void UpdateIsSelected() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerData();

            SetColorAsSelected(LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, "colorId"));
        } else {
            SetColorAsSelected(KitchenGameLobby.Instance.GetPlayerColor());
        }
    }

    private void SetColorAsSelected(int colorId) {
        if (colorId == this.colorId) {
            selectedGameObject.SetActive(true);
        } else {
            selectedGameObject.SetActive(false);
        }
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyJoined -= KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated -= KitchenGameLobbyOnJoinedLobbyDataUpdated;
        KitchenGameLobby.Instance.OnPlayerDataUpdated -= KitchenGameLobbyOnPlayerDataUpdated;
    }
}
