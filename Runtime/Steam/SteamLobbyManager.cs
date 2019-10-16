using Steamworks;
using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking.Steam {

public class SteamLobbyManager : ILobbyManager {

  public LobbyBase CreateLobby(LobbyCreateParams createParams) {
  }

  public IList<LobbyBase> SearchLobbies(Action<ILobbySearchBuilder> builder) {
    var queryBuilder = new LobbySearchBuilder(_lobbyManager);
    builder?.Invoke(queryBuilder);
    var results = new List<LobbyBase>();
    _lobbyManager.Search(queryBuilder.Build(), (result) => {
        if (result == DiscordApp.Result.Ok) {
          var count = _lobbyManager.LobbyCount();
          if (results.Capacity < count) {
            results.Capacity = count;
          }
          for (var i = 0; i < count; i++) {
            var id = _lobbyManager.GetLobbyId(i);
            var lobby = _lobbyManager.GetLobby(id);
            results.Add(new DiscordLobby(this, lobby));
          }
          return;
        }
    });
    return results;
  }

  class LobbySearchBuilder : ILobbySearchBuilder {

    readonly DiscordApp.LobbySearchQuery _query;

    public LobbySearchBuilder(DiscordApp.LobbyManager manager) {
      _query = manager.GetSearchQuery();
    }

    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, string value) {
      _query.Filter(key, (DiscordApp.LobbySearchComparison)comparison,
                   DiscordApp.LobbySearchCast.String, value);
      return this;
    }

    public ILobbySearchBuilder Sort(string key, string value) {
      _query.Sort(key, DiscordApp.LobbySearchCast.String, value);
      return this;
    }

    public ILobbySearchBuilder Limit(int limit) {
      _query.Limit((uint)limit);
      return this;
    }

    public DiscordApp.LobbySearchQuery Build() => _query;
  }

}

}
