using System;
using UnityEngine;

public class CuttingCounter : BaseCounter, IHasProgess {
    public static event EventHandler OnAnyCut;

    public new static void ResetStaticData() {
        OnAnyCut = null;
    }
    
    public event EventHandler<IHasProgess.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler OnCut;
    
    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray;

    private int _cuttingProgress;
    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            if (player.HasKitchenObject()) {
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    
                    // There is a bug here where the setkitchenobjectparent happens over the network, but the code below
                    // this line expects the kitchen object parent to be set synchronously on the local client
                    
                    player.GetKitchenObject().SetKitchenObjectParent(this);
                    _cuttingProgress = 0;
                    
                    CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
                    
                    OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
                        ProgressNormalized = (float)_cuttingProgress / cuttingRecipeSO.cuttingProgressMax
                    });
                }
            }
        } else {
            // There is a kitchen object here.
            if (player.HasKitchenObject()) {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    // Player is holding a plate.
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        GetKitchenObject().DestroySelf();
                    }
                }
            } else {
                GetKitchenObject().SetKitchenObjectParent(player);
                OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
                    ProgressNormalized = 0f
                });
            }
        }
    }

    public override void InteractAlternate(Player player) {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            _cuttingProgress++;
            
            KitchenObject kitchenObject = GetKitchenObject();
            KitchenObjectSO inputKitchenObjectSO = kitchenObject.GetKitchenObjectSO();
            
            OnCut?.Invoke(this, EventArgs.Empty);
            OnAnyCut?.Invoke(this, EventArgs.Empty);
            
            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
            
            OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
                ProgressNormalized = (float)_cuttingProgress / cuttingRecipeSO.cuttingProgressMax
            });

            if (_cuttingProgress >= cuttingRecipeSO.cuttingProgressMax) {
                kitchenObject.DestroySelf();
                KitchenObject.SpawnKitchenObject(GetOutputForInput(inputKitchenObjectSO), this);
            }
        }
    }

    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        return GetCuttingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        if (cuttingRecipeSO != null) {
            return cuttingRecipeSO.output;
        }
        return null;
    }

    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray) {
            if (cuttingRecipeSO.input == inputKitchenObjectSO) {
                return cuttingRecipeSO;
            }
        }
        return null;
    }
}
