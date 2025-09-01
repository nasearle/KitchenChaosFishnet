using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayer : MonoBehaviour {
    [SerializeField] private int playerIndex;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button makeHostButton;
    [SerializeField] private Image isHostImage;
    [SerializeField] private TextMeshPro playerNameText;

    private void Start() {
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyAnyChange += KitchenGameLobbyOnJoinedLobbyAnyChange;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerStatusChanged += KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerDataChanged += KitchenGameLobbyOnPlayerDataChanged;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange += KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;

        kickButton.onClick.AddListener(() => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.Id);
        });

        leaveButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
        });

        makeHostButton.onClick.AddListener(async () => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerIndex(playerIndex);
            // Make this player the host
            await KitchenGameLobby.Instance.SetLobbyHost(playerData.Id);
        });

        UpdatePlayerLobbyUI();
        UpdateLocalPlayer();
        UpdateLobbyPlayer();
    }

    private void KitchenGameLobbyOnJoinedLobbyTopLevelDataChange(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        string matchmakingStatus = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.MatchmakingStatus);
        if (matchmakingStatus == KitchenGameLobby.MatchmakingStatus.Waiting.ToString()) {
            makeHostButton.interactable = true;
        } else {
            makeHostButton.interactable = false;
        }
    }

    private void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
        UpdateLocalPlayer();
    }

    private void KitchenGameLobbyOnPlayerDataChanged(object sender, EventArgs e) {
        UpdateLocalPlayer();
    }

    private void KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
    }

    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
        UpdateLocalPlayer();
        UpdateLobbyPlayer();
    }

    private void KitchenGameLobbyOnJoinedLobbyAnyChange(object sender, EventArgs e) {
        UpdateLocalPlayer();
        UpdateLobbyPlayer();
    }

    private void UpdatePlayerLobbyUI() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby == null) {
            makeHostButton.gameObject.SetActive(false);
            isHostImage.gameObject.SetActive(false);
            kickButton.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(false);
            return;
        }

        if (KitchenGameLobby.Instance.GetLobby().Players.Count > 1 && KitchenGameLobby.Instance.IsPlayerIndexConnected(playerIndex)) {
            // enable UI
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerIndex(playerIndex);
            
            if (KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
                // The local player is the lobby host
                if (KitchenGameLobby.Instance.LobbyPlayerIsLocalPlayer(playerData)) {
                    // This player avatar is the local player, so
                    // enable the host image and disable the makeHostButton
                    isHostImage.gameObject.SetActive(true);
                    makeHostButton.gameObject.SetActive(false);
                } else {
                    // This player avatar is not the local host player, so
                    // enable the makeHostButton and disable the host image
                    isHostImage.gameObject.SetActive(false);
                    makeHostButton.gameObject.SetActive(true);
                }
            } else {
                // The local player is not the lobby host
                if (KitchenGameLobby.Instance.IsPlayerLobbyHost(playerData)) {
                    // This is the player avatar for the lobby host, so
                    // enable the host image
                    isHostImage.gameObject.SetActive(true);
                } else {
                    // This is not the player avatar for the lobby host, so
                    // disable the host image
                    isHostImage.gameObject.SetActive(false);
                }
                // Can't make other players the host if this player not the host
                makeHostButton.gameObject.SetActive(false);
            }
            
            
            if (KitchenGameLobby.Instance.LobbyPlayerIsLocalPlayer(playerData)) {
                kickButton.gameObject.SetActive(false);
                leaveButton.gameObject.SetActive(true);
            } else if (KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
                kickButton.gameObject.SetActive(true);
                leaveButton.gameObject.SetActive(false);
            } else {
                kickButton.gameObject.SetActive(false);
                leaveButton.gameObject.SetActive(false);
            }
        } else {
            // disable UI
            makeHostButton.gameObject.SetActive(false);
            isHostImage.gameObject.SetActive(false);
            kickButton.gameObject.SetActive(false);
            leaveButton.gameObject.SetActive(false);
        }
    }
    
    private void UpdateLocalPlayer() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            if (KitchenGameLobby.Instance.IsPlayerIndexConnected(playerIndex)) {
                var lobbyPlayer = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerIndex(playerIndex);
                if (KitchenGameLobby.Instance.LobbyPlayerIsLocalPlayer(lobbyPlayer)) {
                    Show();

                    PlayerData playerData = KitchenGameLobby.Instance.GetPlayerData();
                    playerNameText.text = playerData.playerName;
                    playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(playerData.colorId));
                }
            }
        } else {
            if (playerIndex == 0) {
                Show();

                PlayerData playerData = KitchenGameLobby.Instance.GetPlayerData();
                playerNameText.text = playerData.playerName;
                playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(playerData.colorId));
            } else {
                Hide();
            }
        }
    }

    private void UpdateLobbyPlayer() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            if (KitchenGameLobby.Instance.IsPlayerIndexConnected(playerIndex)) {
                Unity.Services.Lobbies.Models.Player lobbyPlayer = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerIndex(playerIndex);

                if (!KitchenGameLobby.Instance.LobbyPlayerIsLocalPlayer(lobbyPlayer)) {
                    Show();

                    playerNameText.text = LobbyPlayerDataConverter.GetPlayerDataValue(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.PlayerName);

                    int colorId = LobbyPlayerDataConverter.GetPlayerDataValue<int>(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.ColorId);

                    playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId));
                }
            } else {
                Hide();
            }
        }
        
    }

    public void Show() {
        gameObject.SetActive(true);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyAnyChange -= KitchenGameLobbyOnJoinedLobbyAnyChange;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerStatusChanged -= KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerDataChanged -= KitchenGameLobbyOnPlayerDataChanged;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange -= KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
    }
}
