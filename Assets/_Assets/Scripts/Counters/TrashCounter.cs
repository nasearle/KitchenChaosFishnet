using System;
using UnityEngine;

public class TrashCounter : BaseCounter {
    public event EventHandler OnPlayerTrashedObject;
    public override void Interact(Player player) {
        if (player.HasKitchenObject()) {
            player.GetKitchenObject().DestroySelf();
            
            OnPlayerTrashedObject?.Invoke(this, EventArgs.Empty);
        }
    }
}
