
namespace HouraiTeahouse.Networking {

public interface IIntegrationClient {

  IntegrationAccountHandle ActiveUser { get; }
  IIntegrationLobbyManager LobbyManager { get; }

}

}
