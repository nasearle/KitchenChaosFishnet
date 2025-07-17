using System;
using UnityEngine;
using UnityEngine.Serialization;

public class SelectedCounterVisual : MonoBehaviour {
    [SerializeField] private BaseCounter baseCounter;
    [SerializeField] private GameObject[] visualGameObjectArray;
    
    private void Start() {
        // Player.Instance.OnSelectedCounterChanged += PlayerOnSelectedCounterChanged;
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
