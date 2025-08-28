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

        if (UnityServices.State != ServicesInitializationState.Initialized) {
            Debug.Log("InitializeUnityAuthentication initializing");            

            InitializationOptions initializationOptions = new InitializationOptions();

#if !UNITY_SERVER && LOCAL_CLIENT_DEV
            initializationOptions.SetProfile(CurrentPlayer.ReadOnlyTags()[0]);
#endif

            await UnityServices.InitializeAsync(initializationOptions);

#if !UNITY_SERVER
            await AuthenticationService.Instance.SignInAnonymouslyAsync();              
#endif
        }

        OnInitialized?.Invoke(this, EventArgs.Empty);
    }
}
