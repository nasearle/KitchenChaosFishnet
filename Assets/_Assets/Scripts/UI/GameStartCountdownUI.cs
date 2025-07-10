using System;
using TMPro;
using UnityEngine;

public class GameStartCountdownUI : MonoBehaviour {
    [SerializeField] private TextMeshProUGUI countdownText;

    private void Start() {
        GameManager.Instance.OnStateChanged += GameManagerOnStateChanged;

        Hide();
    }

    private void Update() {
        countdownText.text = Mathf.Ceil(GameManager.Instance.GetCountdownToStartTimer()).ToString();
    }

    private void GameManagerOnStateChanged(object sender, EventArgs e) {
        if (GameManager.Instance.IsCountdownToStartActive()) {
            Show();
        } else {
            Hide();
        }
    }

    private void Show() {
        gameObject.SetActive(true);
    }

    private void Hide() {
        gameObject.SetActive(false);
    }
}
