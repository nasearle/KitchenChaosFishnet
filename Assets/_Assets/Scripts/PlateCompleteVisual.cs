using System;
using System.Collections.Generic;
using UnityEngine;

public class PlateCompleteVisual : MonoBehaviour {
    
    [Serializable]
    public struct KitchenObjectSO_GameObject {
        public KitchenObjectSO KitchenObjectSO;
        public GameObject GameObject;
    }
    
    [SerializeField] private PlateKitchenObject plateKitchenObject;
    [SerializeField] private List<KitchenObjectSO_GameObject> kitchenObjectSOGameObjectList;

    private void Start() {
        plateKitchenObject.OnIngredientAdded += PlateKitchenObjectOnIngredientAdded;
        
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            kitchenObjectSOGameObject.GameObject.SetActive(false);
        }
    }

    private void PlateKitchenObjectOnIngredientAdded(object sender, PlateKitchenObject.OnIngredientAddedEventArgs e) {
        foreach (KitchenObjectSO_GameObject kitchenObjectSOGameObject in kitchenObjectSOGameObjectList) {
            if (kitchenObjectSOGameObject.KitchenObjectSO == e.KitchenObjectSO) {
                kitchenObjectSOGameObject.GameObject.SetActive(true);
            }
        }
    }
}
