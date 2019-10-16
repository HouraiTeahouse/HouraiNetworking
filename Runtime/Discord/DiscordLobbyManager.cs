namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobbyManager : ILobbyManager {

  internal readonly Discord.LobbyManager _lobbyManager;
  internal readonly Discord.ActivtyManager _activityManager;

  public DiscordiIntegrationLobbyManager(DiscordIntegrationClient client) {
    _lobbyManager = _client._discordClient.GetLobbyManager();
    _activityManager = _client._discordClient.GetActivityManager();
  }

  public ILobby CreateLobby(LobbyCreateParams createParams) {
    var txn = _lobbyManager.GetLobbyCreateTransaction();
    txn.SetCapacity(createParams.Capacity);
    txn.SetType((Discord.LobbyType)createParams.Type);
    ILobby outputLobby = null;
    _lobbyManager.CreateLobby(txn, (result, lobby) => {
        outputLobby = new DiscordLobby(this, lobby.id);

        var secret = _lobbyManager.GetLobbyActivitySecret(lobby.id);

        var activity = new Discord.Activity {
          Party = {
            Id = lobby.id,
            Size = {
              CurrentSize = 1,
              MaxSize = createParams.Capacity
            }
          },
          Secrets = { Join = secret }
        };

        _activityManager.UpdateActivity(activity, res => {});
    })
    return outputLobby;
  }

  public IList<LobbyBase> SearchLobbies(Action<ILobbySearchBuilder> builder) {
    var queryBuilder = new LobbySearchBuilder(_lobbyManager);
    builder?.Invoke(queryBuilder);
    var results = new List<LobbyBase>();
    _lobbyManager.Search(queryBuilder.Build(), (result) => {
        if (result == Discord.Result.OK) {
          var count = _lobbyManager.LobbyCount();
          if (results.Capacity < count) {
            results.Capacity = count;
          }
          for (var i = 0; i < count; i++) {
            var id = _lobbyManager.GetLobbyId(i);
            results.Add(new DiscordLobby(this, id));
          }
          return;
        }
    });
    return results;
  }

  class LobbySearchBuilder : ILobbySearchBuilder {

    readonly LobbySearchQuery _query;

    public LobbySearchBuilder(Discord.LobbyManager manager) {
      _query = manager.GetSearchQuery();
    }

    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, string value) {
      _query.Filterkey, (LobbySearchComparison)comparison,
                   LobbySearchCast.String, value);
      return this;
    }

    public ILobbySearchBuilder Sort(string key, string value) {
      _query.Sort(key, LobbSearchCast.String, value);
    }

    public ILobbySearchBuilder Limit(int limit) => _query.Limit(limit);

    public LobbySearchQuery Build() => _query;
  }

}

}
