using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordIntegrationClient : IIntegrationClient {

  internal readonly DiscordApp.Discord _discordClient;
  readonly DiscordApp.LobbyManager _lobbyManager;
  readonly DiscordApp.UserManager _userManager;

  public AccountHandle ActiveUser {
    get => new AccountHandle((ulong)_userManager.GetCurrentUser().Id);
  }
  public ILobbyManager LobbyManager { get; }

  // TODO(james7132): Add log handling

  public DiscordIntegrationClient(long clientId) {
    var flags = (ulong)DiscordApp.CreateFlags.NoRequireDiscord;
    _discordClient = new DiscordApp.Discord(clientId, flags);
    _lobbyManager = _discordClient.GetLobbyManager();
    _userManager = _discordClient.GetUserManager();
    LobbyManager = new DiscordLobbyManager(this);
  }

  public void Update() {
    _lobbyManager.FlushNetwork();
    _discordClient.RunCallbacks();
  }

  public void Dispose() => _discordClient.Dispose();

}

}
