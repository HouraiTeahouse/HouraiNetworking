using System;
using System.Collections.Generic;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobbyManager : ILobbyManager {

  readonly IDictionary<long, DiscordLobby> _lobbies;

  internal readonly DiscordApp.LobbyManager _lobbyManager;
  internal readonly DiscordApp.ActivityManager _activityManager;

  public DiscordLobbyManager(DiscordIntegrationClient client) {
    _lobbies = new Dictionary<long, DiscordLobby>();
    _lobbyManager = client._discordClient.GetLobbyManager();
    _activityManager = client._discordClient.GetActivityManager();

    _lobbyManager.OnMemberConnect += _OnMemberJoin;
    _lobbyManager.OnMemberDisconnect += _OnMemberLeave;
    _lobbyManager.OnNetworkMessage += _OnNetworkMessage;
  }

  public LobbyBase CreateLobby(LobbyCreateParams createParams) {
    var txn = _lobbyManager.GetLobbyCreateTransaction();
    txn.SetCapacity(createParams.Capacity);
    txn.SetType((DiscordApp.LobbyType)createParams.Type);
    LobbyBase outputLobby = null;
    _lobbyManager.CreateLobby(txn, (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
        outputLobby = new DiscordLobby(this, lobby);
        _lobbies.Add(lobby.Id, outputLobby);

        var secret = _lobbyManager.GetLobbyActivitySecret(lobby.Id);

        var activity = new DiscordApp.Activity {
          Party = {
            Id = lobby.Id.ToString(),
            Size = {
              CurrentSize = 1,
              MaxSize = (int)createParams.Capacity
            }
          },
          Secrets = { Join = secret }
        };

        _activityManager.UpdateActivity(activity, res => {});
    });
    return outputLobby;
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
            var discordLobby = new DiscordLobby(this, lobby);
            _lobbies.Add(lobby.Id, discordLobby);
            results.Add(discordLobby);
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

  void _OnMemberJoin(long lobbyId, long userId) {
    DiscordLobby lobby;
    if (_lobbies.TryGetValue(lobbyId, out lobby)) {
      lobby.Members.Add(new AccountHandle((ulong)userId);
    }
  }

  void _OnMemberLeave(long lobbyId, long userId) {
    DiscordLobby lobby;
    if (_lobbies.TryGetValue(lobbyId, out lobby)) {
      lobby.Members.Remove(new AccountHandle((ulong)userId);
    }
  }

  DiscordLobby GetLobby(DiscordApp.Lobby data) {
    DiscordLobby lobby;
    if (!_lobbies.TryGetValue(lobby.Id, out lobby)) {
      lobby = new SteamLobby(id);
      _lobbies.Add(id, data);
    } else {
      lobby.Update(data);
    }
    return lobby;
  }



}

}
