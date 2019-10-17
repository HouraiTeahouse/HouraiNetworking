using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
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
    _readBuffer = new byte[kMaxMessageSize];
    _lobbies = new Dictionary<CSteamID, SteamLobby>();

    callbackP2PSesssionRequest = Callback<P2PSessionRequest_t>.Create(OnP2PSessionRequest);
    callbackP2PConnectFail = Callback<P2PSessionConnectFail_t>.Create(OnP2PSessionConnectFail);
    callbackLobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    callbackLobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
  }

  public void Update() {
    uint dataSize;
    CSteamID userId;
    while (SteamNetworking.ReadP2PPacket(_readBuffer, kMaxMessageSize, 
                                         out dataSize, out userId)) {
      var handle = new AccountHandle(userId.m_SteamID);
      foreach (var lobby in _lobbies.Values) {
        LobbyMember member;
        if (lobby.Members.TryGetValue(handle, out member)) {
            member.DispatchNetworkMessage(_readBuffer, (int)dataSize);
        }
      }
    }
  }

  public async Task<LobbyBase> CreateLobby(LobbyCreateParams createParams) {
    ELobbyType type;
    switch (createParams.Type) {
      case LobbyType.Private: type = ELobbyType.k_ELobbyTypePrivate; break;
      default:
      case LobbyType.Public: type = ELobbyType.k_ELobbyTypePublic; break;
    }
    var size = createParams.Capacity;
    var lobbyEnter = SteamUtility.WaitFor<LobbyEnter_t>();
    var result = await SteamMatchmaking.CreateLobby(type, (int)size).ToTask<LobbyCreated_t>();
    if (SteamUtility.IsError(result.m_eResult)) {
      throw SteamUtility.CreateError(result.m_eResult);
    }
    await lobbyEnter;
    return AddOrUpdateLobby(new CSteamID(lobbyEnter.Result.m_ulSteamIDLobby));
  }

  public async Task<IList<LobbyBase>> SearchLobbies(Action<ILobbySearchBuilder> builder) {
    var queryBuilder = new LobbySearchBuilder();
    builder?.Invoke(queryBuilder);
    var list = await SteamMatchmaking.RequestLobbyList().ToTask<LobbyMatchList_t>();
    var results = new LobbyBase[list.m_nLobbiesMatching];
    for (var i = 0; i < list.m_nLobbiesMatching; i++) {
      var lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
      if (!lobbyId.IsValid()) continue;
      results[i] = new SteamLobby(lobbyId);
    }
    return results;
  }

  class LobbySearchBuilder : ILobbySearchBuilder {
    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, string value) {
      var comp = (ELobbyComparison)((int)comparison);
      SteamMatchmaking.AddRequestLobbyListStringFilter(key, value, comp);
      return this;
    }

    public ILobbySearchBuilder Filter(string key, SearchComparison comparison, int value) {
      var comp = (ELobbyComparison)((int)comparison);
      SteamMatchmaking.AddRequestLobbyListNumericalFilter(key, value, comp);
      return this;
    }

    public ILobbySearchBuilder Sort(string key, string value) {
      int intVal;
      if (!int.TryParse(value, out intVal)) {
        throw new ArgumentException("Steam lobby sorting must have a integer value");
      }
      SteamMatchmaking.AddRequestLobbyListNearValueFilter(key, intVal);
      return this;
    }

    public ILobbySearchBuilder Limit(int limit) {
      SteamMatchmaking.AddRequestLobbyListResultCountFilter(limit);
      return this;
    }
  }

  // Event Handlers

  void OnP2PSessionRequest(P2PSessionRequest_t evt) {
    var myId = SteamUser.GetSteamID();
    var remoteId = evt.m_steamIDRemote;
    foreach (var lobbyId in _lobbies.Keys) {
      var count = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
      bool hasMe = false;
      bool hasRemote = false;
      for (var i = 0; i < count; i++) {
        var memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
        hasMe |= memberId == myId;
        hasRemote |= memberId== remoteId;
      }
      if (hasMe && hasRemote) {
        Assert.IsTrue(SteamNetworking.AcceptP2PSessionWithUser(remoteId));
        return;
      }
    }
    // Did not find a matching lobby close the session.
    Debug.LogError($"Steam: Unexpected connection with {remoteId}");
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
    var lobby = AddOrUpdateLobby(lobbyId);
    if (evt.m_ulSteamIDLobby == evt.m_ulSteamIDMember) {
      // Lobby metadata updated
      lobby.DispatchUpdate();
    } else {
      LobbyMember member;
      var handle = new AccountHandle(evt.m_ulSteamIDMember);
      if (lobby.Members.TryGetValue(handle, out member)) {
        member.DispatchUpdate();
      }
    }
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