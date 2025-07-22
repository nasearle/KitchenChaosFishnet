using System;
using FishNet.Object;
using UnityEngine;

public class PlatesCounter : BaseCounter {
    public event EventHandler OnPlateSpawned;
    public event EventHandler OnPlateRemoved;
    
    [SerializeField] private KitchenObjectSO plateKitchenObjectSO;
    
    private float _spawnPlateTimer;
    private float _spawnPlateTimerMax = 4f;
    private int _platesSpawnedAmount;
    private int _platesSpawnedAmountMax = 4;

    private void Update() {
        if (!IsServerStarted) {
            return;
        }
        
        _spawnPlateTimer += Time.deltaTime;
        if (GameManager.Instance.IsGamePlaying() && _spawnPlateTimer > _spawnPlateTimerMax) {
            _spawnPlateTimer = 0f;
            
            if (_platesSpawnedAmount < _platesSpawnedAmountMax) {
                SpawnPlateClientRpc();
            }
        }
    }

    [ObserversRpc(RunLocally = true)]
    private void SpawnPlateClientRpc() {
        _platesSpawnedAmount++;
            
        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    // Interactions should really be handled server side only. Need to think about how to make these interactions
    // predicted on the client.
    // https://fish-networking.gitbook.io/docs/fishnet-building-blocks/components/prediction/predictedspawn
    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            if (_platesSpawnedAmount > 0) {
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                InteractLogicServerRpc();
            }
        }
    }
    
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc() {
        InteractLogicClientRpc();
    }

    [ObserversRpc(RunLocally = true)]
    private void InteractLogicClientRpc() {
        _platesSpawnedAmount--;
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }
}
