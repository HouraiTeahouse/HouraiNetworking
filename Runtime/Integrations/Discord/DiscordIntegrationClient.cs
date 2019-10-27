using UnityEngine;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordIntegrationClient : IIntegrationClient {

  internal readonly DiscordApp.Discord _discordClient;
  readonly DiscordApp.LobbyManager _lobbyManager;
  readonly DiscordApp.UserManager _userManager;

  readonly DiscordLobbyManager _discordLobbyManager;

  public AccountHandle ActiveUser {
    get => new AccountHandle((ulong)_userManager.GetCurrentUser().Id);
  }
  public ILobbyManager LobbyManager => _discordLobbyManager;

  public DiscordIntegrationClient(long clientId, DiscordApp.LogLevel logLevel = DiscordApp.LogLevel.Info) {
    var flags = (ulong)DiscordApp.CreateFlags.NoRequireDiscord;
    _discordClient = new DiscordApp.Discord(clientId, flags);
    _discordClient.SetLogHook(logLevel, OnDiscordLog);
    _lobbyManager = _discordClient.GetLobbyManager();
    _userManager = _discordClient.GetUserManager();
    _discordLobbyManager = new DiscordLobbyManager(this);
  }

  public void Update() {
    _discordLobbyManager.Update();
    _lobbyManager.FlushNetwork();
    _discordClient.RunCallbacks();
  }

  public void Dispose() => _discordClient.Dispose();

  void OnDiscordLog(DiscordApp.LogLevel logLevel, string message) {
    var msg = $"Discord: {message}";
    LogType logType;
    switch (logLevel) {
      case DiscordApp.LogLevel.Error:   logType = LogType.Error;   break;
      default:
      case DiscordApp.LogLevel.Info: 
      case DiscordApp.LogLevel.Debug: 
        logType = LogType.Warning; 
        break;
    }
    Debug.unityLogger.Log(logType, msg);
  }

}

}
