using System;
using System.Collections.Generic;
using UnityEngine;

public class PlateKitchenObject : KitchenObject {
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;
    public class OnIngredientAddedEventArgs : EventArgs {
        public KitchenObjectSO KitchenObjectSO;
    }
    
    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList;
    
    private List<KitchenObjectSO> _kitchenObjectSOList;

    private void Awake() {
        _kitchenObjectSOList = new List<KitchenObjectSO>();
    }

    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO) {
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO)) {
            // Not a valid ingredient.
            return false;
        }
        if (_kitchenObjectSOList.Contains(kitchenObjectSO)) {
            return false;
        }
        _kitchenObjectSOList.Add(kitchenObjectSO);
        
        OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs {
            KitchenObjectSO = kitchenObjectSO
        });
        
        return true;
    }

    public List<KitchenObjectSO> GetKitchenObjectSOList() {
        return _kitchenObjectSOList;
    }
}
