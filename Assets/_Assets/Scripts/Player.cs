using System;
using FishNet.Object;
using FishNet.Object.Prediction;
using FishNet.Transporting;
using UnityEngine;
using UnityEngine.Serialization;

public class Player : NetworkBehaviour, IKitchenObjectParent {
    // public static Player Instance { get; private set; }

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
    [SerializeField] private Transform kitchenObjectHoldPoint;

    private bool _isWalking;
    private Vector3 _lastInteractDir;
    private BaseCounter _selectedCounter;
    private KitchenObject _kitchenObject;

    private bool PlayerCanMove(Vector3 moveDir) {
        float playerRadius = .7f;
        float playerHeight = 2f;
        
        return !Physics.CapsuleCast(transform.position, transform.position + Vector3.up * playerHeight,
            playerRadius, moveDir, moveSpeed * (float)TimeManager.TickDelta);
    }

    private void Awake() {
        // Instance = this;
    }

    private void Start() {
        GameInput.Instance.OnInteractAction += GameInputOnInteractAction;
        GameInput.Instance.OnInteractAlternateAction += GameInputOnInteractAlternateAction;
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
    
    public override void OnStartNetwork()
    {
        TimeManager.OnTick += TimeManager_OnTick;
        TimeManager.OnPostTick += TimeManager_OnPostTick;
    }
    
    public override void OnStopNetwork()
    {
        TimeManager.OnTick -= TimeManager_OnTick;
        TimeManager.OnPostTick -= TimeManager_OnPostTick;
    }
    
    private void TimeManager_OnTick() {
        HandleMovementReplicate(CreateReplicateData());
    }

    private ReplicateData CreateReplicateData() {
        if (!IsOwner) {
            return default;
        }
        
        Vector2 inputVector = GameInput.Instance.GetMovementVectorNormalized();
        return new ReplicateData(inputVector);
    }

    
    private void TimeManager_OnPostTick() {
        CreateReconcile();
    }
    
    public override void CreateReconcile()
    {
        transform.GetPositionAndRotation(out Vector3 position, out Quaternion rotation);
        ReconcileData data = new ReconcileData(position, rotation);
        ReconcileState(data);
    }
    
    [Reconcile]
    private void ReconcileState(ReconcileData data, Channel channel = Channel.Unreliable)
    {
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

    [Replicate]
    private void HandleMovementReplicate(ReplicateData data, ReplicateState state = ReplicateState.Invalid, Channel channel = Channel.Unreliable) {
        HandleMovement(data.InputVector);
    }

    private void HandleMovement(Vector2 inputVector) {
        Vector3 moveDir = new Vector3(inputVector.x, 0, inputVector.y);
        bool canMove = PlayerCanMove(moveDir);

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
}
