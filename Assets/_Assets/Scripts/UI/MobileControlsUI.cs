using System;
using UnityEngine;
using UnityEngine.UI;

public class MobileControlsUI : MonoBehaviour {
    [SerializeField] private Image fullScreenButtonImage;
    [SerializeField] private Sprite magnifyPlusSprite;
    [SerializeField] private Sprite magnifyMinusSprite;

    private void Start() {
        gameObject.SetActive(Application.isMobilePlatform);

        SetFullScreenButtonSprite();

        GameInput.Instance.OnFullScreen += GameInputOnFullScreen;
    }

    private void SetFullScreenButtonSprite() {
        if (Screen.fullScreen) {
            fullScreenButtonImage.sprite = magnifyMinusSprite;
        } else {
            fullScreenButtonImage.sprite = magnifyPlusSprite;
        }
    }

    private void GameInputOnFullScreen(object sender, EventArgs e) {
        SetFullScreenButtonSprite();       
    }

    private void OnDestroy() {
        GameInput.Instance.OnFullScreen -= GameInputOnFullScreen;
    }
}
