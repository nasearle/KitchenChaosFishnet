using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour {
    [SerializeField] private int colorId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    void Start() {
        GetComponent<Button>().onClick.AddListener(() => {
            KitchenGameLobby.Instance.SetPlayerColor(colorId);
        });

        KitchenGameLobby.Instance.OnPlayerDataChanged += KitchenGameLobbyOnPlayerDataChanged;

        image.color = KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId);

        UpdateIsSelected();
    }

    private void KitchenGameLobbyOnPlayerDataChanged(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void UpdateIsSelected() {
        SetColorAsSelected(KitchenGameLobby.Instance.GetPlayerColorId());
    }

    private void SetColorAsSelected(int colorId) {
        if (colorId == this.colorId) {
            selectedGameObject.SetActive(true);
        } else {
            selectedGameObject.SetActive(false);
        }
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnPlayerDataChanged -= KitchenGameLobbyOnPlayerDataChanged;
    }
}
