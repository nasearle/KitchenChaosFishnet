using System;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyPlayerVisualManager : MonoBehaviour {
    [SerializeField] private LobbyPlayerVisual localLobbyPlayerVisual;
    [SerializeField] private LobbyPlayerVisual[] otherLobbyPlayerVisuals;


    private void Start() {
        if (UnityServices.State == ServicesInitializationState.Initialized) {
            InitializeLobbyPlayerVisual(localLobbyPlayerVisual, AuthenticationService.Instance.PlayerId, true);
        } else {
            InitializeLobbyPlayerVisual(localLobbyPlayerVisual, null, true);
            KitchenGameLobby.Instance.OnUnityGamingServicesInitialized += KitchenGameLobbyOnUnityGamingServicesInitialized;
        }
        
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerJoined += KitchenGameLobbyOnJoinedLobbyPlayerJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerLeft += KitchenGameLobbyOnJoinedLobbyPlayerLeft;
    }

    private void KitchenGameLobbyOnJoinedLobbyPlayerLeft(object sender, EventArgs e) {
        foreach (LobbyPlayerVisual playerVisual in otherLobbyPlayerVisuals) {
            if (!KitchenGameLobby.Instance.IsPlayerIdInLobby(playerVisual.GetPlayerId())) {
                ResetLobbyPlayerVisual(playerVisual);
            }
        }
    }

    private void KitchenGameLobbyOnJoinedLobbyPlayerJoined(object sender, KitchenGameLobby.OnJoinedLobbyPlayerJoinedEventArgs e) {
        foreach (var joinedPlayer in e.JoinedPlayers) {
            LobbyPlayerVisual lobbyPlayerVisual = GetFirstAvailableLobbyPlayerVisual();

            if (lobbyPlayerVisual != null) {
                InitializeLobbyPlayerVisual(lobbyPlayerVisual, joinedPlayer.Player.Id, false);
            }
        }
    }

    private void KitchenGameLobbyOnUnityGamingServicesInitialized(object sender, EventArgs e) {
        localLobbyPlayerVisual.SetPlayerId(AuthenticationService.Instance.PlayerId);
    }

    private void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        localLobbyPlayerVisual.UpdateLobbyPlayerVisualUI(null);
        ResetAllOtherLobbyPlayerVisuals();
    }

    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        localLobbyPlayerVisual.UpdateLobbyPlayerVisualUI(joinedLobby.HostId);
        InitializeAllOtherLobbyPlayerVisuals();
    }

    private void ResetLobbyPlayerVisual(LobbyPlayerVisual lobbyPlayerVisual) {
        lobbyPlayerVisual.SetPlayerId(null);
        lobbyPlayerVisual.Hide();
    }

    private void InitializeLobbyPlayerVisual(LobbyPlayerVisual lobbyPlayerVisual, string playerId, bool isLocalPlayer) {
        lobbyPlayerVisual.InitializePlayerVisual(playerId, isLocalPlayer);
    }

    private void ResetAllOtherLobbyPlayerVisuals() {
        foreach (var lobbyPlayerVisual in otherLobbyPlayerVisuals) {
            ResetLobbyPlayerVisual(lobbyPlayerVisual);
        }
    }

    private void InitializeAllOtherLobbyPlayerVisuals() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();

        int lobbyPlayersIndex = 0;

        foreach (var player in joinedLobby.Players) {
            if (!KitchenGameLobby.Instance.LobbyPlayerIsLocalPlayer(player)) { 
                LobbyPlayerVisual lobbyPlayerVisual = otherLobbyPlayerVisuals[lobbyPlayersIndex];
                
                InitializeLobbyPlayerVisual(lobbyPlayerVisual, player.Id, false);

                lobbyPlayersIndex++;
            }
        }
    }

    private LobbyPlayerVisual GetFirstAvailableLobbyPlayerVisual() {
        foreach (var lobbyPlayerVisual in otherLobbyPlayerVisuals) {
            if (!lobbyPlayerVisual.isActiveAndEnabled) {
                return lobbyPlayerVisual;
            }
        }
        return default;
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnUnityGamingServicesInitialized -= KitchenGameLobbyOnUnityGamingServicesInitialized;
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerJoined -= KitchenGameLobbyOnJoinedLobbyPlayerJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerLeft -= KitchenGameLobbyOnJoinedLobbyPlayerLeft;
    }
}
