using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour {
    
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button joinCodeButton;
    [SerializeField] private Button findMatchButton;
    [SerializeField] private Button copyLobbyCodeButton;
    [SerializeField] private TextMeshProUGUI copyLobbyCodeButtonText;
    [SerializeField] private TMP_InputField joinCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private TextMeshProUGUI joinCodeText;
    [SerializeField] private TextMeshProUGUI findMatchButtonText;

    private float _copyLobbyCodeButtonTextTimer = 0f;
    private float _copyLobbyCodeButtonTextTimerMax = 3f;
    private bool _findMatchButtonOnCoolDown;
    private float _findMatchButtonCoolDownTimer = 0f;
    private float _findMatchButtonCoolDownTimerMax = 2f;

    private void Start() {
        // Make all buttons not interactable initially
        createLobbyButton.interactable = false;
        joinCodeButton.interactable = false;
        findMatchButton.interactable = false;

        if (KitchenGameLobby.Instance.GetLobby() == null) {
            copyLobbyCodeButton.gameObject.SetActive(false);
            joinCodeText.text = "";
        }

        KitchenGameLobby.Instance.OnUnityGamingServicesInitialized += KitchenGameLobbyOnUnityGamingServicesInitialized; 
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded += KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded += KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange += KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerStatusChanged += KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerMatchmakingStatusChanged += KitchenGameLobbyOnPlayerMatchmakingStatusChanged;

        mainMenuButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.LeaveLobby();
            Loader.Load(Loader.Scene.MainMenuScene);
        });

        createLobbyButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.CreateLobby("LobbyName", true);
        });

        copyLobbyCodeButton.onClick.AddListener(() => {
            GUIUtility.systemCopyBuffer = joinCodeText.text;
            copyLobbyCodeButtonText.text = "COPIED";
            _copyLobbyCodeButtonTextTimer = _copyLobbyCodeButtonTextTimerMax;
        });

        joinCodeButton.onClick.AddListener(async () => {
            await KitchenGameLobby.Instance.JoinWithCode(joinCodeInputField.text);
        });

        findMatchButton.onClick.AddListener(FindMatch);

        playerNameInputField.text = KitchenGameLobby.Instance.GetPlayerName();
        playerNameInputField.onDeselect.AddListener((string newText) => {
            string currentName = KitchenGameLobby.Instance.GetPlayerName();
            
            if (newText != currentName) {
                KitchenGameLobby.Instance.SetPlayerName(newText);
            }
        });
    }

    private void Update() {
        if (_copyLobbyCodeButtonTextTimer > 0) {
            _copyLobbyCodeButtonTextTimer -= Time.deltaTime;
            if (_copyLobbyCodeButtonTextTimer <= 0) {
                copyLobbyCodeButtonText.text = "COPY";
            }
        }

        if (_findMatchButtonOnCoolDown) {
            _findMatchButtonCoolDownTimer -= Time.deltaTime;
            if (!findMatchButton.interactable && _findMatchButtonCoolDownTimer <= 0) {
                findMatchButton.interactable = true;
                _findMatchButtonOnCoolDown = false;
            }
        }
    }

    private void KitchenGameLobbyOnUnityGamingServicesInitialized(object sender, EventArgs e) {
        joinCodeButton.interactable = true;

        if (KitchenGameLobby.Instance.GetLobby() == null) {
            createLobbyButton.interactable = true;
        }

        if (KitchenGameLobby.Instance.GetLobby() == null || KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            findMatchButton.interactable = true;
        }
    }

    private void KitchenGameLobbyOnLobbyLeaveSucceeded(object sender, EventArgs e) {
        joinCodeText.text = "";
        findMatchButton.interactable = true;
        createLobbyButton.interactable = true;
        copyLobbyCodeButton.gameObject.SetActive(false);
    }
    
    private void KitchenGameLobbyOnLobbyJoinSucceeded(object sender, EventArgs e) {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();
        joinCodeText.text = joinedLobby.LobbyCode;
        
        createLobbyButton.interactable = false;

        if (!KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            findMatchButton.interactable = false;
        }

        copyLobbyCodeButton.gameObject.SetActive(true);
    }

    private void KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged(object sender, EventArgs e) {
        if (!KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            findMatchButton.interactable = false;
        } else {
            findMatchButton.interactable = true;
        }
    }

    private void KitchenGameLobbyOnJoinedLobbyTopLevelDataChange(object sender, EventArgs e) {
        UpdateFindMatchUI();
    }

    private void KitchenGameLobbyOnPlayerMatchmakingStatusChanged(object sender, EventArgs e) {
        LocalUpdateFindMatchUI();
    }

    private void SetFindMatchButtonText(string matchmakingStatus) {
        switch (matchmakingStatus) {
            case nameof(KitchenGameLobby.MatchmakingStatus.Waiting):
                findMatchButtonText.text = "FIND MATCH";
                findMatchButton.onClick.RemoveListener(CancelMatchmaking);
                findMatchButton.onClick.RemoveListener(FindMatch);
                findMatchButton.onClick.AddListener(FindMatch);
                break;
            case nameof(KitchenGameLobby.MatchmakingStatus.Searching):
                findMatchButtonText.text = "SEARCHING...";
                findMatchButton.onClick.RemoveListener(FindMatch);
                findMatchButton.onClick.RemoveListener(CancelMatchmaking);
                findMatchButton.onClick.AddListener(CancelMatchmaking);
                break;
            case nameof(KitchenGameLobby.MatchmakingStatus.Cancelling):
                findMatchButtonText.text = "CANCELLING...";
                break;
            case nameof(KitchenGameLobby.MatchmakingStatus.MatchFound):
                findMatchButtonText.text = "MATCH FOUND!";
                break;
            default:
                break;
        }
    }

    private void LocalUpdateFindMatchUI() {
        PlayerData playerData = KitchenGameLobby.Instance.GetPlayerData();

        SetFindMatchButtonText(playerData.matchmakingStatus);
    }

    private void UpdateFindMatchUI() {
        Lobby joinedLobby = KitchenGameLobby.Instance.GetLobby();

        // The host player has already updated, so no need to do it again.
        if (joinedLobby != null && !KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            string matchmakingStatus = LobbyPlayerDataConverter.GetLobbyDataValue(joinedLobby, KitchenGameLobby.LobbyDataKeys.MatchmakingStatus);
            
            SetFindMatchButtonText(matchmakingStatus);
        }
    }

    private async void FindMatch() {
        findMatchButton.interactable = false;

        if (KitchenGameLobby.Instance.GetLobby() == null || KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            await KitchenGameMatchmaker.Instance.FindMatchWithBackoff();
        }
        
        _findMatchButtonOnCoolDown = true;
        _findMatchButtonCoolDownTimer = _findMatchButtonCoolDownTimerMax;
    }

    private async void CancelMatchmaking() {
        findMatchButton.interactable = false;

        if (KitchenGameLobby.Instance.GetLobby() == null || KitchenGameLobby.Instance.IsLocalPlayerLobbyHost()) {
            // await KitchenGameMatchmaker.Instance.CancelMatchmaking();
            await KitchenGameMatchmaker.Instance.CancelMatchmakingWithBackoff();
        }
        
        _findMatchButtonOnCoolDown = true;
        _findMatchButtonCoolDownTimer = _findMatchButtonCoolDownTimerMax;
    }

    private void OnDestroy() {
        KitchenGameLobby.Instance.OnUnityGamingServicesInitialized -= KitchenGameLobbyOnUnityGamingServicesInitialized;
        KitchenGameLobby.Instance.OnLobbyJoinSucceeded -= KitchenGameLobbyOnLobbyJoinSucceeded;
        KitchenGameLobby.Instance.OnLobbyLeaveSucceeded -= KitchenGameLobbyOnLobbyLeaveSucceeded;
        KitchenGameLobby.Instance.OnJoinedLobbyTopLevelDataChange -= KitchenGameLobbyOnJoinedLobbyTopLevelDataChange;
        KitchenGameLobby.Instance.OnJoinedLobbyPlayerStatusChanged -= KitchenGameLobbyOnJoinedLobbyPlayerStatusChanged;
        KitchenGameLobby.Instance.OnPlayerMatchmakingStatusChanged -= KitchenGameLobbyOnPlayerMatchmakingStatusChanged;
    }
}
