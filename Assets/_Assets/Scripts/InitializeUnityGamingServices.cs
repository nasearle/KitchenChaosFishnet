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

        if (!IsInitialized()) {
            Debug.Log("InitializeUnityAuthentication initializing");            

            InitializationOptions initializationOptions = new InitializationOptions();

#if !UNITY_SERVER && LOCAL_CLIENT_DEV
            initializationOptions.SetProfile(CurrentPlayer.ReadOnlyTags()[0]);
#endif

            await UnityServices.InitializeAsync(initializationOptions);
        }

        if (!IsSignedIn()) {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        OnInitialized?.Invoke(this, EventArgs.Empty);
    }

    public bool IsInitialized() {
        return UnityServices.State == ServicesInitializationState.Initialized;
    }

    public bool IsSignedIn() {
        return AuthenticationService.Instance.IsSignedIn;
    }
}
