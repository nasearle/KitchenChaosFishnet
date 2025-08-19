using UnityEngine;
using FishNet.Object;

public class KitchenGameMultiplayer : NetworkBehaviour {
     public static KitchenGameMultiplayer Instance { get; private set; }
     
     [SerializeField] private KitchenObjectListSO kitchenObjectListSO;

     private void Awake() {
          Instance = this;
     }

     private void SpawnKitchenObjectLogic(KitchenObjectSO kitchenObjectSo, IKitchenObjectParent kitchenObjectParent) {
          // TODO: small visual bug when a player spams picking up an object at
          // a container counter that causes the animation to be replayed
          if (kitchenObjectParent.HasKitchenObject()) {
               // Parent already spawned an object
               return;
          }

          Transform kitchenObjectTransform = Instantiate(kitchenObjectSo.prefab);
          NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();
          ServerManager.Spawn(kitchenObjectNetworkObject);
          
          KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();
          
          kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
     }
     
     public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSo, IKitchenObjectParent kitchenObjectParent) {
          if (IsServerStarted) {
               SpawnKitchenObjectLogic(kitchenObjectSo, kitchenObjectParent);
          } else {
               SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSo), kitchenObjectParent.GetNetworkObject());
          }
     }

     [ServerRpc(RequireOwnership = false)]
     private void SpawnKitchenObjectServerRpc(int kitchenObjectSoIndex, NetworkObject kitchenObjectParentNetworkObject) {
          KitchenObjectSO kitchenObjectSo = GetKitchenObjectSOFromIndex(kitchenObjectSoIndex);
          IKitchenObjectParent kitchenObjectParent =
               kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

          SpawnKitchenObjectLogic(kitchenObjectSo, kitchenObjectParent);
     }

     public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO) {
          return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
     }

     public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex) {
          return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
     }

     public void DestroyKitchenObject(KitchenObject kitchenObject) {
          if (IsServerStarted) {
               ClearKitchenObjectOnParentClientRpc(kitchenObject.NetworkObject);
               kitchenObject.ClearKitchenObjectOnParent();
               kitchenObject.DestroySelf();
          } else {
               DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
          }
     }

     [ServerRpc(RequireOwnership = false)]
     private void DestroyKitchenObjectServerRpc(NetworkObject kitchenNetworkObject) {
          if (kitchenNetworkObject == null) {
               // This object is already destroyed
               return;
          }

          KitchenObject kitchenObject = kitchenNetworkObject.GetComponent<KitchenObject>();

          ClearKitchenObjectOnParentClientRpc(kitchenNetworkObject);
          
          kitchenObject.ClearKitchenObjectOnParent();
          kitchenObject.DestroySelf();
     }

     [ObserversRpc]
     private void ClearKitchenObjectOnParentClientRpc(NetworkObject kitchenNetworkObject) {
          KitchenObject kitchenObject = kitchenNetworkObject.GetComponent<KitchenObject>();
          
          kitchenObject.ClearKitchenObjectOnParent();
     }
}
