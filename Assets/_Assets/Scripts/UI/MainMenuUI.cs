using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MainMenuUI : MonoBehaviour {
    [SerializeField] private Button playButton;
    [SerializeField] private Button quitButton;
    [SerializeField] private GameObject fullScreenButton;
    [SerializeField] private Sprite magnifyPlusSprite;
    [SerializeField] private Sprite magnifyMinusSprite;

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

    private void Start() {
#if UNITY_WEBGL
        fullScreenButton.SetActive(Application.isMobilePlatform);
        SetFullScreenButtonSprite();
#else
        fullScreenButton.SetActive(false);
#endif
    }

    private void SetFullScreenButtonSprite() {
        Image fullScreenButtonImage = fullScreenButton.GetComponent<Image>();

        if (Screen.fullScreen) {
            fullScreenButtonImage.sprite = magnifyMinusSprite;
        } else {
            fullScreenButtonImage.sprite = magnifyPlusSprite;
        }
    }

    private void FullscreenOnPerformed(InputAction.CallbackContext context) {
        Screen.fullScreen = !Screen.fullScreen;
        
        SetFullScreenButtonSprite();
    }

    private void OnDestroy() {
        _playerInputActions.UI.Fullscreen.performed -= FullscreenOnPerformed;

        _playerInputActions.Disable();
        
        _playerInputActions.Dispose();
    }
}
