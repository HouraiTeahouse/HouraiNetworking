using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouraiTeahouse.Networking {

public interface ILobbyManager {

  Task<LobbyBase> CreateLobby(LobbyCreateParams createParams);
  Task<IList<LobbyBase>> SearchLobbies(Action<ILobbySearchBuilder> builder);

}

}
