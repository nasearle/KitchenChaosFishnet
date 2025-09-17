using UnityEngine;

public class MobileManager : MonoBehaviour {
    private void Start() {
#if UNITY_ANDROID || UNITY_IOS
        // Disable screen dimming
        Screen.sleepTimeout = SleepTimeout.NeverSleep;
        Screen.orientation = ScreenOrientation.LandscapeLeft;
#endif
    }
}
