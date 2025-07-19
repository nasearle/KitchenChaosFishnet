using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SelectedCounterVisual : MonoBehaviour {
    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject[] visualGameObjectArray;
    
    private void Start() {
        if (Player.LocalInstance != null) {
            Player.LocalInstance.OnSelectedCounterChanged += PlayerOnSelectedCounterChanged;
        } else {
            Player.OnAnyPlayerSpawned += PlayerOnAnyPlayerSpawned;
        }
    }

    private void PlayerOnAnyPlayerSpawned(object sender, EventArgs e) {
        if (Player.LocalInstance == sender as Player) {
            Player.LocalInstance.OnSelectedCounterChanged += PlayerOnSelectedCounterChanged;
        }
    }

    private void PlayerOnSelectedCounterChanged(object sender, Player.OnSelectedCounterChangedEventArgs e) {
        if (e.SelectedCounter == baseCounter) {
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        foreach (GameObject visualGameObject in visualGameObjectArray) {
            visualGameObject.SetActive(true);
        }
    }

    private void Hide() {
        foreach (GameObject visualGameObject in visualGameObjectArray) {
            visualGameObject.SetActive(false);
        }
    }
}
