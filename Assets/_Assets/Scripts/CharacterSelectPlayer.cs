using UnityEngine;

public class CharacterSelectPlayer : MonoBehaviour {
    
    public void Show() {
        gameObject.SetActive(true);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }
}
