using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;

    private PlayerInputActions _playerInputActions;

    private void Awake() {
        playButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.LobbyScene);
        });
        
        quitButton.onClick.AddListener(() => {
            Application.Quit();
        });

        Time.timeScale = 1f;


        _playerInputActions = new PlayerInputActions();
        _playerInputActions.UI.Enable();
        _playerInputActions.UI.Fullscreen.performed += FullscreenOnPerformed;
    
    }

    private void FullscreenOnPerformed(InputAction.CallbackContext context) {
        Debug.Log("FullscreenOnPerformed");
        Screen.fullScreen = !Screen.fullScreen;
        Debug.Log(Screen.fullScreen);
    }

    private void OnDestroy() {
        _playerInputActions.UI.Fullscreen.performed -= FullscreenOnPerformed;

        _playerInputActions.Disable();
        
        _playerInputActions.Dispose();
    }
}
