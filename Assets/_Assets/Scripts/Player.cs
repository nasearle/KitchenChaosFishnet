using System;
using FishNet.CodeGenerating;
using FishNet.Connection;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : NetworkBehaviour, IKitchenObjectParent {
    public static event EventHandler OnAnyPlayerSpawned;
    public static event EventHandler OnAnyObjectPickedUp;
    public static void ResetStaticData() {
        OnAnyPlayerSpawned = null;
        OnAnyObjectPickedUp = null;
    }
    
    public static Player LocalInstance { get; private set; }

    public event EventHandler OnObjectPickedUp;
    public event EventHandler<OnSelectedCounterChangedEventArgs> OnSelectedCounterChanged;
    public class OnSelectedCounterChangedEventArgs : EventArgs {
        public BaseCounter SelectedCounter;
    }

    public struct ReplicateData : IReplicateData {
        public Vector2 InputVector;
        public ReplicateData(Vector2 inputVector) : this() {
            InputVector = inputVector;
        }
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }

    public struct ReconcileData : IReconcileData {
        public Vector3 Position;
        public Quaternion Rotation;
        
        public ReconcileData(Vector3 position, Quaternion rotation) : this() {
            Position = position;
            Rotation = rotation;
        }
        
        private uint _tick;
        public void Dispose() { }
        public uint GetTick() => _tick;
        public void SetTick(uint value) => _tick = value;
    }
    
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 10f;
    [SerializeField] private LayerMask countersLayerMask;
    [SerializeField] private LayerMask collisionsLayerMask;
    [SerializeField] private Transform kitchenObjectHoldPoint;
    [SerializeField] private PlayerVisual playerVisual;

    private bool _isWalking;
    private Vector3 _lastInteractDir;
    private BaseCounter _selectedCounter;
    private KitchenObject _kitchenObject;
    
    private void Start() {
        GameInput.Instance.OnInteractAction += GameInputOnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInputOnInteractAlternateAction;

        PlayerData playerData = NetworkConnections.Instance.GetPlayerDataFromClientId(OwnerId);
        playerVisual.SetPlayerColor(NetworkConnections.Instance.GetPlayerColor(playerData.colorId));
    }

    private void GameInputOnInteractAlternateAction(object sender, EventArgs e) {
        if (!GameManager.Instance.IsGamePlaying()) return;
        
        if (_selectedCounter != null) {
            _selectedCounter.InteractAlternate(this);
        }
    }

    private void GameInputOnInteractAction(object sender, EventArgs e) {
        if (!GameManager.Instance.IsGamePlaying()) return;
        
        if (_selectedCounter != null) {
            _selectedCounter.Interact(this);
        }
    }
    
    public override void OnStartNetwork() {
        if (Owner.IsLocalClient) {
            LocalInstance = this;
        }
        
        OnAnyPlayerSpawned?.Invoke(this, EventArgs.Empty);
        
        TimeManager.OnTick += TimeManager_OnTick;
    }
    
    public override void OnStopNetwork() {
        TimeManager.OnTick -= TimeManager_OnTick;
        
        GameInput.Instance.OnInteractAction -= GameInputOnInteractAction;
        GameInput.Instance.OnInteractAlternateAction -= GameInputOnInteractAlternateAction;
    }
    
    private void TimeManager_OnTick() {
        HandleMovementReplicate(CreateReplicateData());
        CreateReconcile();
    }

    private ReplicateData CreateReplicateData() {
        if (!IsOwner) {
            return default;
        }
        
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        return new ReplicateData(inputVector);
    }
    
    private bool PlayerCanMove(Vector3 moveDir) {
        float playerRadius = .7f;
        float playerHeight = 2f;
        float moveDistance = moveSpeed * (float)TimeManager.TickDelta;
        
        return !Physics.BoxCast(transform.position, Vector3.one * playerRadius, moveDir, 
            Quaternion.identity, moveDistance, collisionsLayerMask);
    }
    
    [Replicate]
    private void HandleMovementReplicate(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        Vector3 moveDir = new Vector3(data.InputVector.x, 0, data.InputVector.y);
        bool canMove = PlayerCanMove(moveDir);


        // if (state.ContainsReplayed()) {
        //     // Debug.Log($"Replaying at tick {data.GetTick()}, delta: {(float)TimeManager.TickDelta}, pos: {transform.position}");
        //     Debug.Log($"Owner.ClientId={Owner.ClientId} REPLAY: Tick={data.GetTick()}, LocalTick={NetworkManager.TimeManager.LocalTick}, " +
        //               $"LastRemote={NetworkManager.TimeManager.LastPacketTick.LastRemoteTick}");
        // } else {
        //     Debug.Log($"Owner.ClientId={Owner.ClientId} Normal replicate at tick {data.GetTick()}, delta: {(float)TimeManager.TickDelta}, pos: {transform.position}");
        // }
        
        
        if (!canMove) {
            Vector3 moveDirX = new Vector3(moveDir.x, 0, 0).normalized;
            canMove = (moveDir.x < -.5f || moveDir.x > .5f) && PlayerCanMove(moveDirX);

            if (canMove) {
                moveDir = moveDirX;
            } else {
                Vector3 moveDirZ = new Vector3(0, 0, moveDir.z).normalized;
                canMove = (moveDir.z < -.5f || moveDir.z > .5f) && PlayerCanMove(moveDirZ);

                if (canMove) {
                    moveDir = moveDirZ;
                }
            }
        }

        if (canMove) {
            transform.position += moveDir * (moveSpeed * (float)TimeManager.TickDelta);
        }
        
        transform.forward = Vector3.Slerp(transform.forward, moveDir, rotateSpeed * (float)TimeManager.TickDelta);
        
        _isWalking = moveDir != Vector3.zero;

        // if (Owner.ClientId == 0 && !state.ContainsReplayed()) {
        //     Debug.Log(
        //         $"REPLICATE DATA: Tick={data.GetTick()} Position={transform.position}, Rotation={transform.rotation}");
        // }
    }
    
    public override void CreateReconcile() {
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        ReconcileData data = new ReconcileData(position, rotation);
        ReconcileState(data);
    }
    
    [Reconcile]
    private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable) {
        transform.SetPositionAndRotation(data.Position, data.Rotation);
    }

    private void Update() {
        if (!IsOwner) return;
        // HandleMovement();
        // Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        // HandleMovementServerRpc(inputVector);
        HandleCounterSelection();
    }

    public bool IsWalking() {
        return _isWalking;
    }

    private void HandleCounterSelection() {
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);
        if (moveDir != Vector3.zero) {
            _lastInteractDir = moveDir;
        }
        float interactDistance = 2f;
        if (Physics.Raycast(transform.position, _lastInteractDir, out RaycastHit raycastHit, interactDistance, countersLayerMask)) {
            if (raycastHit.transform.TryGetComponent(out BaseCounter baseCounter)) {
                // Has ClearCounter
                if (baseCounter != _selectedCounter) {
                    SetSelectedCounter(baseCounter);
                }
            } else {
                SetSelectedCounter(null);
            }
        } else {
            SetSelectedCounter(null);
        }
    }

    private void SetSelectedCounter(BaseCounter selectedCounter) {
        this._selectedCounter = selectedCounter;
        OnSelectedCounterChanged?.Invoke(this, new OnSelectedCounterChangedEventArgs {
            SelectedCounter = _selectedCounter
        });
    }

    public Transform GetKitchenObjectFollowTransform() {
        return kitchenObjectHoldPoint;
    }

    public void SetKitchenObject(KitchenObject kitchenObject) {
        _kitchenObject = kitchenObject;

        if (kitchenObject != null) {
            OnObjectPickedUp?.Invoke(this, EventArgs.Empty);
            OnAnyObjectPickedUp?.Invoke(this, EventArgs.Empty);
        }
    }

    public KitchenObject GetKitchenObject() {
        return _kitchenObject;
    }

    public void ClearKitchenObject() {
        _kitchenObject = null;
    }

    public bool HasKitchenObject() {
        return _kitchenObject != null;
    }

    public NetworkObject GetNetworkObject() {
        return NetworkObject;
    }
}
