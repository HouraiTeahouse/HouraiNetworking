using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobbyManager : ILobbyManager {

  readonly IDictionary<long, DiscordLobby> _lobbies;

  internal readonly DiscordIntegrationClient _integrationClient;
  internal readonly DiscordApp.LobbyManager _lobbyManager;
  internal readonly DiscordApp.ActivityManager _activityManager;

  public DiscordLobbyManager(DiscordIntegrationClient integrationClient) {
    _lobbies = new Dictionary<long, DiscordLobby>();
    _integrationClient = integrationClient;
    var client = _integrationClient._discordClient;
    _lobbyManager = client.GetLobbyManager();
    _activityManager = client.GetActivityManager();

    _lobbyManager.OnNetworkMessage += OnNetworkMessage;
    _lobbyManager.OnLobbyMessage += OnLobbyMessage;

    _lobbyManager.OnMemberConnect += OnMemberConnect;
    _lobbyManager.OnMemberDisconnect += OnMemberDisconnect;
    _lobbyManager.OnMemberUpdate += OnMemberUpdate;
  }

  public Task<Lobby> CreateLobby(LobbyCreateParams createParams) {
    var txn = _lobbyManager.GetLobbyCreateTransaction();
    txn.SetCapacity(createParams.Capacity);
    txn.SetType((DiscordApp.LobbyType)createParams.Type);
    var future = new TaskCompletionSource<Lobby>();
    _lobbyManager.CreateLobby(txn, (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
        if (result != DiscordApp.Result.Ok) {
          future.SetException(new Exception($"Discord Error: {result}"));
          return;
        }
        var outputLobby = new DiscordLobby(this, lobby);
        _lobbies.Add(lobby.Id, outputLobby);
        future.SetResult(outputLobby);

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
    return future.Task;
  }

  public Task<IList<Lobby>> SearchLobbies(Action<ILobbySearchBuilder> builder) {
    var queryBuilder = new LobbySearchBuilder(_lobbyManager);
    builder?.Invoke(queryBuilder);
    var future = new TaskCompletionSource<IList<Lobby>>();
    _lobbyManager.Search(queryBuilder.Build(), (result) => {
        if (result != DiscordApp.Result.Ok) {
          future.SetException(new Exception($"Discord Error: {result}"));
          return;
        }
        var count = _lobbyManager.LobbyCount();
        var results = new Lobby[count];
        for (var i = 0; i < count; i++) {
          var lobby = _lobbyManager.GetLobby(_lobbyManager.GetLobbyId(i));
          var discordLobby = new DiscordLobby(this, lobby);
          _lobbies.Add(lobby.Id, discordLobby);
          results[i] = discordLobby;
        }
        future.SetResult(results);
    });
    return future.Task;
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

  internal void DestroyLobby(DiscordLobby lobby) {
    _lobbies.Remove((long)lobby.Id);
  }

  // Callbacks

  void OnMemberConnect(long lobbyId, long userId) {
    DiscordLobby lobby;
    if (_lobbies.TryGetValue(lobbyId, out lobby)) {
      lobby.Members.Add(new AccountHandle((ulong)userId));
    }
  }

  void OnMemberDisconnect(long lobbyId, long userId) {
    DiscordLobby lobby;
    if (_lobbies.TryGetValue(lobbyId, out lobby)) {
      lobby.Members.Remove(new AccountHandle((ulong)userId));
    }
  }

  void OnNetworkMessage(long lobbyId, long userId, byte _, byte[] data) {
    DiscordLobby lobby;
    LobbyMember member;
    var handle = new AccountHandle((ulong)userId);
    if (!_lobbies.TryGetValue(lobbyId, out lobby)) return;
    if (!lobby.Members.TryGetValue(handle, out member)) return;
    member.DispatchNetworkMessage(data);
  }

  void OnMemberUpdate(long lobbyId, long userId) {
    DiscordLobby lobby;
    LobbyMember member;
    var handle = new AccountHandle((ulong)userId);
    if (!_lobbies.TryGetValue(lobbyId, out lobby)) return;
    if (!lobby.Members.TryGetValue(handle, out member)) return;
    member.DispatchUpdate();
  }

  void OnLobbyDelete(long lobbyId, string reason) {
    DiscordLobby lobby;
    if (_lobbies.TryGetValue(lobbyId, out lobby)) {
      lobby.Dispose();
      _lobbies.Remove(lobbyId);
    }
  }

  void OnLobbyMessage(long lobbyId, long userId, byte[] data) {
    DiscordLobby lobby;
    LobbyMember member;
    var handle = new AccountHandle((ulong)userId);
    if (!_lobbies.TryGetValue(lobbyId, out lobby)) return;
    if (!lobby.Members.TryGetValue(handle, out member)) return;
    lobby.DispatchLobbyMessage(member, data, (uint)data.Length);
  }

  DiscordLobby GetLobby(DiscordApp.Lobby data) {
    DiscordLobby lobby;
    if (!_lobbies.TryGetValue(data.Id, out lobby)) {
      lobby = new DiscordLobby(this, data);
      _lobbies.Add(data.Id, lobby);
    } else {
      lobby.Update(data);
    }
    return lobby;
  }

}

}
