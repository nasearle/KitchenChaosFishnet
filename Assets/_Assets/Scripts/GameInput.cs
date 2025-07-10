using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {
    public static GameInput Instance { get; private set; }
    
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    
    private PlayerInputActions _playerInputActions;
    
    private void Awake() {
        Instance = this;
        
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        
        _playerInputActions.Player.Interact.performed += InteractOnperformed;
        _playerInputActions.Player.InteractAlternate.performed += InteractAlternateOnperformed;
        _playerInputActions.Player.Pause.performed += PauseOnperformed;
    }

    private void OnDestroy() {
        _playerInputActions.Player.Interact.performed -= InteractOnperformed;
        _playerInputActions.Player.InteractAlternate.performed -= InteractAlternateOnperformed;
        _playerInputActions.Player.Pause.performed -= PauseOnperformed;
        
        _playerInputActions.Dispose();
    }

    private void PauseOnperformed(InputAction.CallbackContext obj) {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    private void InteractAlternateOnperformed(InputAction.CallbackContext obj) {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    private void InteractOnperformed(InputAction.CallbackContext obj) {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    public Vector2 GetMovementVectorNormalized() {
        Vector2 inputVector = _playerInputActions.Player.Move.ReadValue<Vector2>();
        inputVector.Normalize();
        return inputVector;
    }
}
