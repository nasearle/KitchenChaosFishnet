using System;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

public class StoveCounter : BaseCounter, IHasProgess {
    public event EventHandler<IHasProgess.OnProgressChangedEventArgs> OnProgressChanged;
    public event EventHandler<OnStateChangedEventArgs> OnStateChanged;
    public class OnStateChangedEventArgs : EventArgs {
        public State State;
    }
    
    public enum State {
        Idle,
        Frying,
        Fried,
        Burned,
    }
    
    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;

    private readonly SyncVar<State> _state = new SyncVar<State>(State.Idle);
    private readonly SyncVar<float> _fryingTimer = new SyncVar<float>(0f);
    private FryingRecipeSO _fryingRecipeSO;
    private readonly SyncVar<float> _burningTimer = new SyncVar<float>(0f);
    private BurningRecipeSO _burningRecipeSO;

    public override void OnStartNetwork() {
        _fryingTimer.OnChange += FryingTimerOnChange;
        _burningTimer.OnChange += BurningTimerOnChange;
        _state.OnChange += StateOnChange;
    }

    private void StateOnChange(State prev, State next, bool asServer) {
        OnStateChanged?.Invoke(this, new OnStateChangedEventArgs {
            State = _state.Value
        });

        if (_state.Value == State.Burned || _state.Value == State.Idle) {
            OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
                ProgressNormalized = 0f
            });
        }
    }

    private void BurningTimerOnChange(float prev, float next, bool asserver) {
        // TODO: Bug where this shows a full burning bar with the warning flashing for an instant before reverting to the 
        // normal progress bar
        float burningTimerMax = _burningRecipeSO != null ? _burningRecipeSO.burningTimerMax : 1f;
        
        OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
            ProgressNormalized = _burningTimer.Value / burningTimerMax
        });
    }

    private void FryingTimerOnChange(float prev, float next, bool asServer) {
        float fryingTimerMax = _fryingRecipeSO != null ? _fryingRecipeSO.fryingTimerMax : 1f;
        
        OnProgressChanged?.Invoke(this, new IHasProgess.OnProgressChangedEventArgs {
            ProgressNormalized = _fryingTimer.Value / fryingTimerMax
        });
    }

    private void Update() {
        if (!IsServerStarted) {
            return;
        }
        
        if (HasKitchenObject()) {
            switch (_state.Value) {
                case State.Idle:
                    break;
                case State.Frying:
                    _fryingTimer.Value += Time.deltaTime;
                    
                    
                    if (_fryingTimer.Value > _fryingRecipeSO.fryingTimerMax) {
                        // Fried
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(_fryingRecipeSO.output, this);
                        
                        _state.Value = State.Fried;
                        
                        // Bug here where the burning timer on change is triggered, but the burning recipe isn't set yet
                        // see TODO in BurningTimerOnChange.
                        _burningTimer.Value = 0f;
                        SetBurningRecipeSOClientRpc(
                            KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO())
                        );
                    }
                    break;
                case State.Fried:
                    _burningTimer.Value += Time.deltaTime;
                    
                    if (_burningTimer.Value > _burningRecipeSO.burningTimerMax) {
                        // Burned
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(_burningRecipeSO.output, this);
                        
                        _state.Value = State.Burned;
                    }
                    break;
                case State.Burned:
                    break;
            }
        }
    }

    public override void Interact(Player player) {
        if (!HasKitchenObject()) {
            if (player.HasKitchenObject()) {
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO())) {
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    kitchenObject.SetKitchenObjectParent(this);

                    PlaceObjectOnCounterServerRpc(
                        KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO())
                    );
                }
            }
        } else {
            // There is a kitchen object here.
            if (player.HasKitchenObject()) {
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject)) {
                    // Player is holding a plate.
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO())) {
                        GetKitchenObject().DestroySelf();
                        
                        _state.Value = State.Idle;
                    }
                }
            } else {
                GetKitchenObject().SetKitchenObjectParent(player);
                
                // TODO: Bug where when we pick up the object and set the state to idle, it sends a one-time event to
                // hide the progress bar, but the latest frying timer change might arrive right after, unhiding the
                // progress bar.
                SetStateIdleServerRpc();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetStateIdleServerRpc() {
        _state.Value = State.Idle;
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlaceObjectOnCounterServerRpc(int kitchenObjectSOIndex) {
        _fryingTimer.Value = 0f;
        _state.Value = State.Frying;
        SetFryingRecipeSOClientRpc(kitchenObjectSOIndex);
    }
    
    [ObserversRpc(RunLocally = true)]
    private void SetFryingRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        _fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }
    
    [ObserversRpc(RunLocally = true)]
    private void SetBurningRecipeSOClientRpc(int kitchenObjectSOIndex) {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);
        _burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
    }
    
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO) {
        return GetFryingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO) {
        FryingRecipeSO fryingRecipeSO = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        if (fryingRecipeSO != null) {
            return fryingRecipeSO.output;
        }
        return null;
    }

    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (FryingRecipeSO fryingRecipeSO in fryingRecipeSOArray) {
            if (fryingRecipeSO.input == inputKitchenObjectSO) {
                return fryingRecipeSO;
            }
        }
        return null;
    }
    
    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO) {
        foreach (BurningRecipeSO burningRecipeSO in burningRecipeSOArray) {
            if (burningRecipeSO.input == inputKitchenObjectSO) {
                return burningRecipeSO;
            }
        }
        return null;
    }

    public bool IsFried() {
        return _state.Value == State.Fried;
    }
}
