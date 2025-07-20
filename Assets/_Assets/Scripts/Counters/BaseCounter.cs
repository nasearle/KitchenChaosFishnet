using System;
using FishNet.Object;
using UnityEngine;

public class BaseCounter : MonoBehaviour, IKitchenObjectParent {
    public static event EventHandler OnAnyObjectPlacedHere;
    
    public static void ResetStaticData() {
        OnAnyObjectPlacedHere = null;
    }
    
    [SerializeField] private Transform counterTopPoint;

    private KitchenObject _kitchenObject;
    
    public virtual void Interact(Player player) {
        Debug.LogError("BaseCounter.Interact()");
    }
    
    public virtual void InteractAlternate(Player player) {
        // Debug.LogError("BaseCounter.InteractAlternate()");
    }
    
    public Transform GetKitchenObjectFollowTransform() {
        return counterTopPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        _kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() {
        return _kitchenObject;
    }

    public void ClearKitchenObject() {
        _kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return _kitchenObject != null;
    }
    
    public NetworkObject GetNetworkObject() {
        return null;
    }
}
