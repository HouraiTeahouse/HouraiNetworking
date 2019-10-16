namespace HouraiTeahouse.Networking.Steam {

public class SteamIntegrationClient : IIntegrationClient {

  public AccountHandle ActiveUser { get; private set; }
  public ILobbyManager LobbyManager { get; }

  public SteamIntegrationClient(int clientId) {
    LobbyManager = new SteamLobbyManager();
  }

}

}
