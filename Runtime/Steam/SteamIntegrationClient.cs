namespace HouraiTeahouse.Networking.Steam {

public class SteamIntegrationClient : IIntegrationClient {

  internal readonly Steam _steamClient;

  public IntegrationAccountHandle ActiveUser { get; private set; }
  public IIntegrationLobbyManager LobbyManager => _steamClient.GetLobbyManager();

  public SteamIntegrationClient(int clientId) {
    _steamClient = new Steam(clientId,
                             steamCreateFlags.NoRequiredsteam);
  }

}

}
