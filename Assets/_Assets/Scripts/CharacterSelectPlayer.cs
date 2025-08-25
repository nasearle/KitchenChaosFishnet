using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPlayer : MonoBehaviour {
    [SerializeField] private int playerIndex;
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button makeHostButton;
    [SerializeField] private Image isHostImage;
    [SerializeField] private TextMeshPro playerNameText;

    private void Start() {
        KitchenGameLobby.Instance.OnLobbyJoined += KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated += KitchenGameLobbyOnJoinedLobbyDataUpdated;
        KitchenGameLobby.Instance.OnLobbyPlayerStatusChanged += KitchenGameLobbyOnLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerDataUpdated += KitchenGameLobbyOnPlayerDataUpdated;
        KitchenGameLobby.Instance.OnLobbyLeft += KitchenGameLobbyOnLobbyLeft;

        kickButton.onClick.AddListener(() => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            KitchenGameLobby.Instance.KickPlayer(playerData.Id);
        });

        leaveButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
        });

        makeHostButton.onClick.AddListener(async () => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            // Make this player the host
            await KitchenGameLobby.Instance.SetLobbyHost(playerData.Id);
        });

        UpdatePlayerLobbyUI();
        UpdatePlayer();
    }

    private void KitchenGameLobbyOnLobbyLeft(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
        UpdatePlayer();
    }

    private void KitchenGameLobbyOnPlayerDataUpdated(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            return;
        }

        UpdatePlayer();
    }

    private void KitchenGameLobbyOnLobbyPlayerStatusChanged(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
    }

    private void KitchenGameLobbyOnLobbyJoined(object sender, EventArgs e) {
        UpdatePlayerLobbyUI();
        UpdatePlayer();
    }

    private void KitchenGameLobbyOnJoinedLobbyDataUpdated(object sender, EventArgs e) {
        UpdatePlayer();
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
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetPlayerDataFromPlayerIndex(playerIndex);
            
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

    private void UpdatePlayer() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        if (joinedLobby != null) {
            if (KitchenGameLobby.Instance.IsPlayerIndexConnected(playerIndex)) {
                Show();

                Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetPlayerDataFromPlayerIndex(playerIndex);

                playerNameText.text = LobbyPlayerDataConverter.GetPlayerDataValue(playerData, "playerName");

                int colorId = LobbyPlayerDataConverter.GetPlayerDataValue<int>(playerData, "colorId");

                playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId));
            } else {
                Hide();
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

    public void Show() {
        gameObject.SetActive(true);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnLobbyJoined -= KitchenGameLobbyOnLobbyJoined;
        KitchenGameLobby.Instance.OnJoinedLobbyDataUpdated -= KitchenGameLobbyOnJoinedLobbyDataUpdated;
        KitchenGameLobby.Instance.OnLobbyPlayerStatusChanged -= KitchenGameLobbyOnLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerDataUpdated -= KitchenGameLobbyOnPlayerDataUpdated;
        KitchenGameLobby.Instance.OnLobbyLeft -= KitchenGameLobbyOnLobbyLeft;
    }
}
