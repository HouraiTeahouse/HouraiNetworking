using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouraiTeahouse.Networking {

public interface ILobbyManager : IDisposable {

  Task<Lobby> CreateLobby(LobbyCreateParams createParams);
  Task<IList<Lobby>> SearchLobbies(Action<ILobbySearchBuilder> builder = null);

}

}
