using System;
using System.Collections;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public class LobbyMemberMap : IEnumerable<LobbyMember>, IDisposable {

  readonly Dictionary<AccountHandle, LobbyMember> _members;
  readonly LobbyBase _lobby;

  public event Action<LobbyMember> OnMemberJoin;
  public event Action<LobbyMember> OnMemberLeave;

  public LobbyMemberMap(LobbyBase lobby) {
    _members = new Dictionary<AccountHandle, LobbyMember>();
    _lobby = lobby;
  }

  public LobbyMember Add(AccountHandle handle) {
    LobbyMember player;
    if (!_members.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _members.Add(handle, player);
      OnMemberJoin?.Invoke(player);
      return player;
    }
    throw new Exception("Cannot add a member that already exists in the lobby.");
  }

  public LobbyMember Get(AccountHandle handle) => _members[handle];

  public LobbyMember GetOrAdd(AccountHandle handle) {
    LobbyMember player;
    if (!_members.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _members.Add(handle, player);
      OnMemberJoin?.Invoke(player);
    }
    return player;
  }

  public bool TryGetValue(AccountHandle handle, out LobbyMember member) =>
    _members.TryGetValue(handle, out member);

  public bool Contains(AccountHandle handle) => _members.ContainsKey(handle);

  public bool Remove(AccountHandle handle) {
    LobbyMember player;
    if (_members.TryGetValue(handle, out player)) {
      _members.Remove(handle);
      OnMemberLeave?.Invoke(player);
      return true;
    }
    return false;
  }

  public IEnumerator<LobbyMember> GetEnumerator() => _members.Values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _members.Values.GetEnumerator();

  public void Broadcast(byte[] msg, Reliabilty reliabilty = Reliabilty.Reliable) {
    foreach (var id in _members.Keys) {
      _lobby.SendNetworkMessage(id, msg, reliabilty: reliabilty);
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
