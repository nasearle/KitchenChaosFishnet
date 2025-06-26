using System;
using UnityEngine;

public class PlayerAnimator : MonoBehaviour {
    [SerializeField] private Player player;
    private Animator _animator;
    private int _walkingStateHash;

    private void Awake() {
        _animator = GetComponent<Animator>();
        _walkingStateHash = Animator.StringToHash("IsWalking");
    }

    private void Update() {
        _animator.SetBool(_walkingStateHash, player.IsWalking());
    }
}
