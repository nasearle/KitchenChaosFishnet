using System;
using System.Threading.Tasks;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Transporting.UTP;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class LobbyPlayerConnection : MonoBehaviour {
    public static LobbyPlayerConnection Instance { get; private set; }

    public event EventHandler OnTryingToJoinGame;
    public event EventHandler OnFailedToJoinGame;

    private NetworkManager _networkManager;

    private void Awake() {
        Instance = this;
        
        _networkManager = InstanceFinder.NetworkManager;
    }

    private void Start() {
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange += KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
    }

    private async void KitchenGameLobbyOnJoinedLobbyTopLevelDataChange(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby == null) {
            return;
        }

        string matchmakingStatus = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.MatchmakingStatus);

        if (matchmakingStatus == KitchenGameLobby.MatchmakingStatus.MatchFound.ToString()) {
            string relayJoinCode = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.RelayJoinCode);
            // string ipv4Address = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.ServerIp);
            // ushort port = LobbyPlayerDataConverter.GetLobbyDataValue<ushort>(joinedLobby, KitchenGameLobby.LobbyDataKeys.ServerPort);
            
            await SetConnectionData(relayJoinCode);
            StartClient();
        }
    }

    private async Task<JoinAllocation> JoinRelay(string joinCode) {
        try {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            return joinAllocation;
        } catch (RelayServiceException e) {
            Debug.Log(e);
            return default;
        }
    }

    public async Task SetConnectionData(string relayJoinCode) {
        JoinAllocation joinAllocation = await JoinRelay(relayJoinCode);

        var unityTransport = _networkManager.TransportManager.GetTransport<UnityTransport>();
        unityTransport.SetRelayServerData(AllocationUtils.ToRelayServerData(joinAllocation, "wss"));
        unityTransport.UseWebSockets = true;

        // _networkManager.TransportManager.Transport.SetClientAddress(ipv4Address);
        // _networkManager.TransportManager.Transport.SetPort(port);
    }

    public void StartClient() {
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);
        
        _networkManager.ClientManager.OnClientConnectionState += ClientManagerOnClientConnectionState;
        _networkManager.ClientManager.StartConnection();
    }

    private void ClientManagerOnClientConnectionState(ClientConnectionStateArgs stateArgs) {
        if (stateArgs.ConnectionState == LocalConnectionState.Stopped) {
            OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OnDestroy() {
        _networkManager.ClientManager.OnClientConnectionState -= ClientManagerOnClientConnectionState;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange -= KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
    }
}
