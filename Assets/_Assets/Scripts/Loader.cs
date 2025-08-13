using FishNet;
using FishNet.Connection;
using FishNet.Managing.Scened;
using SceneManager = UnityEngine.SceneManagement.SceneManager;

public static class Loader {
    public enum Scene {
        MainMenuScene,
        GameScene,
        LoadingScene,
        LobbyScene,
        CharacterSelectScene,
        ManagersLoadingScene,
    }
    
    private static Scene _targetScene;

    public static void Load(Scene targetScene) {
        _targetScene = targetScene;
        SceneManager.LoadScene(Scene.LoadingScene.ToString());
    }

    public static void LoadNetwork(Scene targetScene) {
        SceneLoadData sld = new SceneLoadData(targetScene.ToString());
        sld.ReplaceScenes = ReplaceOption.All;
        InstanceFinder.SceneManager.LoadGlobalScenes(sld);
    }

    public static void LoadSingleClientNetwork(Scene targetScene, NetworkConnection conn) {       
        SceneLoadData sld = new SceneLoadData(targetScene.ToString());
        InstanceFinder.SceneManager.LoadConnectionScenes(conn, sld);
    }

    public static void LoaderCallback() {
        SceneManager.LoadScene(_targetScene.ToString());
    }
}
