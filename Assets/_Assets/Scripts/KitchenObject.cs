using System;
using FishNet.Object;
using UnityEngine;

public class KitchenObject : NetworkBehaviour {
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private IKitchenObjectParent _kitchenObjectParent;
    private bool _parentObjectAlreadySetLocally;

    public KitchenObjectSO GetKitchenObjectSO() {
        return kitchenObjectSO;
    }

    private void SetKitchenObjectParentLocally(IKitchenObjectParent kitchenObjectParent) {
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

    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent) {
        if (IsServerStarted) {
            // If we are already running on the server, call the client RPC directly (with RunLocally set to true) to
            // avoid the issue where a UGS server can't call a Server RPC from the server itself.
            SetKitchenObjectParentClientRpc(kitchenObjectParent.GetNetworkObject());
        } else {
            SetKitchenObjectParentLocally(kitchenObjectParent);
            _parentObjectAlreadySetLocally = true;
            SetKitchenObjectParentServerRpc(kitchenObjectParent.GetNetworkObject());
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetKitchenObjectParentServerRpc(NetworkObject kitchenObjectParentNetworkObject) {
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObject);
    }

    [ObserversRpc(RunLocally = true)]
    private void SetKitchenObjectParentClientRpc(NetworkObject kitchenObjectParentNetworkObject) {
        if (!_parentObjectAlreadySetLocally) {
            IKitchenObjectParent kitchenObjectParent =
                kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

            SetKitchenObjectParentLocally(kitchenObjectParent);
        }

        _parentObjectAlreadySetLocally = false;
    }

    public IKitchenObjectParent GetKitchenObjectParent() {
        return _kitchenObjectParent;
    }

    public void DestroySelf() {
        _kitchenObjectParent.ClearKitchenObject();
        
        Despawn();
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
}
