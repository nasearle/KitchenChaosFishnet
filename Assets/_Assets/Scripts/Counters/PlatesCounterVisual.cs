using System;
using System.Collections.Generic;
using UnityEngine;

public class PlatesCounterVisual : MonoBehaviour {
    [SerializeField] private PlatesCounter platesCounter;
    [SerializeField] private Transform counterTopPoint;
    [SerializeField] private Transform plateVisualPrefab;
    
    private List<GameObject> _plateVisualGameObjectList;

    private void Awake() {
        _plateVisualGameObjectList = new List<GameObject>();
    }

    private void Start() {
        platesCounter.OnPlateSpawned += PlatesCounterOnPlateSpawned;
        platesCounter.OnPlateRemoved += PlatesCounterOnPlateRemoved;
    }

    private void PlatesCounterOnPlateRemoved(object sender, EventArgs e) {
        GameObject plateGameObject = _plateVisualGameObjectList[^1];
        _plateVisualGameObjectList.Remove(plateGameObject);
        Destroy(plateGameObject);
    }

    private void PlatesCounterOnPlateSpawned(object sender, EventArgs e) {
        Transform plateVisualTransform = Instantiate(plateVisualPrefab, counterTopPoint);

        float plateOffsetY = .1f;
        
        plateVisualTransform.localPosition = new Vector3(0, plateOffsetY * _plateVisualGameObjectList.Count, 0);
        
        _plateVisualGameObjectList.Add(plateVisualTransform.gameObject);
    }
}
