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
            await KitchenGameLobby.Instance.SetLobbyPlayerColor(colorId);
        });

        KitchenGameLobby.Instance.OnLobbyJoined += KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated += KitchenGameLobbyOnJoinedLobbyDataUpdated;
        image.color = KitchenGameLobby.Instance.GetPlayerColor(colorId);
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
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetPlayerData();
            if (LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, "colorId") == colorId) {
                selectedGameObject.SetActive(true);
            } else {
                selectedGameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyJoined -= KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated -= KitchenGameLobbyOnJoinedLobbyDataUpdated;
    }
}
