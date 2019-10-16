using Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordIntegrationClient : IIntegrationClient {

  internal readonly Discord _discordClient;

  public IntegrationAccountHandle ActiveUser { get; private set; }
  public ILobbyManager LobbyManager { get; }

  public DiscordIntegrationClient(int clientId) {
    _discordClient = new Discord(clientId,
                                 DiscordCreateFlags.NoRequiredDiscord);
    LobbyManager = new DiscordLobbyManager(this);
  }

}

}
