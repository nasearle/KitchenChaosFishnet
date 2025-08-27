using UnityEngine;

public class MainMenuDedicatedServer : MonoBehaviour {


    private void Start() {
#if UNITY_SERVER
        Debug.Log("DEDICATED_SERVER 6.8");
        Loader.Load(Loader.Scene.LobbyScene);
#endif
    }

}