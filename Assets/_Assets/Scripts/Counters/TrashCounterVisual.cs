using System;
using UnityEngine;
using UnityEngine.Serialization;

public class TrashCounterVisual : MonoBehaviour {
    [SerializeField] private TrashCounter trashCounter;
    private Animator _animator;
    
    private int _trashAnimationHash;

    private void Awake() {
        _animator = GetComponent<Animator>();
        _trashAnimationHash = Animator.StringToHash("Trash");
    }

    private void Start() {
        trashCounter.OnPlayerTrashedObject += TrashCounterOnPlayerTrashedObject;
    }

    private void TrashCounterOnPlayerTrashedObject(object sender, EventArgs e) {
        _animator.SetTrigger(_trashAnimationHash);
    }
}
