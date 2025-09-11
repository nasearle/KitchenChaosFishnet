using System;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyPlayerVisual : MonoBehaviour {
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private Button leaveButton;
    [SerializeField] private Button makeHostButton;
    [SerializeField] private Image isHostImage;
    [SerializeField] private TextMeshPro playerNameText;
    [SerializeField] private Transform[] playerSpawnPoints;

    private string _playerId;
    private bool _isLocalPlayer;

    private void Awake() {
        Hide();
    }

    private void Start() {
        KitchenGameLobby.Instance.OnPlayerDataChanged += KitchenGameLobbyOnPlayerDataChanged;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerDataChanged += KitchenGameLobbyOnJoinedLobbyPlayerDataChanged;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange += KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
        KitchenGameLobby.Instance.OnJoinedLobbyHostIdChanged += KitchenGameLobbyOnJoinedLobbyHostIdChanged;

        kickButton.onClick.AddListener(() => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerId(_playerId);
            KitchenGameLobby.Instance.KickPlayer(playerData.Id);
        });

        leaveButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
        });

        makeHostButton.onClick.AddListener(async () => {
            Unity.Services.Lobbies.Models.Player playerData = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerId(_playerId);
            // Make this player the host
            await KitchenGameLobby.Instance.SetLobbyHost(playerData.Id);
        });
    }

    public void InitializePlayerVisual(string playerId, bool isLocalPlayer) {
        SetPlayerId(playerId);
        SetIsLocalPlayer(isLocalPlayer);
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        UpdateLobbyPlayerVisualUI(joinedLobby != null ? joinedLobby.HostId : null);
        UpdateLobbyPlayerVisualData();
        Show();
    }

    private void KitchenGameLobbyOnJoinedLobbyHostIdChanged(object sender, KitchenGameLobby.OnJoinedLobbyHostIdChangedEventArgs e) {
        UpdateLobbyPlayerVisualUI(e.HostId);
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

    private void KitchenGameLobbyOnPlayerDataChanged(object sender, EventArgs e) {
        UpdateLocalLobbyPlayerVisualData();
    }

    private void KitchenGameLobbyOnJoinedLobbyPlayerDataChanged(object sender, EventArgs e) {
        UpdateLobbyPlayerVisualData();
    }

    public void UpdateLobbyPlayerVisualUI(string lobbyHostId) {        
        makeHostButton.gameObject.SetActive(false);
        isHostImage.gameObject.SetActive(false);
        kickButton.gameObject.SetActive(false);
        leaveButton.gameObject.SetActive(false);

        if (lobbyHostId == null) {
            return;
        }

        if (_playerId == lobbyHostId) {
            // this player visual is the host
            isHostImage.gameObject.SetActive(true);
        }

        if (_isLocalPlayer) {
            // this player visual is the local player
            leaveButton.gameObject.SetActive(true);
        }

        if (KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            // the client is the host
            if (_playerId != lobbyHostId) {
                // this player visual is not the host
                makeHostButton.gameObject.SetActive(true);
                kickButton.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateLocalLobbyPlayerVisualData() {
        if (_isLocalPlayer) {
            PlayerData playerData = KitchenGameLobby.Instance.GetPlayerData();
            playerNameText.text = playerData.playerName;
            playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(playerData.colorId));
        }
    }

    public void UpdateLobbyPlayerVisualData() {
        if (_isLocalPlayer) {
            UpdateLocalLobbyPlayerVisualData();
        } else {
            var lobbyPlayer = KitchenGameLobby.Instance.GetLobbyPlayerDataFromPlayerId(_playerId);
            playerNameText.text = LobbyPlayerDataConverter.GetPlayerDataValue(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.PlayerName);

            int colorId = LobbyPlayerDataConverter.GetPlayerDataValue<int>(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.ColorId);

            playerVisual.SetPlayerColor(KitchenGameLobby.Instance.GetPlayerColorByColorId(colorId));
        }
    }

    public void Show() {
        gameObject.SetActive(true);
    }
    
    public void Hide() {
        gameObject.SetActive(false);
    }

    public string GetPlayerId() {
        return _playerId;
    }

    public void SetPlayerId(string playerId) {
        _playerId = playerId;
    }

    public void SetIsLocalPlayer(bool isLocalPlayer) {
        _isLocalPlayer = isLocalPlayer;
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnPlayerDataChanged -= KitchenGameLobbyOnPlayerDataChanged;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerDataChanged -= KitchenGameLobbyOnJoinedLobbyPlayerDataChanged;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange -= KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
        KitchenGameLobby.Instance.OnJoinedLobbyHostIdChanged -= KitchenGameLobbyOnJoinedLobbyHostIdChanged;
    }
}
