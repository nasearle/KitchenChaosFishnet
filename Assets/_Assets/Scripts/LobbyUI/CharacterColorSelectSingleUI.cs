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

        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyAnyChange += KitchenGameLobbyOnJoinedLobbyAnyChange;
        KitchenGameLobby.Instance.OnPlayerDataChanged += KitchenGameLobbyOnPlayerDataChanged;

        image.color = KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId);

        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnPlayerDataChanged(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            return;
        }
        
        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnJoinedLobbyAnyChange(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void UpdateIsSelected() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataForLocalPlayer();

            SetColorAsSelected(LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, KitchenGameLobby.LobbyDataKeys.ColorId));
        } else {
            SetColorAsSelected(KitchenGameLobby.Instance.GetPlayerColorId());
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
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyAnyChange -= KitchenGameLobbyOnJoinedLobbyAnyChange;
        KitchenGameLobby.Instance.OnPlayerDataChanged -= KitchenGameLobbyOnPlayerDataChanged;
    }
}
