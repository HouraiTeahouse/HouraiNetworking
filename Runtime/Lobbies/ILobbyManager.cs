using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouraiTeahouse.Networking {

public interface ILobbyManager {

  Task<Lobby> CreateLobby(LobbyCreateParams createParams);
  Task<IList<Lobby>> SearchLobbies(Action<ILobbySearchBuilder> builder = null);

}

}
