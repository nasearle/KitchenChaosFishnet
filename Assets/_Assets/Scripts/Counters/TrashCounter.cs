using System;
using UnityEngine;

public class TrashCounter : BaseCounter {
    public static event EventHandler OnAnyObjectTrashed;
    
    public new static void ResetStaticData() {
        OnAnyObjectTrashed = null;
    }
    public event EventHandler OnPlayerTrashedObject;
    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            player.GetKitchenObject().DestroySelf();
            
            OnPlayerTrashedObject?.Invoke(this, EventArgs.Empty);
            OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
        }
    }
}
