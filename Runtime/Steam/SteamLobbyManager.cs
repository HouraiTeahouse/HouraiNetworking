using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Steam {

public class SteamLobbyManager : ILobbyManager {

  public const int kMaxMessageSize = 1200;

  readonly IDictionary<CSteamID, SteamLobby> _lobbies;
  readonly byte[] _readBuffer;

#pragma warning disable 0414
  readonly Callback<P2PSessionRequest_t> callbackP2PSesssionRequest;
  readonly Callback<P2PSessionConnectFail_t> callbackP2PConnectFail;
  readonly Callback<LobbyChatUpdate_t> callbackLobbyChatUpdate;
  readonly Callback<LobbyDataUpdate_t> callbackLobbyDataUpdate;
#pragma warning restore 0414

  public SteamLobbyManager() {
    _lobbies = new Dictionary<CSteamID, SteamLobby>();

    callbackP2PSesssionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    callbackP2PConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
    callbackLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    callbackLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);

    SteamNetworkingUtils.InitializeRelayAccess();

    _readBuffer = new byte[kMaxMessageSize];
  }

  public void Update() {
    uint dataSize;
    CSteamID userId;
    while (SteamNetworking.ReadP2PPacket(ref packetSize)) {
      byte[] buffer = ArrayPool<byte>.Shared.Get(packetSize);
    }
  }

  public Task<LobbyBase> CreateLobby(LobbyCreateParams createParams) {
    var type = createParams.Type;
    var size = createParams.Capacity;
    var lobbyEnter = SteamUtility.WaitFor<LobbyEnter_t>();
    var result = await SteamMatchmaking.CreateLobby(type, size).ToTask<LobbyCreated_t>();
    if (SteamUtility.IsError(result.m_eResult)) {
      throw new SteamUtility.CreateError(result.m_eResult);
    }
    await lobbyEnter;
    return AddOrUpdateLobby(new CSteamID(lobbyEnter.Result.m_ulSteamIDLobby));
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

  // Event Handlers

  void OnP2PSessionRequest(P2PSessionRequest_t evt) {
    var myId = SteamUser.GetSteamID();
    var remoteId = new CSteamID(evt.m_steamIDRemote);
    for (var lobbyId in _lobbies.Keys) {
      var count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
      bool hasMe = false;
      bool hasRemote = false;
      for (var i = 0; i < count; i++) {
        var memberId = SteamMatchmaking.GetLobbymemberByIndex(lobbyId, i);
        hasMe |= memberId == id;
        hasRemote |= memberId == id;
      }
      if (hasMe && hasRemote) {
        Assert.IsTrue(SteamNetworking.AcceptP2PSessionWithUser(remoteId));
        return;
      }
    }
    // Did not find a matching lobby close the session.
    Assert.IsTrue(SteamNetworking.CloseP2PSessionWithUser(remoteId));
  }

  void OnP2PSessionConnectFail(P2PSessionConnectFail_t evt) {
    // TODO(james7132): Implement
  }

  // Why Steam does lobby joins and leaves via a chat update is beyond me.
  void OnLobbyChatUpdate(LobbyChatUpdate_t evt) {
    var lobbyId = new CSteamID(evt.m_ulSteamIDLobby);
    SteamLobby lobby = AddOrUpdateLobby(lobbyId);
    uint leaveMask = ~(uint)EChatMemberStateChange.k_EChatMemberStateChangeEntered;
    uint stateChange = evt.m_rgfChatMemberStateChange;
    AccountHandle user = evt.m_ulSteamIDUserChanged;
    if ((stateChange & leaveMask) != 0) {
      lobby.Members.Remove(user);
    } else if ((stateChange & ~leaveMask) != 0) {
      lobby.Members.Add(user);
    }
  }

  void OnLobbyDataUpdate(LobbyDataUpdate_t evt) {
    var lobbyId = new CSteamID(evt.m_ulSteamIDLobby);
    SteamLobby lobby = AddOrUpdateLobby(lobbyId);
    lobby.Update();
  }

  SteamLobby AddOrUpdateLobby(CSteamID id) {
    SteamLobby lobby;
    if (!_lobbies.TryGetValue(id, out lobby)) {
      lobby = new SteamLobby(id);
      _lobbies.Add(id, lobby);
    }
    return lobby;
  }


}

}
