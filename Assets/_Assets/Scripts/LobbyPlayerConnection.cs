using System;
using FishNet;
using FishNet.Managing;
using FishNet.Transporting;
using Unity.Services.Lobbies.Models;
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

    private void KitchenGameLobbyOnJoinedLobbyTopLevelDataChange(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby == null) {
            return;
        }

        string matchmakingStatus = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.MatchmakingStatus);

        if (matchmakingStatus == KitchenGameLobby.MatchmakingStatus.MatchFound.ToString()) {
            string ipv4Address = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.ServerIp);
            ushort port = LobbyPlayerDataConverter.GetLobbyDataValue<ushort>(joinedLobby, KitchenGameLobby.LobbyDataKeys.ServerPort);
            
            SetConnectionData(ipv4Address, port);
            StartClient();
        }
    }

    public void SetConnectionData(string ipv4Address, ushort port) {
        _networkManager.TransportManager.Transport.SetClientAddress(ipv4Address);
        _networkManager.TransportManager.Transport.SetPort(port);
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
