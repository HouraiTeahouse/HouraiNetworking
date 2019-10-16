using System;

namespace HouraiTeahouse.Networking {

public interface IIntegrationClient : IDisposable {

  AccountHandle ActiveUser { get; }
  ILobbyManager LobbyManager { get; }

  void Update();

}

}
