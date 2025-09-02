using UnityEngine;

#if UNITY_SERVER
using System;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
#endif

public class KitchenGameDedicatedServer : MonoBehaviour {
#if UNITY_SERVER
    public static KitchenGameDedicatedServer Instance { get; private set; }
    public static IServerQueryHandler serverQueryHandler; // static so it doesn't get destroyed when this object is destroyed
    private bool _alreadyAutoAllocated;
    private bool _serverSetUpComplete;
    private NetworkManager _networkManager;

    private void Awake() {
        Instance = this;

        DontDestroyOnLoad(gameObject);

        _networkManager = InstanceFinder.NetworkManager;

        _networkManager.ServerManager.OnServerConnectionState += ServerManagerOnServerConnectionState;
    }

    private async void ServerManagerOnServerConnectionState(ServerConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Started) {
            Debug.Log("DEDICATED_SERVER LOBBY CONNECTION STARTED");

            if (_serverSetUpComplete) {
                try {
                    await MultiplayService.Instance.ReadyServerForPlayersAsync();
                } catch (Exception e) {
                    Debug.LogError($"Error in ServerManagerOnServerConnectionState: {e}");
                }

                Debug.Log("DEDICATED_SERVER Loading the GameScene...");
                Loader.LoadNetwork(Loader.Scene.GameScene);
            }
        }
    }

    private void Start() {
        if (InitializeUnityGamingServices.Instance.IsInitialized() &&
            InitializeUnityGamingServices.Instance.IsSignedIn()) {
            InitializeUnityGamingServicesOnInitialized(this, EventArgs.Empty);
        } else {
            InitializeUnityGamingServices.Instance.OnInitialized += InitializeUnityGamingServicesOnInitialized;
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

    private async Task<Allocation> AllocateRelay() {
        Debug.Log("DEDICATED_SERVER Allocating relay");
        try {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(KitchenGameLobby.MAX_PLAYER_AMOUNT);
            Debug.Log("DEDICATED_SERVER Allocated relay");
            return allocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);

            return default;
        }
    }

    private async Task<string> GetRelayJoinCode(Allocation allocation) {
        Debug.Log("DEDICATED_SERVER Getting relay join code");
        try {
            string relayJoinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("DEDICATED_SERVER Got relay join code: " + relayJoinCode);
            return relayJoinCode;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
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

        // string ipv4Address = "0.0.0.0";
        // ushort port = serverConfig.Port;

        // Allocate the relay server and get the join code
        Allocation relayAllocation = await AllocateRelay();
        string relayJoinCode = await GetRelayJoinCode(relayAllocation);

        // Get the matchmaking results
        MatchmakingResults matchmakingResults = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();

        Debug.Log($"DEDICATED_SERVER matchmakingResults.MatchId: {matchmakingResults.MatchId}");
        
        // Add the relay join code to a public lobby with name = matchId.
        Debug.Log("DEDICATED_SERVER Creating relay join code lobby with name: " + matchmakingResults.MatchId);
        Debug.Log("DEDICATED_SERVER And adding the relay join code: " + relayJoinCode);
        await KitchenGameLobby.Instance.CreateRelayJoinCodeLobby(matchmakingResults.MatchId, relayJoinCode);

        // Set connection details on the transport
        var unityTransport = _networkManager.TransportManager.GetTransport<UnityTransport>();
        unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(relayAllocation, "wss"));
        unityTransport.UseWebSockets = true;
        
        // Start server
        
        // Debug.Log($"DEDICATED_SERVER setting TransportManager ipv4Address to {ipv4Address}");
        // _networkManager.TransportManager.Transport.SetServerBindAddress(ipv4Address, IPAddressType.IPv4);
        // Debug.Log($"DEDICATED_SERVER setting TransportManager port to {port}");
        // _networkManager.TransportManager.Transport.SetPort(port);

        _serverSetUpComplete = true;

        Debug.Log("DEDICATED_SERVER STARTING CONNECTION");
        _networkManager.ServerManager.StartConnection();
    }

    private void OnDestroy() {
        InitializeUnityGamingServices.Instance.OnInitialized -= InitializeUnityGamingServicesOnInitialized;
        _networkManager.ServerManager.OnServerConnectionState -= ServerManagerOnServerConnectionState;
    }

#endif
}
