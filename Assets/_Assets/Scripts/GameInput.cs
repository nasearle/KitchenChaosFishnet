using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    
    private PlayerInputActions _playerInputActions;
    
    private void Awake() {
        _playerInputActions = new PlayerInputActions();
        _playerInputActions.Player.Enable();
        
        _playerInputActions.Player.Interact.performed += InteractOnperformed;
        _playerInputActions.Player.InteractAlternate.performed += InteractAlternateOnperformed;
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
