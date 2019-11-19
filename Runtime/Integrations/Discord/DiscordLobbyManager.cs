using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

internal class DiscordLobbyManager : ILobbyManager {

  readonly Dictionary<long, DiscordLobby> _connectedLobbies;

  internal readonly DiscordIntegrationClient _integrationClient;
  internal readonly DiscordApp.LobbyManager _lobbyManager;
  internal readonly DiscordApp.ActivityManager _activityManager;

  public DiscordLobbyManager(DiscordIntegrationClient integrationClient) {
    _connectedLobbies = new Dictionary<long, DiscordLobby>();
    _integrationClient = integrationClient;
    var client = _integrationClient._discordClient;
    _lobbyManager = client.GetLobbyManager();
    _activityManager = client.GetActivityManager();

    _lobbyManager.OnNetworkMessage += OnNetworkMessage;
    _lobbyManager.OnLobbyMessage += OnLobbyMessage;
    _lobbyManager.OnLobbyUpdate += OnLobbyUpdate;

    _lobbyManager.OnMemberConnect += OnMemberConnect;
    _lobbyManager.OnMemberDisconnect += OnMemberDisconnect;
    _lobbyManager.OnMemberUpdate += OnMemberUpdate;
  }

  public Task<Lobby> CreateLobby(LobbyCreateParams createParams) {
    var txn = _lobbyManager.GetLobbyCreateTransaction();
    txn.SetCapacity(createParams.Capacity);
    txn.SetType((DiscordApp.LobbyType)createParams.Type);
    if (createParams.Metadata != null) {
      foreach (var kvp in createParams.Metadata) {
        if (kvp.Key == null || kvp.Value == null) continue;
        txn.SetMetadata(kvp.Key, kvp.Value.ToString());
      }
    }
    var future = new TaskCompletionSource<Lobby>();
    _lobbyManager.CreateLobby(txn, (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
        if (result != DiscordApp.Result.Ok) {
          future.SetException(DiscordUtility.ToError(result));
          return;
        }
        var outputLobby = new DiscordLobby(this, lobby);
        _connectedLobbies.Add(lobby.Id, outputLobby);
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
        future.SetResult(outputLobby);
    });
    return future.Task;
  }

