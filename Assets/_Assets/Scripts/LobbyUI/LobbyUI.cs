using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {
    
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button testMatchFoundButton;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    [SerializeField] private TextMeshProUGUI findMatchButtonText;

    private void Start() {
        // Make all buttons not interactable initially
        createLobbyButton.interactable = false;
        joinCodeButton.interactable = false;
        findMatchButton.interactable = false;
        testMatchFoundButton.interactable = false;

        KitchenGameLobby.Instance.OnUnityGamingServicesInitialized += KitchenGameLobbyOnUnityGamingServicesInitialized;       

        mainMenuButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        createLobbyButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.CreateLobby("LobbyName", true);
        });

        joinCodeButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });

        findMatchButton.onClick.AddListener(async () => {
            UpdateFindMatchUI();
            await KitchenGameLobby.Instance.SetPlayerMatchmakingStatus(KitchenGameLobby.MatchmakingStatus.Searching);
        });

        testMatchFoundButton.onClick.AddListener(() => {
            KitchenGameLobby.Instance.OnMatchFound();
        });

        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange += KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;

        playerNameInputField.text = KitchenGameLobby.Instance.GetPlayerName();
        playerNameInputField.onDeselect.AddListener(async (string newText) => {
            string currentName = KitchenGameLobby.Instance.GetPlayerName();
            
            if (newText != currentName) {
                await KitchenGameLobby.Instance.SetPlayerName(newText);
            }
        });
    }

    private void KitchenGameLobbyOnUnityGamingServicesInitialized(object sender, EventArgs e) {
        createLobbyButton.interactable = true;
        joinCodeButton.interactable = true;
        findMatchButton.interactable = true;
        testMatchFoundButton.interactable = true;
    }

    private void KitchenGameLobbyOnJoinedLobbyTopLevelDataChange(object sender, EventArgs e) {
        UpdateFindMatchUI();
    }

    private void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        lobbyCodeText.text = "Lobby Code: ";
    }

    private void UpdateFindMatchUI() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();

        if (joinedLobby != null) {
            if (LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.MatchmakingStatus) == KitchenGameLobby.MatchmakingStatus.Searching.ToString()) {
                findMatchButtonText.text = "SEARCHING...";
            } else {
                findMatchButtonText.text = "FIND MATCH";
            }
        } else {
            PlayerData playerData = KitchenGameLobby.Instance.GetPlayerData();
            if (playerData.matchmakingStatus == KitchenGameLobby.MatchmakingStatus.Searching.ToString()) {
                findMatchButtonText.text = "SEARCHING...";
            } else {
                findMatchButtonText.text = "FIND MATCH";
            }
        }
    }

    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        lobbyCodeText.text = "Lobby Code: " + joinedLobby.LobbyCode;
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnUnityGamingServicesInitialized -= KitchenGameLobbyOnUnityGamingServicesInitialized;
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange -= KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
    }
}
