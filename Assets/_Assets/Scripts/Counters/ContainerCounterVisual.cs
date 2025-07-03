using System;
using UnityEngine;

public class ContainerCounterVisual : MonoBehaviour {
    [SerializeField] private ContainerCounter containerCounter;
    private Animator _animator;
    
    private int _openCloseAnimationHash;

    private void Awake() {
        _animator = GetComponent<Animator>();
        _openCloseAnimationHash = Animator.StringToHash("OpenClose");
    }

    private void Start() {
        containerCounter.OnPlayerGrabbedObject += ContainerCounterOnPlayerGrabbedObject;
    }

    private void ContainerCounterOnPlayerGrabbedObject(object sender, EventArgs e) {
        _animator.SetTrigger(_openCloseAnimationHash);
    }
}
