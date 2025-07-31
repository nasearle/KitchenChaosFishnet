using System;
using TMPro;
using UnityEngine;

public class GameStartCountdownUI : MonoBehaviour {
    private const string NUMBER_POPUP = "NumberPopup";
    
    [SerializeField] private TextMeshProUGUI countdownText;
    
    private Animator _animator;
    private int _previousCountdownNumber;

    private void Awake() {
        _animator = GetComponent<Animator>();
    }

    private void Start() {
        GameManager.Instance.OnStateChanged += GameManagerOnStateChanged;

        Hide();
    }

    private void Update() {
        //TODO: annoying visual bug causing the previous number to flash briefly before the new number pops up. Caused 
        // by the syncVar in GameManager not lining up with the animations
        int countdownNumber = Mathf.CeilToInt(GameManager.Instance.GetCountdownToStartTimer());
        countdownText.text = countdownNumber.ToString();

        if (_previousCountdownNumber != countdownNumber) {
            _previousCountdownNumber = countdownNumber;
            _animator.SetTrigger(NUMBER_POPUP);
            SoundManager.Instance.PlayCountdownSound();
        }
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
