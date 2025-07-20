using UnityEngine;
using FishNet.Object;

public class KitchenGameMultiplayer : NetworkBehaviour {
     public static KitchenGameMultiplayer Instance { get; private set; }
     
     [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

     private void Awake() {
          Instance = this;
     }

     public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSo, IKitchenObjectParent kitchenObjectParent) {
          SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSo), kitchenObjectParent.GetNetworkObject());
     }

     [ServerRpc(RequireOwnership = false)]
     private void SpawnKitchenObjectServerRpc(int kitchenObjectSoIndex, NetworkObject kitchenObjectParentNetworkObject) {
          KitchenObjectSO kitchenObjectSo = GetKitchenObjectSOFromIndex(kitchenObjectSoIndex);
          Transform kitchenObjectTransform = Instantiate(kitchenObjectSo.prefab);
          NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
          ServerManager.Spawn(kitchenObjectNetworkObject);
          
          KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
          IKitchenObjectParent kitchenObjectParent =
               kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();
          
          kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
     }

     private int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
          return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
     }

     private KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
          return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
     }
}
