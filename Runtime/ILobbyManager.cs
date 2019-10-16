using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public interface ILobbyManager {

  LobbyBase CreateLobby(LobbyCreateParams createParams);
  IList<LobbyBase> SearchLobbies(Action<ILobbySearchBuilder> builder);

}

}
