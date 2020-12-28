using Steamworks;
using Steamworks.Data;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Steam {

internal class SteamLobbyManager : ILobbyManager {

  public const int kMaxMessageSize = ushort.MaxValue;

  readonly Dictionary<SteamId, SteamLobby> _connectedLobbies;
  readonly byte[] _readBuffer;

  public SteamLobbyManager() {
    _readBuffer = new byte[kMaxMessageSize];
    _connectedLobbies = new Dictionary<SteamId, SteamLobby>();

    SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;
    SteamNetworking.OnP2PConnectionFailed += OnP2PSessionConnectFail;

    SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
    SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
    SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeave;
    SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberRemoved;
    SteamMatchmaking.OnLobbyMemberBanned += OnLobbyMemberRemoved;

    SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;
  }

    public unsafe void Update() {
        fixed (byte* ptr = _readBuffer) {
            uint dataSize = 0u;
            SteamId userId = 0UL;
            while (SteamNetworking.ReadP2PPacket(_readBuffer, ref dataSize, ref userId)) {
                var handle = new AccountHandle(userId);
                var handled = false;
                foreach (var lobby in _connectedLobbies.Values) {
                    if (lobby.Members.TryGetValue(handle, out LobbyMember member)) {
                        member.DispatchNetworkMessage(new ReadOnlySpan<byte>(_readBuffer, 0, (int)dataSize));
                        handled = true;
                    }
                }
                if (!handled) {
                    Debug.LogWarning($"[Steam] Unexpected network message from user: {userId}");
                }
            }
        }
    }

  public async Task<Lobby> CreateLobby(LobbyCreateParams createParams) {
    var result = await SteamMatchmaking.CreateLobbyAsync((int)createParams.Capacity);
    var steamLobby = result ?? throw new Exception("Failed to create the lobby");
    if (createParams.Type == LobbyType.Public) {
      steamLobby.SetPublic();
    }
    if (createParams.Metadata != null) {
      foreach (var kvp in createParams.Metadata) {
        if (kvp.Key == null || kvp.Value == null) continue;
        steamLobby.SetData(kvp.Key, kvp.Value.ToString());
      }
    }
    var lobby = new SteamLobby(steamLobby, this, true);
    _connectedLobbies.Add(steamLobby.Id, lobby);
    return lobby;
  }

  public async Task<IList<Lobby>> SearchLobbies(Action<ILobbySearchBuilder> builder = null) {
    var queryBuilder = new LobbySearchBuilder(SteamMatchmaking.LobbyList);
    builder?.Invoke(queryBuilder);
    var result = await queryBuilder.RunAsync();
    if (result == null) return new Lobby[0];
    var lobbies = new Lobby[result.Length];
    for (var i = 0; i < lobbies.Length; i++) {
        lobbies[i] = new SteamLobby(result[i], this, false);
    }
    return lobbies;
  }

  class LobbySearchBuilder : ILobbySearchBuilder {

      readonly LobbyQuery _query;

      public LobbySearchBuilder(LobbyQuery query) {
          _query = query;
      }

    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, string value) {
      //var comp = (LobbyComparison)((int)comparison);
      //_query.AddStringFilter(key, value, comp);
      return this;
    }

    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, int value) {
      //var comp = (LobbyComparison)((int)comparison);
      //_query.AddNumericalFilter(key, value, comp);
      return this;
    }

    public ILobbySearchBuilder Sort(string key, string value) {
      int intVal;
      if (!int.TryParse(value, out intVal)) {
        throw new ArgumentException("Steam lobby sorting must have a integer value");
      }
      _query.OrderByNear(key, intVal);
      return this;
    }

    public ILobbySearchBuilder Limit(int limit) {
        _query.WithMaxResults(limit);
        return this;
    }

    public Task<Steamworks.Data.Lobby[]> RunAsync() => _query.RequestAsync();
  }

  public void Dispose() {
    SteamNetworking.OnP2PSessionRequest += OnP2PSessionRequest;
    SteamNetworking.OnP2PConnectionFailed += OnP2PSessionConnectFail;

    SteamMatchmaking.OnLobbyMemberJoined += OnLobbyMemberJoined;
    SteamMatchmaking.OnLobbyMemberLeave += OnLobbyMemberLeave;
    SteamMatchmaking.OnLobbyMemberDisconnected += OnLobbyMemberLeave;
    SteamMatchmaking.OnLobbyMemberKicked += OnLobbyMemberRemoved;
    SteamMatchmaking.OnLobbyMemberBanned += OnLobbyMemberRemoved;
    SteamMatchmaking.OnLobbyMemberDataChanged += OnLobbyMemberDataChanged;
  }

  internal async Task JoinLobby(SteamLobby lobby) {
    Assert.IsNotNull(lobby);
    if (_connectedLobbies.ContainsKey(lobby.Id)) {
      throw new InvalidOperationException("[Steam] Already connected to lobby.");
    }
    var result = await SteamMatchmaking.JoinLobbyAsync(lobby.Id);
    var steamLobby = result ?? throw new Exception($"Failed to connect to lobby: {lobby.Id}");
    lobby._lobby = steamLobby;
    _connectedLobbies.Add(lobby.Id, lobby);
    lobby.Members.Refresh();
  }

  internal void LeaveLobby(SteamLobby lobby) {
    Assert.IsNotNull(lobby);
    if (!_connectedLobbies.ContainsKey(lobby.Id)) {
      throw new InvalidOperationException("[Steam] Not connected to lobby.");
    }
    _connectedLobbies.Remove(lobby.Id);
    lobby._lobby.Leave();
    lobby.Dispose();
  }

  // Event Handlers

  void OnP2PSessionRequest(SteamId remoteId) {
    var myId = SteamClient.SteamId;
    foreach (var lobby in _connectedLobbies.Values) {
      bool hasMe = false;
      bool hasRemote = false;
      foreach (var member in lobby._lobby.Members) {
        hasMe |= member.Id == myId;
        hasRemote |= member.Id == remoteId;
      }
      if (hasMe && hasRemote) {
        Assert.IsTrue(SteamNetworking.AcceptP2PSessionWithUser(remoteId));
        Debug.Log($"[Steam] Established connection with {remoteId}");
        return;
      }
    }
    // Did not find a matching lobby close the session.
    Debug.LogError($"[Steam] Unexpected connection with {remoteId}");
    Assert.IsTrue(SteamNetworking.CloseP2PSessionWithUser(remoteId));
  }

  void OnP2PSessionConnectFail(SteamId id, P2PSessionError error) {
    // TODO(james7132): Implement Properly
    Debug.LogError($"[Steam] Failed to connect to remote user {id}: {error}");
  }

  void OnLobbyMemberJoined(Steamworks.Data.Lobby steamLobby, Friend member) {
    SteamLobby lobby;
    if (!_connectedLobbies.TryGetValue(steamLobby.Id, out lobby)) {
      Debug.LogWarning($"[Steam] Unexpected lobby member join event for lobby: {steamLobby.Id}");
      return;
    }
    lobby.Members.Add(new AccountHandle(member.Id));
  }

  void OnLobbyMemberLeave(Steamworks.Data.Lobby steamLobby, Friend member) {
    SteamLobby lobby;
    if (!_connectedLobbies.TryGetValue(steamLobby.Id, out lobby)) {
      Debug.LogWarning($"[Steam] Unexpected lobby member join event for lobby: {steamLobby.Id}");
      return;
    }
    lobby.Members.Remove(new AccountHandle(member.Id));
  }

  void OnLobbyMemberRemoved(Steamworks.Data.Lobby steamLobby, Friend member, Friend authorizer) =>
    OnLobbyMemberLeave(steamLobby, member);

  void OnLobbyDataChanged(Steamworks.Data.Lobby steamLobby) {
    SteamLobby lobby;
    if (!_connectedLobbies.TryGetValue(steamLobby.Id, out lobby)) {
      Debug.LogWarning($"[Steam] Unexpected lobby update event for lobby: {steamLobby.Id}");
      return;
    }
    lobby.DispatchUpdate();
  }

  void OnLobbyMemberDataChanged(Steamworks.Data.Lobby steamLobby, Friend friend) {
    SteamLobby lobby;
    LobbyMember member;
    if (!_connectedLobbies.TryGetValue(steamLobby.Id, out lobby)) {
      Debug.LogWarning($"[Steam] Unexpected lobby member update event for lobby: {steamLobby.Id}");
      return;
    }
    var handle = new AccountHandle(friend.Id);
    if (!lobby.Members.TryGetValue(handle, out member)) {
      Debug.LogWarning($"[Steam] Unexpected lobby member update event for lobby: {steamLobby.Id}");
      return;
    }

    Debug.Log("member changed!!!");
    member.DispatchUpdate();
  }

}

}
