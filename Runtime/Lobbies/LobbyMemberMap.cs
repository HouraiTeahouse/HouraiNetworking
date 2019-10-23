using System;
using System.Collections;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public sealed class LobbyMemberMap : IEnumerable<LobbyMember>, IDisposable {

  readonly Dictionary<AccountHandle, LobbyMember> _members;
  readonly LobbyBase _lobby;

  public LobbyMember Me => Get(new AccountHandle(_lobby.UserId));
  public LobbyMember Owner => Get(new AccountHandle(_lobby.OwnerId));

  public event Action<LobbyMember> OnMemberJoin;
  public event Action<LobbyMember> OnMemberLeave;

  public LobbyMemberMap(LobbyBase lobby) {
    _members = new Dictionary<AccountHandle, LobbyMember>();
    _lobby = lobby;
  }

  internal LobbyMember Add(AccountHandle handle) {
    if (!_members.TryGetValue(handle, out LobbyMember player)) {
      player = new LobbyMember(_lobby, handle);
      _members.Add(handle, player);
      OnMemberJoin?.Invoke(player);
      return player;
    }
    throw new Exception("Cannot add a member that already exists in the lobby.");
  }

  internal LobbyMember GetOrAdd(AccountHandle handle) {
    LobbyMember player;
    if (!_members.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _members.Add(handle, player);
      OnMemberJoin?.Invoke(player);
    }
    return player;
  }

  internal bool Remove(AccountHandle handle) {
    if (_members.TryGetValue(handle, out LobbyMember player)) {
      _members.Remove(handle);
      OnMemberLeave?.Invoke(player);
      return true;
    }
    return false;
  }

  // Read-only accessors are public

  public LobbyMember Get(AccountHandle handle) {
    if (_members.TryGetValue(handle, out LobbyMember owner)) {
      return owner;
    }
    return default(LobbyMember);
  }

  public bool TryGetValue(AccountHandle handle, out LobbyMember member) =>
    _members.TryGetValue(handle, out member);

  public bool Contains(AccountHandle handle) => _members.ContainsKey(handle);

  public IEnumerator<LobbyMember> GetEnumerator() => _members.Values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _members.Values.GetEnumerator();

  public void Broadcast(byte[] msg, int size = -1, Reliability reliability = Reliability.Reliable) {
    foreach (var member in _members.Values) {
      member.SendMessage(msg, size, reliability: reliability);
    }
  }

  public void Dispose() {
    OnMemberJoin = null;
    OnMemberLeave = null;
    foreach (var member in _members.Values) {
      member.Dispose();
    }
    _members.Clear();
  }

}

}
