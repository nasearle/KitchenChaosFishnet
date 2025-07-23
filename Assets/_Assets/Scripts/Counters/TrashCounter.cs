using System;
using FishNet.Object;
using UnityEngine;

public class TrashCounter : BaseCounter {
    public static event EventHandler OnAnyObjectTrashed;
    
    public new static void ResetStaticData() {
        OnAnyObjectTrashed = null;
    }
    public event EventHandler OnPlayerTrashedObject;
    
    private bool _objectAlreadyTrashedLocally;
    
    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            KitchenObject kitchenObject = player.GetKitchenObject();
            kitchenObject.DestroySelf();
            
            OnPlayerTrashedObject?.Invoke(this, EventArgs.Empty);
            OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
            _objectAlreadyTrashedLocally = true;
            
            InteractLogicServerRpc(player.GetComponent<NetworkObject>());
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc(NetworkObject playerNetworkObject) {
        InteractLogicClientRpc(playerNetworkObject);
    }

    [ObserversRpc]
    private void InteractLogicClientRpc(NetworkObject playerNetworkObject) {
        if (!_objectAlreadyTrashedLocally) {
            Player player = playerNetworkObject.GetComponent<Player>();
            player.ClearKitchenObject();
        
            OnPlayerTrashedObject?.Invoke(this, EventArgs.Empty);
            OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
        }

        _objectAlreadyTrashedLocally = false;
    }
}
