using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordIntegrationClient : IIntegrationClient {

  readonly DiscordApp.Discord _discordClient;
  readonly DiscordApp.LobbyManager _lobbyManager;

  public AccountHandle ActiveUser { get; private set; }
  public ILobbyManager LobbyManager { get; }

  // TODO(james7132): Add log handling

  public DiscordIntegrationClient(long clientId) {
    var flags = (ulong)DiscordApp.CreateFlags.NoRequireDiscord;
    _discordClient = new DiscordApp.Discord(clientId, flags);
    _lobbyManager = _discordClient.GetLobbyManager();
    LobbyManager = new DiscordLobbyManager(_discordClient);
  }

  public void Update() {
    _lobbyManager.FlushNetwork();
    _discordClient.RunCallbacks();
  }

  public void Dispose() => _discordClient.Dispose();

}

}