  public Task<IList<Lobby>> SearchLobbies(Action<ILobbySearchBuilder> builder) {
    var queryBuilder = new LobbySearchBuilder(_lobbyManager);
    builder?.Invoke(queryBuilder);
    var future = new TaskCompletionSource<IList<Lobby>>();
    _lobbyManager.Search(queryBuilder.Build(), (result) => {
        if (result != DiscordApp.Result.Ok) {
          future.SetException(DiscordUtility.ToError(result));
          return;
        }
        var count = _lobbyManager.LobbyCount();
        var results = new Lobby[count];
        for (var i = 0; i < count; i++) {
          var lobby = _lobbyManager.GetLobby(_lobbyManager.GetLobbyId(i));
          results[i] = new DiscordLobby(this, lobby);
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

  public void Dispose() {
    _lobbyManager.OnNetworkMessage -= OnNetworkMessage;
    _lobbyManager.OnLobbyMessage -= OnLobbyMessage;
    _lobbyManager.OnLobbyUpdate -= OnLobbyUpdate;

    _lobbyManager.OnMemberConnect -= OnMemberConnect;
    _lobbyManager.OnMemberDisconnect -= OnMemberDisconnect;
    _lobbyManager.OnMemberUpdate -= OnMemberUpdate;
  }

  internal Task JoinLobby(DiscordLobby discordLobby) {
    if (_connectedLobbies.ContainsKey((long)discordLobby.Id)) {
      throw new InvalidOperationException("[Discord] Already connected to lobby.");
    }
    Assert.IsNotNull(discordLobby);
    var future = new TaskCompletionSource<object>();
    _lobbyManager.ConnectLobby((long)discordLobby.Id, discordLobby.Secret, 
      (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
        if (result != DiscordApp.Result.Ok) {
          future.SetException(DiscordUtility.ToError(result));
          return;
        }
        discordLobby._data = lobby;
        _connectedLobbies.Add(lobby.Id, discordLobby);
        _lobbyManager.ConnectNetwork(lobby.Id);
        _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliability.Reliable, true);
        _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliability.Unreliable, false);
        discordLobby.Members.Refresh();
        future.SetResult(null);
      });
    return future.Task;
  }

  internal void LeaveLobby(DiscordLobby lobby) {
    Assert.IsNotNull(lobby);
    var id = (long)lobby.Id;
    if (!_connectedLobbies.ContainsKey(id)) {
      throw new InvalidOperationException($"Not connected to lobby: {id}");
    }
    _connectedLobbies.Remove(id);
    _lobbyManager.DisconnectNetwork(id);
    _lobbyManager.DisconnectLobby(id, DiscordUtility.LogIfError);
    lobby.Dispose();
  }

  internal void Update() {
    foreach (var lobby in _connectedLobbies.Values) {
      lobby.FlushChanges();
    }
  }

  // Callbacks

  void OnMemberConnect(long lobbyId, long userId) {
    if (_connectedLobbies.TryGetValue(lobbyId, out DiscordLobby lobby)) {
      var handle = new AccountHandle((ulong)userId);
      if (!lobby.Members.Contains(handle)) {
        lobby.Members.Add(handle);
      } else {
        Debug.LogWarning($"[Discord] Members somehow has joined multiple times: {lobbyId}, member: {userId}");
      }
    } else {
      Debug.LogWarning($"[Discord] Unexpected member connect for lobby: {lobbyId}");
    }
  }

  void OnMemberDisconnect(long lobbyId, long userId) {
    if (_connectedLobbies.TryGetValue(lobbyId, out DiscordLobby lobby)) {
      lobby.Members.Remove(new AccountHandle((ulong)userId));
    } else {
      Debug.LogWarning($"[Discord] Unexpected member disconnect for lobby: {lobbyId}, member: {userId}");
    }
  }

  unsafe void OnNetworkMessage(long lobbyId, long userId, byte _, byte[] data) {
    LobbyMember member = GetLobbyMember(lobbyId, userId);
    if (member != null) {
        member.DispatchNetworkMessage(new ReadOnlySpan<byte>(data, 0, data.Length));
    } else {
        Debug.LogWarning($"[Discord] Unexpected network message for lobby: {lobbyId}, member: {userId}");
    }
  }

  void OnMemberUpdate(long lobbyId, long userId) {
    LobbyMember member = GetLobbyMember(lobbyId, userId);
    if (member != null) {
      member.DispatchUpdate();
    } else {
      Debug.LogWarning($"[Discord] Unexpected Lobby Member Update event for lobby: {lobbyId}, member: {userId}");
    }
  }

  void OnLobbyUpdate(long lobbyId) {
    if (_connectedLobbies.TryGetValue(lobbyId, out DiscordLobby lobby)) {
      lobby.DispatchUpdate();
    } else {
      Debug.LogWarning($"[Discord] Unexpected lobby update event for lobby: {lobbyId}");
    }
  }

  void OnLobbyDelete(long lobbyId, string reason) {
    if (_connectedLobbies.TryGetValue(lobbyId, out DiscordLobby lobby)) {
      _connectedLobbies.Remove(lobbyId);
      lobby.DispatchDelete();
      lobby.Dispose();
    } else {
      Debug.LogWarning($"[Discord] Unexpected Lobby Delete event for lobby: {lobbyId}");
    }
  }

  unsafe void OnLobbyMessage(long lobbyId, long userId, byte[] data) {
        LobbyMember member = GetLobbyMember(lobbyId, userId);
        if (member != null) {
            member.Lobby.DispatchLobbyMessage(member, new ReadOnlySpan<byte>(data, 0, data.Length));
        } else {
            Debug.LogWarning($"[Discord] Unexpected network message for lobby: {lobbyId}, member: {userId}");
        }
  }

  LobbyMember GetLobbyMember(long lobbyId, long userId) {
    var handle = new AccountHandle((ulong)userId);
    if (_connectedLobbies.TryGetValue(lobbyId, out DiscordLobby lobby) &&
        lobby.Members.TryGetValue(handle, out LobbyMember member)) {
      return member;
    }
    return default(LobbyMember);
  }

}

}
