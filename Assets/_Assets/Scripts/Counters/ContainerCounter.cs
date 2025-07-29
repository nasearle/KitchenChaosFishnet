using System;
using FishNet.Object;
using FishNet.Component.Ownership;
using UnityEngine;

public class ContainerCounter : BaseCounter {
    public event EventHandler OnPlayerGrabbedObject;
    [SerializeField] private KitchenObjectSO kitchenObjectSO;

    private bool _objectAlreadyGrabbedLocally;
    
    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);
            OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
            _objectAlreadyGrabbedLocally = true;

            InteractLogicServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    [ObserversRpc]
    private void InteractLogicClientRpc() {
        if (!_objectAlreadyGrabbedLocally) {
            OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
        }
        _objectAlreadyGrabbedLocally = false;
    }
}
