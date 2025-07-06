using System;
using Unity.VisualScripting;
using UnityEngine;

public class StoveCounterVisual : MonoBehaviour {
    
    [SerializeField] private StoveCounter stoveCounter;
    [SerializeField] private GameObject stoveOnGameObject;
    [SerializeField] private GameObject particlesGameObject;

    private void Start() {
        stoveCounter.OnStateChanged += StoveCounterOnStateChanged;
    }

    private void StoveCounterOnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e) {
        bool showVisual = e.State == StoveCounter.State.Frying || e.State == StoveCounter.State.Fried;
        stoveOnGameObject.SetActive(showVisual);
        particlesGameObject.SetActive(showVisual);
    }
}
