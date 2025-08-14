using System;
using UnityEngine;
using UnityEngine.UI;

public class CharacterColorSelectSingleUI : MonoBehaviour {
    [SerializeField] private int colorId;
    [SerializeField] private Image image;
    [SerializeField] private GameObject selectedGameObject;

    void Awake() {
        GetComponent<Button>().onClick.AddListener(() => {
            NetworkConnections.Instance.ChangePlayerColor(colorId);
        });
    }

    void Start() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged += NetworkConnectionOnPlayerDataSyncListChanged;
        image.color = NetworkConnections.Instance.GetPlayerColor(colorId);

        UpdateIsSelected();
    }

    private void NetworkConnectionOnPlayerDataSyncListChanged(object sender, EventArgs e) {
        UpdateIsSelected();
    }

    private void UpdateIsSelected() {
        if (NetworkConnections.Instance.GetPlayerData().colorId == colorId) {
            selectedGameObject.SetActive(true);
        } else {
            selectedGameObject.SetActive(false);
        }
    }

    private void OnDestroy() {
        NetworkConnections.Instance.OnPlayerDataSyncListChanged -= NetworkConnectionOnPlayerDataSyncListChanged;
    }
}
