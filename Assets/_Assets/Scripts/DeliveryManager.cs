using System;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour {
    public event EventHandler OnRecipeSpawned;
    public event EventHandler OnRecipeCompleted;
    public event EventHandler OnRecipeSuccess;
    public event EventHandler OnRecipeFailed;
    
    public static DeliveryManager Instance { get; private set; }
    
    [SerializeField] private RecipeListSO recipeListSO;
    
    private List<RecipeSO> _waitingRecipeSOList;
    private float _spawnRecipeTimer = 4f;
    private float _spawnRecipeTimerMax = 4f;
    private int _waitingRecipesMax = 4;
    private int _successfulRecipesAmount;

    private void Awake() {
        Instance = this;
        _waitingRecipeSOList = new List<RecipeSO>();
    }

    private void Update() {
        if (!IsServerStarted) return;
        _spawnRecipeTimer -= Time.deltaTime;
        if (GameManager.Instance.IsGamePlaying() && _spawnRecipeTimer <= 0f) {
            _spawnRecipeTimer = _spawnRecipeTimerMax;

            if (_waitingRecipeSOList.Count < _waitingRecipesMax) {
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);
                
                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    [ObserversRpc(RunLocally = true)]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex) {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];
        _waitingRecipeSOList.Add(waitingRecipeSO);
            
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }
    
    public void DeliverRecipe(PlateKitchenObject plateKitchenObject) {
        for (int i = 0; i < _waitingRecipeSOList.Count; i++) {
            RecipeSO waitingRecipeSO = _waitingRecipeSOList[i];

            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count) {
                // Has the same number of ingredients.
                bool plateContentsMatchesRecipe = true;
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList) {
                    // Cycling through all ingredients in the waiting recipe.
                    bool ingredientFound = false;
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList()) {
                        // Cycling through all the ingredients in the Plate
                        if (plateKitchenObjectSO == recipeKitchenObjectSO) {
                            ingredientFound = true;
                            break;
                        }
                    }

                    if (!ingredientFound) {
                        // This waiting recipe ingredient was not found on the plate.
                        plateContentsMatchesRecipe = false;
                    }
                }

                if (plateContentsMatchesRecipe) {
                    // Player delivered the correct recipe!
                    
                    // Leaving deliveries to happen on the server without prediction. There is actual logic here not
                    // just animations so it needs to be accurate. Also delay on a delivery feels ok to the player.
                    DeliverCorrectRecipeServerRpc(i);
                    return;
                }
            }
        }
        // No matches found!
        // Player did not deliver a correct recipe
        DeliverIncorrectRecipeServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverIncorrectRecipeServerRpc() {
        DeliverIncorrectRecipeClientRpc();
    }

    [ObserversRpc(RunLocally = true)]
    private void DeliverIncorrectRecipeClientRpc() {
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOListIndex) {
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    [ObserversRpc(RunLocally = true)]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex) {
        _successfulRecipesAmount++;
        _waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);
                    
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    public List<RecipeSO> GetWaitingRecipeSOList() {
        return _waitingRecipeSOList;
    }

    public int GetSuccessfulRecipesAmount() {
        return _successfulRecipesAmount;
    }
}
