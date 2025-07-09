using System;
using UnityEngine;

public class PlayerSounds : MonoBehaviour {
    private Player _player;
    private float _footstepTimer;
    private float _footstepTimerMax = .1f;

    private void Awake() {
        _player = GetComponent<Player>();
    }

    private void Update() {
        _footstepTimer -= Time.deltaTime;
        if (_footstepTimer < 0f) {
            _footstepTimer = _footstepTimerMax;

            if (_player.IsWalking()) {
                float volume = 1f;
                SoundManager.Instance.PlayFootstepSound(_player.transform.position, volume);
            }
        }
    }
}
