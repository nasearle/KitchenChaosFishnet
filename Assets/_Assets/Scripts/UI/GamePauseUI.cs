using System;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour {
    
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsButton;

    private void Awake() {
        resumeButton.onClick.AddListener(() => {
            GameManager.Instance.TogglePauseGame();
        });
        mainMenuButton.onClick.AddListener(() => {
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        optionsButton.onClick.AddListener(() => {
            Hide();
            OptionsUI.Instance.Show(Show);
        });
    }

    private void Start() {
        GameManager.Instance.OnLocalGamePaused += GameManagerOnLocalGamePaused;
        GameManager.Instance.OnLocalGameResumed += GameManagerOnLocalGameResumed;
        
        Hide();
    }

    private void GameManagerOnLocalGameResumed(object sender, EventArgs e) {
        Hide();
    }
    
    private void GameManagerOnLocalGamePaused(object sender, EventArgs e) {
        Show();
    }

    private void Show() {
        gameObject.SetActive(true);
        
        resumeButton.Select();
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
