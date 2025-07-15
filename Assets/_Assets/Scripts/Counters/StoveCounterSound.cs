using System;
using UnityEngine;

public class StoveCounterSound : MonoBehaviour {
    [SerializeField] private StoveCounter stoveCounter;
    
    private AudioSource audioSource;
    private float _warningSoundTimer;
    private bool _playWarningSound;

    private void Awake() {
        audioSource = GetComponent<AudioSource>();
    }
    
    private void Start() {
        stoveCounter.OnStateChanged += StoveCounterOnStateChanged;
        stoveCounter.OnProgressChanged += StoveCounterOnProgressChanged;
    }

    private void StoveCounterOnProgressChanged(object sender, IHasProgess.OnProgressChangedEventArgs e) {
        float burnShowProgressAmount = .5f;
        _playWarningSound = stoveCounter.IsFried() && e.ProgressNormalized >= burnShowProgressAmount;
    }

    private void StoveCounterOnStateChanged(object sender, StoveCounter.OnStateChangedEventArgs e) {
        bool playSound = e.State == StoveCounter.State.Frying || e.State == StoveCounter.State.Fried;
        if (playSound) {
            if (!audioSource.isPlaying) {
                audioSource.Play();
            }
        } else {
            audioSource.Pause();
        }
    }

    private void Update() {
        if (_playWarningSound) {
            _warningSoundTimer -= Time.deltaTime;
            if (_warningSoundTimer <= 0f) {
                float warningSoundTimerMax = .2f;
                _warningSoundTimer = warningSoundTimerMax;
                
                SoundManager.Instance.PlayWarningSound(stoveCounter.transform.position);
            }
        }
    }
}
