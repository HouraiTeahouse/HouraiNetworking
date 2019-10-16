
namespace HouraiTeahouse.Networking {

public interface IIntegrationClient {

  AccountHandle ActiveUser { get; }
  ILobbyManager LobbyManager { get; }

}

}
