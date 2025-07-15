using System;
using UnityEngine;

public class StoveBurnFlashingBarUI : MonoBehaviour {
    private const string IS_FLASHING = "IsFlashing";
    
    [SerializeField] private StoveCounter stoveCounter;
    
    private Animator _animator;

    private void Awake() {
        _animator = GetComponent<Animator>();
    }

    private void Start() {
        stoveCounter.OnProgressChanged += StoveCounterOnProgressChanged;
        
        _animator.SetBool(IS_FLASHING, false);
    }

    private void StoveCounterOnProgressChanged(object sender, IHasProgess.OnProgressChangedEventArgs e) {
        float burnShowProgressAmount = .5f;
        bool show = stoveCounter.IsFried() && e.ProgressNormalized >= burnShowProgressAmount;

        _animator.SetBool(IS_FLASHING, show);
    }
}
