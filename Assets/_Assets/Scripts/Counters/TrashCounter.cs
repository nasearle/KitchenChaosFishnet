using System;
using FishNet.Object;
using UnityEngine;

public class TrashCounter : BaseCounter {
    public static event EventHandler OnAnyObjectTrashed;
    
    public new static void ResetStaticData() {
        OnAnyObjectTrashed = null;
    }
    public event EventHandler OnPlayerTrashedObject;
    
    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            InteractLogicServerRpc();
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    [ObserversRpc]
    private void InteractLogicClientRpc() {
        OnPlayerTrashedObject?.Invoke(this, EventArgs.Empty);
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}
