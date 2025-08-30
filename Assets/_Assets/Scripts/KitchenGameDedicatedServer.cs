using System;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;

public class KitchenGameDedicatedServer : MonoBehaviour {
#if UNITY_SERVER
    public static KitchenGameDedicatedServer Instance { get; private set; }
    public static IServerQueryHandler serverQueryHandler; // static so it doesn't get destroyed when this object is destroyed
    private float _autoAllocateTimer = 9999999f;
    private bool _alreadyAutoAllocated;
    private bool _addressAndPortSet;
    private NetworkManager _networkManager;

    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _networkManager = InstanceFinder.NetworkManager;

        _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;
    }

    private void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            Debug.Log("DEDICATED_SERVER LOBBY CONNECTION STARTED");

            if (_addressAndPortSet) {
                Debug.Log("DEDICATED_SERVER Loading the GameScene...");
                Loader.LoadNetwork(Loader.Scene.GameScene);
            }
        }
    }

    private void Start() {
        if (UnityServices.State == ServicesInitializationState.Initialized) {
            InitializeUnityGamingServicesOnInitialized(this, EventArgs.Empty);
        } else {
            InitializeUnityGamingServices.Instance.OnInitialized += InitializeUnityGamingServicesOnInitialized;
        }
    }

    private void Update() {
        // TODO: remove this
        _autoAllocateTimer -= Time.deltaTime;
        if (_autoAllocateTimer <= 0f) {
            _autoAllocateTimer = 999f;
            MultiplayEventCallbacks_Allocate(null);
        }
    }

    private async void InitializeUnityGamingServicesOnInitialized(object sender, EventArgs e) {
        Debug.Log("DEDICATED_SERVER LOBBY");

        MultiplayEventCallbacks multiplayEventCallbacks = new MultiplayEventCallbacks();
        multiplayEventCallbacks.Allocate += MultiplayEventCallbacks_Allocate;
        multiplayEventCallbacks.Deallocate += MultiplayEventCallbacks_Deallocate;
        multiplayEventCallbacks.Error += MultiplayEventCallbacks_Error;
        multiplayEventCallbacks.SubscriptionStateChanged += MultiplayEventCallbacks_SubscriptionStateChanged;
        IServerEvents serverEvents = await MultiplayService.Instance.SubscribeToServerEventsAsync(multiplayEventCallbacks);

        serverQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(4, "MyServerName", "KitchenChaos", "5.6", "Default");

        var serverConfig = MultiplayService.Instance.ServerConfig;
        if (serverConfig.AllocationId != "") {
            // Already Allocated
            MultiplayEventCallbacks_Allocate(new MultiplayAllocation("", serverConfig.ServerId, serverConfig.AllocationId));
        }
    }

    private void MultiplayEventCallbacks_SubscriptionStateChanged(MultiplayServerSubscriptionState obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_SubscriptionStateChanged");
        Debug.Log(obj);
    }

    private void MultiplayEventCallbacks_Error(MultiplayError obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Error");
        Debug.Log(obj.Reason);
    }

    private void MultiplayEventCallbacks_Deallocate(MultiplayDeallocation obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Deallocate");
    }

    private async void MultiplayEventCallbacks_Allocate(MultiplayAllocation obj) {
        Debug.Log("DEDICATED_SERVER MultiplayEventCallbacks_Allocate");

        if (_alreadyAutoAllocated) {
            Debug.Log("Already auto allocated!");
            return;
        }

        _alreadyAutoAllocated = true;

        var serverConfig = MultiplayService.Instance.ServerConfig;
        Debug.Log($"Server ID[{serverConfig.ServerId}]");
        Debug.Log($"AllocationID[{serverConfig.AllocationId}]");
        Debug.Log($"Port[{serverConfig.Port}]");
        Debug.Log($"QueryPort[{serverConfig.QueryPort}]");
        Debug.Log($"LogDirectory[{serverConfig.ServerLogDirectory}]");

        string ipv4Address = "0.0.0.0";
        ushort port = serverConfig.Port;
        
        Debug.Log($"DEDICATED_SERVER setting TransportManager ipv4Address to {ipv4Address}");
        _networkManager.TransportManager.Transport.SetServerBindAddress(ipv4Address, IPAddressType.IPv4);
        Debug.Log($"DEDICATED_SERVER setting TransportManager port to {port}");
        _networkManager.TransportManager.Transport.SetPort(port);

        _addressAndPortSet = true;

        Debug.Log("DEDICATED_SERVER STARTING CONNECTION MANUALLY");
        _networkManager.ServerManager.StartConnection();

        try {
            await MultiplayService.Instance.ReadyServerForPlayersAsync();
        } catch (Exception e) {
            Debug.LogError($"Error in MultiplayEventCallbacks_Allocate: {e}");
        }
    }

    private void OnDestroy() {
        InitializeUnityGamingServices.Instance.OnInitialized -= InitializeUnityGamingServicesOnInitialized;
        _networkManager.ServerManager.OnServerConnectionState -= ServerManagerOnServerConnectionState;
    }

#endif
}
