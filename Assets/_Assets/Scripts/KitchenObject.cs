using System;
using FishNet.Object;
using UnityEngine;

public class KitchenObject : NetworkBehaviour {
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent _kitchenObjectParent;

    public KitchenObjectSO GetKitchenObjectSO() {
        return kitchenObjectSO;
    }

    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent) {
        if (IsServerStarted) {
            SetKitchenObjectParentClientRpc(kitchenObjectParent.GetNetworkObject());
        } else {
            SetKitchenObjectParentServerRpc(kitchenObjectParent.GetNetworkObject());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKitchenObjectParentServerRpc(NetworkObject kitchenObjectParentNetworkObject) {
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObject);
    }

    [ObserversRpc(RunLocally = true)]
    private void SetKitchenObjectParentClientRpc(NetworkObject kitchenObjectParentNetworkObject) {
        IKitchenObjectParent kitchenObjectParent =
            kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        // Clear previous counter
        if (_kitchenObjectParent != null) {
            _kitchenObjectParent.ClearKitchenObject();
        }
        
        // Set new counter
        _kitchenObjectParent = kitchenObjectParent;

        if (kitchenObjectParent.HasKitchenObject()) {
            Debug.LogError("kitchenObjectParent already has a KitchenObject!");
        }
        
        kitchenObjectParent.SetKitchenObject(this);
        NetworkObject.transform.SetParent(kitchenObjectParent.GetKitchenObjectFollowTransform());
        NetworkObject.transform.localPosition = Vector3.zero;
    }

    public IKitchenObjectParent GetKitchenObjectParent() {
        return _kitchenObjectParent;
    }

    public void DestroySelf() {
        Despawn();
    }

    public void ClearKitchenObjectOnParent() {
        _kitchenObjectParent.ClearKitchenObject();
    }

    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject) {
        if (this is PlateKitchenObject) {
            plateKitchenObject = (this as PlateKitchenObject);
            return true;
        }
        plateKitchenObject = null;
        return false;
    }

    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSo,
        IKitchenObjectParent kitchenObjectParent) {
        KitchenGameMultiplayer.Instance.SpawnKitchenObject(kitchenObjectSo, kitchenObjectParent);
    }

    public static void DestroyKitchenObject(KitchenObject kitchenObject) {
        KitchenGameMultiplayer.Instance.DestroyKitchenObject(kitchenObject);
    }
}
