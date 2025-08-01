using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour {
    private const string PLAYER_INPUT_BINDINGS = "InputBindings";
    public static GameInput Instance { get; private set; }
    
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternateAction;
    public event EventHandler OnPauseAction;
    public event EventHandler OnBindingRebind;

    public enum Binding {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause,
        Gamepad_Interact,
        Gamepad_InteractAlternate,
        Gamepad_Pause,
    }
    
    private PlayerInputActions _playerInputActions;
    
    private void Awake() {
        Instance = this;
        
        _playerInputActions = new PlayerInputActions();

        if (PlayerPrefs.HasKey(PLAYER_INPUT_BINDINGS)) {
            _playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_INPUT_BINDINGS));
        }
        
        _playerInputActions.Player.Enable();
        _playerInputActions.UI.Disable();
        
        _playerInputActions.Player.Interact.performed += InteractOnperformed;
        _playerInputActions.Player.InteractAlternate.performed += InteractAlternateOnperformed;
        _playerInputActions.Player.Pause.performed += PauseOnperformed;
        
        _playerInputActions.UI.Pause.performed += PauseOnperformed;
        
        GameManager.Instance.OnMultiplayerGamePaused += GameManagerOnMultiplayerGamePaused;
        GameManager.Instance.OnMultiplayerGameResumed += GameManagerOnMultiplayerGameResumed;
    }
    
    private void GameManagerOnMultiplayerGameResumed(object sender, EventArgs e) {
        _playerInputActions.Player.Enable();
        _playerInputActions.UI.Disable();
    }

    private void GameManagerOnMultiplayerGamePaused(object sender, EventArgs e) {
        _playerInputActions.Player.Disable();
        _playerInputActions.UI.Enable();
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

    public string GetBindingText(Binding binding) {
        switch (binding) {
            default:
            case Binding.Move_Up:
                // WASD composite up (index 1)
                return _playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                // WASD composite down (index 2)
                return _playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                // WASD composite left (index 3)
                return _playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                // WASD composite right (index 4)
                return _playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return _playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.InteractAlternate:
                return _playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return _playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            case Binding.Gamepad_Interact:
                return _playerInputActions.Player.Interact.bindings[1].ToDisplayString();
            case Binding.Gamepad_InteractAlternate:
                return _playerInputActions.Player.InteractAlternate.bindings[1].ToDisplayString();
            case Binding.Gamepad_Pause:
                return _playerInputActions.Player.Pause.bindings[1].ToDisplayString();
        }
    }

    public void RebindBinding(Binding binding, Action onActionRebound) {
        _playerInputActions.Player.Disable();

        InputAction inputAction;
        int bindingIndex;

        switch (binding) {
            default:
            case Binding.Move_Up:
                inputAction = _playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = _playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = _playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = _playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = _playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = _playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = _playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            case Binding.Gamepad_Interact:
                inputAction = _playerInputActions.Player.Interact;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_InteractAlternate:
                inputAction = _playerInputActions.Player.InteractAlternate;
                bindingIndex = 1;
                break;
            case Binding.Gamepad_Pause:
                inputAction = _playerInputActions.Player.Pause;
                bindingIndex = 1;
                break;
        }

        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback => {
                callback.Dispose();
                _playerInputActions.Player.Enable();
                onActionRebound();

                PlayerPrefs.SetString(PLAYER_INPUT_BINDINGS, _playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();
                
                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start();
    }
}
