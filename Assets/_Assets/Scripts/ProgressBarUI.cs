using System;
using UnityEngine;
using UnityEngine.UI;

public class ProgressBarUI : MonoBehaviour {
    [SerializeField] private GameObject hasProgressGameObject;
    [SerializeField] private Image barImage;
    
    private IHasProgess _hasProgress;

    private void Start() {
        _hasProgress = hasProgressGameObject.GetComponent<IHasProgess>();
        if (_hasProgress == null) {
            Debug.LogError("Game Object " + hasProgressGameObject + " does not have a component that implements IHasProgess");
        }
        _hasProgress.OnProgressChanged += HasProgressOnProgressChanged;
        barImage.fillAmount = 0f;

        Hide();
    }

    private void HasProgressOnProgressChanged(object sender, IHasProgess.OnProgressChangedEventArgs e) {
        barImage.fillAmount = e.ProgressNormalized;

        if (e.ProgressNormalized is 0f or 1f) {
            Hide();
        } else {
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
