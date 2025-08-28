using System.Collections.Generic;
using Unity.Services.Lobbies.Models;

public static class LobbyPlayerDataConverter
{
    public static PlayerData ConvertToPlayerData(Unity.Services.Lobbies.Models.Player lobbyPlayer, int clientId = -1)
    {
        if (lobbyPlayer == null)
        {
            return new PlayerData
            {
                clientId = clientId,
                colorId = 0,
                playerName = "Unknown Player",
                playerId = ""
            };
        }

        return new PlayerData
        {
            clientId = clientId,
            colorId = GetPlayerDataValue<int>(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.ColorId, 0),
            playerName = GetPlayerDataValue(lobbyPlayer, KitchenGameLobby.LobbyDataKeys.PlayerName, "Player"),
            playerId = lobbyPlayer.Id ?? ""
        };
    }

    // Overload that automatically assigns clientId based on player list order
    public static PlayerData ConvertToPlayerData(Unity.Services.Lobbies.Models.Player lobbyPlayer, List<Unity.Services.Lobbies.Models.Player> allPlayers)
    {
        int clientId = allPlayers.FindIndex(p => p.Id == lobbyPlayer.Id);
        if (clientId == -1) clientId = 0; // Fallback to 0 if not found
        
        return ConvertToPlayerData(lobbyPlayer, clientId);
    }

    // Helper method to safely extract data from Player.Data dictionary
    public static string GetPlayerDataValue(Unity.Services.Lobbies.Models.Player player, string key, string defaultValue = "")
    {
        if (player?.Data != null && player.Data.ContainsKey(key))
        {
            return player.Data[key].Value;
        }
        return defaultValue;
    }

    public static string GetLobbyDataValue(Lobby lobby, string key, string defaultValue = "")
    {
        if (lobby?.Data != null && lobby.Data.ContainsKey(key))
        {
            return lobby.Data[key].Value;
        }
        return defaultValue;
    }

    public static T GetLobbyDataValue<T>(Lobby lobby, string key, T defaultValue = default(T))
    {
        string stringValue = GetLobbyDataValue(lobby, key);

        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;
        
        try
        {
            return (T)System.Convert.ChangeType(stringValue, typeof(T));
        }
        catch (System.Exception)
        {
            return defaultValue;
        }
    }

    // Generic helper for type conversion
    public static T GetPlayerDataValue<T>(Unity.Services.Lobbies.Models.Player player, string key, T defaultValue = default(T))
    {
        string stringValue = GetPlayerDataValue(player, key);
        
        if (string.IsNullOrEmpty(stringValue))
            return defaultValue;
            
        try
        {
            return (T)System.Convert.ChangeType(stringValue, typeof(T));
        }
        catch (System.Exception)
        {
            return defaultValue;
        }
    }

    // Convert entire player list to PlayerData array
    public static PlayerData[] ConvertPlayerList(List<Unity.Services.Lobbies.Models.Player> lobbyPlayers)
    {
        if (lobbyPlayers == null || lobbyPlayers.Count == 0)
            return new PlayerData[0];

        PlayerData[] playerDataArray = new PlayerData[lobbyPlayers.Count];
        
        for (int i = 0; i < lobbyPlayers.Count; i++)
        {
            playerDataArray[i] = ConvertToPlayerData(lobbyPlayers[i], i);
        }
        
        return playerDataArray;
    }

    // Find and convert specific player
    // public static PlayerData? FindAndConvertPlayer(List<Unity.Services.Lobbies.Models.Player> lobbyPlayers, string playerId)
    // {
    //     Player targetPlayer = lobbyPlayers.FirstOrDefault(p => p.Id == playerId);
    //     if (targetPlayer == null)
    //         return null;
            
    //     return ConvertToPlayerData(targetPlayer, lobbyPlayers);
    // }
}