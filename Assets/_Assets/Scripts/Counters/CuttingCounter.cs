using System;
using FishNet.Object;
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
    private bool _localCutAnimationPlayed;
    
    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            if (player.HasKitchenObject()) {
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);
                    PlaceObjectOnCounterServerRpc();
                }
            }
        } else {
            // There is a kitchen object here.
            if (player.HasKitchenObject()) {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    // Player is holding a plate.
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
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
            CutObjectServerRpc();

            // Play the animation and audio on the client immediately
            if (player.IsOwner) {
                OnCut?.Invoke(this, EventArgs.Empty);
                OnAnyCut?.Invoke(this, EventArgs.Empty);
                _localCutAnimationPlayed = true;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceObjectOnCounterServerRpc() {
        PlaceObjectOnCounterClientRpc();
    }
    
    [ObserversRpc(RunLocally = true)]
    private void PlaceObjectOnCounterClientRpc() {
        _cuttingProgress = 0;
                    
        OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
            ProgressNormalized = 0f
        });
    }

    [ServerRpc(RequireOwnership = false)]
    private void CutObjectServerRpc() {
        CutObjectClientRpc();
        TestCuttingProgressDone();
    }

    [ObserversRpc(RunLocally = true)]
    private void CutObjectClientRpc() {
        if (!HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            return;
        }
        
        _cuttingProgress++;

        if (!_localCutAnimationPlayed) {
            OnCut?.Invoke(this, EventArgs.Empty);
            OnAnyCut?.Invoke(this, EventArgs.Empty);
        }
        _localCutAnimationPlayed = false;

        KitchenObject kitchenObject = GetKitchenObject();
        if (kitchenObject) {
            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(kitchenObject.GetKitchenObjectSO());
            
            OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
                ProgressNormalized = (float)_cuttingProgress / cuttingRecipeSO.cuttingProgressMax
            });
        }
    }

    private void TestCuttingProgressDone() {
        if (!HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO())) {
            return;
        }
        
        if (IsServerStarted) {
            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());
            if (_cuttingProgress >= cuttingRecipeSO.cuttingProgressMax) {
                KitchenObjectSO outputKitchenObjectSO = GetOutputForInput(GetKitchenObject().GetKitchenObjectSO());
                KitchenObject.DestroyKitchenObject(GetKitchenObject());
                KitchenObject.SpawnKitchenObject(outputKitchenObjectSO, this);
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
