using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

public class KitchenGameMatchmaker : MonoBehaviour {
    public const string DEFAULT_QUEUE = "default-queue";
    private const string POOL_TOTAL_PLAYERS_ATTRIBUTE = "TotalPlayers";

    public static KitchenGameMatchmaker Instance { get; private set; }

    public event EventHandler OnFindMatchStarted;
    public event EventHandler OnFindMatchFailed;
    public event EventHandler OnCancelFindMatchStarted;
    public event EventHandler OnCancelFindMatchFailed;
    public event EventHandler OnCancelFindMatchSucceeded;

    private bool _isFindingMatch;
    private bool _isCancellingMatchmaking;

    [Serializable]
    public class MatchmakingPlayerData {
        public int Skill;
        public int ColorId;
    }

    private CreateTicketResponse _createTicketResponse;
    private float _pollTicketTimer;
    private float _pollTicketTimerMax = 1.1f;
    private bool _isPolling;

    private void Awake() {
        Instance = this;
    }

    private void Start() {
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerLeft += KitchenGameLobbyOnJoinedLobbyPlayerLeft;
    }

    private async void KitchenGameLobbyOnJoinedLobbyPlayerLeft(object sender, EventArgs e) {
        if (KitchenGameLobby.Instance.GetLobby() == null ||
            KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            await CancelMatchmaking();
        }
    }

    private async void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        await CancelMatchmaking();
    }

    private void Update() {
        if (_createTicketResponse != null && !_isPolling) {
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

    public async Task CancelMatchmaking() {
        if (_createTicketResponse == null) {
            OnCancelFindMatchSucceeded?.Invoke(this, EventArgs.Empty);
            return;
        }
        
        if (_isCancellingMatchmaking) {
            return;
        }

        _isCancellingMatchmaking = true;

        OnCancelFindMatchStarted?.Invoke(this, EventArgs.Empty);

        try {
            await MatchmakerService.Instance.DeleteTicketAsync(_createTicketResponse.Id);
            _createTicketResponse = null;

            OnCancelFindMatchSucceeded?.Invoke(this, EventArgs.Empty);
        } catch (MatchmakerServiceException e) {
            Debug.LogError($"Failed to delete matchmaking ticket: {e.Message}");

            OnCancelFindMatchFailed?.Invoke(this, EventArgs.Empty);
        } finally {
            _isCancellingMatchmaking = false;
        }        
    }

    public async Task FindMatch(int totalAllowedPlayers) {
        if (_createTicketResponse != null || _isFindingMatch) {
            return;
        }

        _isFindingMatch = true;

        OnFindMatchStarted?.Invoke(this, EventArgs.Empty);

        try {
            _createTicketResponse = await MatchmakerService.Instance.CreateTicketAsync(
                GetMatchmakingPlayers(),
                new CreateTicketOptions(DEFAULT_QUEUE, new Dictionary<string, object> {
                    {POOL_TOTAL_PLAYERS_ATTRIBUTE, totalAllowedPlayers}
                })
            );

            // Wait a bit, don't poll right away
            _pollTicketTimer = _pollTicketTimerMax;
        } catch (MatchmakerServiceException e) {
            Debug.Log(e);
        } finally {
            _isFindingMatch = false;
        }
    }

    private async void PollMatchmakerTicket() {
        if (_isPolling) {
            return;
        }

        _isPolling = true;

        try {
            Debug.Log("PollMatchmakerTicket");
            TicketStatusResponse ticketStatusResponse = await MatchmakerService.Instance.GetTicketAsync(_createTicketResponse.Id);

            if (ticketStatusResponse == null) {
                // Null means no updates to this ticket, keep waiting
                Debug.Log("Null means no updates to this ticket, keep waiting");
                return;
            }

            // Not null means there is an update to the ticket
            if (ticketStatusResponse.Type == typeof(MultiplayAssignment)) {
                MultiplayAssignment multiplayAssignment = ticketStatusResponse.Value as MultiplayAssignment;

                Debug.Log("multiplayAssignment.Status " + multiplayAssignment.Status);
                switch (multiplayAssignment.Status) {
                    case MultiplayAssignment.StatusOptions.Found:
                        _createTicketResponse = null;

                        Debug.Log(multiplayAssignment.Ip + " " + multiplayAssignment.Port);

                        // string ipv4Address = multiplayAssignment.Ip;
                        // ushort port = (ushort)multiplayAssignment.Port;

                        Debug.Log("Querying for lobby with name == MatchId: " + multiplayAssignment.MatchId);
                        QueryResponse queryResponse = await KitchenGameLobby.Instance.QueryForRelayJoinCodeLobby(multiplayAssignment.MatchId);

                        Debug.Log("Found relay join code lobby with id: " + queryResponse.Results.First().Id);
                        Lobby relayJoinCodeLobby = await KitchenGameLobby.Instance.JoinWithId(queryResponse.Results.First().Id);
                        string relayJoinCode = relayJoinCodeLobby.Data[KitchenGameLobby.LobbyDataKeys.RelayJoinCode].Value;
                        Debug.Log("Relay join code: " + relayJoinCode);

                        Debug.Log("Leaving the relay join code lobby");
                        await KitchenGameLobby.Instance.LeaveLobbyWithId(queryResponse.Results.First().Id);

                        // Could fire an event with the data to de-couple the two
                        // scripts, but this is easier for now.
                        if (KitchenGameLobby.Instance.GetLobby() != null) {
                            // Automatically sets the connection data and starts the
                            // connection for all players in the lobby
                            KitchenGameLobby.Instance.SetLobbyMatchFoundDetails(relayJoinCode);
                        } else {
                            await LobbyPlayerConnection.Instance.SetConnectionData(relayJoinCode);
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
        } catch (MatchmakerServiceException ex) {
            Debug.LogError($"Polling failed: {ex.Message}");
        } finally {
            _isPolling = false;
        }
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerLeft -= KitchenGameLobbyOnJoinedLobbyPlayerLeft;
    }
}
