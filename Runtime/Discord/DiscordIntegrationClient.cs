using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordIntegrationClient : IIntegrationClient {

  internal readonly DiscordApp.Discord _discordClient;

  public AccountHandle ActiveUser { get; private set; }
  public ILobbyManager LobbyManager { get; }

  public DiscordIntegrationClient(long clientId) {
    _discordClient = new DiscordApp.Discord(clientId, (ulong)DiscordApp.CreateFlags.NoRequireDiscord);
    LobbyManager = new DiscordLobbyManager(this);
  }

}

}
