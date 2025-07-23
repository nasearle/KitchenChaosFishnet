using FishNet.Connection;
using UnityEngine;
using FishNet.Object;
using FishNet.Serializing;

public class KitchenGameMultiplayer : NetworkBehaviour {
     public static KitchenGameMultiplayer Instance { get; private set; }

     private void Awake() {
          Instance = this;
     }
     
     public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSo, IKitchenObjectParent kitchenObjectParent) {
          Transform kitchenObjectTransform = Instantiate(kitchenObjectSo.prefab);
          NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
          Spawn(kitchenObjectNetworkObject);
          
          KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
          kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
     }
}
