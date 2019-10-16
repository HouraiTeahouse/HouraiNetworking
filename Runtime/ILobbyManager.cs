using System;

namespace HouraiTeahouse.Networking {

public interface ILobbyManager {

  ILobby CreateLobby(LobbyCreateParams createParams);
  IList<ILobby> SearchLobbies(Action<ILobbySearchBuilder> builder);

}

}
