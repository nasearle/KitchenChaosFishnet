using System;
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
        _spawnPlateTimer += Time.deltaTime;
        if (_spawnPlateTimer > _spawnPlateTimerMax) {
            _spawnPlateTimer = 0f;
            if (_platesSpawnedAmount < _platesSpawnedAmountMax) {
                _platesSpawnedAmount++;
                
                OnPlateSpawned?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public override void Interact(Player player) {
        if (!player.HasKitchenObject()) {
            if (_platesSpawnedAmount > 0) {
                _platesSpawnedAmount--;

                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);
                
                OnPlateRemoved?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
