using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class KitchenGameMatchmaker : MonoBehaviour {
    public const string DEFAULT_QUEUE = "default-queue";

    public static KitchenGameMatchmaker Instance { get; private set; }

    public event EventHandler OnFindMatchStarted;
    public event EventHandler OnFindMatchFailed;
    public event EventHandler OnCancelFindMatchStarted;
    public event EventHandler OnCancelFindMatchFailed;
    public event EventHandler OnCancelFindMatchSucceeded;

    [Serializable]
    public class MatchmakingPlayerData {
        public int Skill;
        public int ColorId;
    }

    private CreateTicketResponse _createTicketResponse;
    private float _pollTicketTimer;
    private float _pollTicketTimerMax = 1.1f;

    private void Awake() {
        Instance = this;
    }

    private void Update() {
        if (_createTicketResponse != null) {
            // Has ticket
            _pollTicketTimer -= Time.deltaTime;
            if (_pollTicketTimer <= 0f) {
                _pollTicketTimer = _pollTicketTimerMax;

                PollMatchmakerTicket();
            }
        }
    }

    private List<Unity.Services.Matchmaker.Models.Player> GetMatchmakingPlayers() {
        List<Unity.Services.Matchmaker.Models.Player> players = new List<Unity.Services.Matchmaker.Models.Player> {};

        Lobby lobby = KitchenGameLobby.Instance.GetLobby();

        if (lobby != null) {
            foreach (Unity.Services.Lobbies.Models.Player player in lobby.Players) {
                players.Add(
                    new Unity.Services.Matchmaker.Models.Player(
                        player.Id, 
                        new MatchmakingPlayerData {
                            Skill = 100,
                        }
                    )
                );
            }
        } else {
            players.Add(
                new Unity.Services.Matchmaker.Models.Player(
                    AuthenticationService.Instance.PlayerId, 
                    new MatchmakingPlayerData {
                        Skill = 100,
                    }
                )
            );
        }

        return players;
    }

    public async void CancelMatchmaking() {
        if (_createTicketResponse != null) {
            OnCancelFindMatchStarted?.Invoke(this, EventArgs.Empty);

            try {
                await MatchmakerService.Instance.DeleteTicketAsync(_createTicketResponse.Id);
                _createTicketResponse = null;
                Debug.Log("Matchmaking cancelled successfully.");

                OnCancelFindMatchSucceeded?.Invoke(this, EventArgs.Empty);
            } catch (Exception e) {
                Debug.LogError($"Failed to delete matchmaking ticket: {e.Message}");

                OnCancelFindMatchFailed?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    public async void FindMatch() {
        Debug.Log("FindMatch");

        OnFindMatchStarted?.Invoke(this, EventArgs.Empty);

        _createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(
            GetMatchmakingPlayers(),
            new CreateTicketOptions { QueueName = DEFAULT_QUEUE }
        );

        // Wait a bit, don't poll right away
        _pollTicketTimer = _pollTicketTimerMax;
    }

    private async void PollMatchmakerTicket() {
        Debug.Log("PollMatchmakerTicket");
        TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(_createTicketResponse.Id);

        if (ticketStatusResponse == null) {
            // Null means no updates to this ticket, keep waiting
            Debug.Log("Null means no updates to this ticket, keep waiting");
            return;
        }

        // Not null means there is an update to the ticket
        if (ticketStatusResponse.Type == typeof(MultiplayAssignment)) {
            // It's a Multiplay assignment
            MultiplayAssignment multiplayAssignment = ticketStatusResponse.Value as MultiplayAssignment;

            Debug.Log("multiplayAssignment.Status " + multiplayAssignment.Status);
            switch (multiplayAssignment.Status) {
                case MultiplayAssignment.StatusOptions.Found:
                    _createTicketResponse = null;

                    Debug.Log(multiplayAssignment.Ip + " " + multiplayAssignment.Port);

                    string ipv4Address = multiplayAssignment.Ip;
                    ushort port = (ushort)multiplayAssignment.Port;

                    // Could fire an event with the data to de-couple the two
                    // scripts, but this is easier for now.
                    if (KitchenGameLobby.Instance.GetLobby() != null) {
                        // Automatically sets the connection data and starts the
                        // connection for all players in the lobby
                        KitchenGameLobby.Instance.SetLobbyMatchFoundDetails(multiplayAssignment);
                    } else {
                        LobbyPlayerConnection.Instance.SetConnectionData(ipv4Address, port);
                        LobbyPlayerConnection.Instance.StartClient();
                    }
                    
                    break;
                case MultiplayAssignment.StatusOptions.InProgress:
                    // Still waiting...
                    break;
                case MultiplayAssignment.StatusOptions.Failed:
                    _createTicketResponse = null;
                    Debug.Log("Failed to create Multiplay server!");

                    OnFindMatchFailed?.Invoke(this, EventArgs.Empty);

                    break;
                case MultiplayAssignment.StatusOptions.Timeout:
                    _createTicketResponse = null;
                    Debug.Log("Multiplay Timeout!");

                    OnFindMatchFailed?.Invoke(this, EventArgs.Empty);

                    break;
            }
        }
    }
}
