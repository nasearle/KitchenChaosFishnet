using System;
using UnityEngine;

public class WaitingForOtherPlayersUI : MonoBehaviour {
    private void Start() {
        GameManager.Instance.OnLocalPlayerReadyChanged += GameManagerOnLocalPlayerReadyChanged;
        GameManager.Instance.OnStateChanged += GameManagerOnStateChanged;
        
        Hide();
    }

    private void GameManagerOnStateChanged(object sender, EventArgs e) {
        if (GameManager.Instance.IsCountdownToStartActive()) {
            Hide();
        }
    }

    private void GameManagerOnLocalPlayerReadyChanged(object sender, EventArgs e) {
        if (GameManager.Instance.IsLocalPlayerReady()) {
            Show();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
