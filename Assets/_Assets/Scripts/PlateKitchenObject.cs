using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlateKitchenObject : KitchenObject {
    public static event EventHandler OnAnyObjectPlated;
    
    public static void ResetStaticData() {
        OnAnyObjectPlated = null;
    }
    
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

        AddIngredientServerRpc(KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObjectSO));
        return true;
    }

    [ServerRpc(RequireOwnership = false)]
    private void AddIngredientServerRpc(int kitchenObjectSOIndex) {
        AddIngredientClientRpc(kitchenObjectSOIndex);
    }

    [ObserversRpc(RunLocally = true)]
    private void AddIngredientClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO =
            KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        _kitchenObjectSOList.Add(kitchenObjectSO);
        
        OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs {
            KitchenObjectSO = kitchenObjectSO
        });
        
        OnAnyObjectPlated?.Invoke(this, EventArgs.Empty);
    }

    public List<KitchenObjectSO> GetKitchenObjectSOList() {
        return _kitchenObjectSOList;
    }
}
