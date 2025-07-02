using System;
using UnityEngine;
using UnityEngine.Serialization;

public class CuttingCounterVisual : MonoBehaviour {
    [SerializeField] private CuttingCounter cuttingCounter;
    private Animator _animator;
    
    private int _cutAnimationHash;

    private void Awake() {
        _animator = GetComponent<Animator>();
        _cutAnimationHash = Animator.StringToHash("Cut");
    }

    private void Start() {
        cuttingCounter.OnCut += CuttingCounterOnCut;
    }

    private void CuttingCounterOnCut(object sender, EventArgs e) {
        _animator.SetTrigger(_cutAnimationHash);
    }
}
