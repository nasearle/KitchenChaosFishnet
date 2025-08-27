using System;
using Unity.Multiplayer.Playmode;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

public class InitializeUnityGamingServices : MonoBehaviour {
    public static InitializeUnityGamingServices Instance { get; private set; }
    public event EventHandler OnInitialized;

    private void Awake() {
        Instance = this;

        InitializeUnityAuthentication();
    }

    private async void InitializeUnityAuthentication() {
        Debug.Log("InitializeUnityAuthentication");
        // TODO: remove this for production
        bool dedicatedServer = CurrentPlayer.ReadOnlyTags().Length > 0 && CurrentPlayer.ReadOnlyTags()[0] == "server";

        if (UnityServices.State != ServicesInitializationState.Initialized) {
            Debug.Log("InitializeUnityAuthentication initializing");            

            InitializationOptions initializationOptions = new InitializationOptions();

            if (!dedicatedServer) {
                initializationOptions.SetProfile(UnityEngine.Random.Range(0, 1000).ToString());
            }

            await UnityServices.InitializeAsync(initializationOptions);

            if (!dedicatedServer) {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();              
            }
        }

        OnInitialized?.Invoke(this, EventArgs.Empty);
    }
}
