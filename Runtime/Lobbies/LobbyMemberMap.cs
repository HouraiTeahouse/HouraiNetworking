using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking {

public sealed class LobbyMemberMap : IEnumerable<LobbyMember>, IDisposable {

  readonly Dictionary<AccountHandle, LobbyMember> _members;
  readonly Lobby _lobby;

  /// <summary>
  /// Gets the LobbyMember for the currently logged in user.
  /// Will be null if the user is not connected to the lobby.
  /// </summary>
  public LobbyMember Me => Get(new AccountHandle(_lobby.UserId));

  /// <summary>
  /// Gets the LobbyMember who currently owns the lobby.
  /// Might be null if the user is not connected to the lobby.
  /// </summary>
  public LobbyMember Owner => Get(new AccountHandle(_lobby.OwnerId));

  internal event Action<LobbyMember> OnMemberJoin;
  internal event Action<LobbyMember> OnMemberLeave;

  internal LobbyMemberMap(Lobby lobby) {
    _members = new Dictionary<AccountHandle, LobbyMember>();
    _lobby = lobby;
  }

  internal int Count => _members.Count;

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
    if (_members.TryGetValue(handle, out LobbyMember member)) {
      _members.Remove(handle);
      OnMemberLeave?.Invoke(member);
      member.Dispose();
      return true;
    }
    return false;
  }

  internal void Clear() {
    foreach (var member in _members.Values) {
      OnMemberLeave?.Invoke(member);
      member.Dispose();
    }
    _members.Clear();
  }

  internal void Refresh() {
    if (_members.Count > 0) Clear();
    foreach (var id in _lobby.GetMemberIds()) {
      // Use GetOrAdd in the case the underlying implemenation accidentally
      // returns multiple of the same member.
      GetOrAdd(id);
    }
    Assert.IsTrue(_members.Count == _lobby.MemberCount);
  }

  // Read-only accessors are public

  /// <summary>
  /// Attempts to get the member 
  /// </summary>
  /// <param name="handle">the user account handle.</param>
  /// <param name="member"></param>
  /// <returns></returns>
  public bool TryGetValue(AccountHandle handle, out LobbyMember member) =>
    _members.TryGetValue(handle, out member);

  /// <summary>
  /// Gets a user from the member map by their ID.
  /// </summary>
  /// <param name="handle">the user account handle.</param>
  /// <returns>the LobbyMember with the ID, or null if they are not present.</returns>
  public LobbyMember Get(AccountHandle handle) {
    if (TryGetValue(handle, out LobbyMember owner)) {
      return owner;
    }
    return default(LobbyMember);
  }

  /// <summary>
  /// Checks if the a user is in the lobby.
  /// </summary>
  /// <param name="handle">the user account handle.</param>
  /// <returns>true if the member is in the lobby, false otherwise.</returns>
  public bool Contains(AccountHandle handle) => Get(handle) == default(LobbyMember);

  /// <summary>
  /// Broadcasts a network level message to all in the lobby.
  /// 
  /// Note: this is (usually) different form sending a lobby message
  /// which is also broadcast to all other remote users in the lobby.
  /// 
  /// In general, the msg buffer does not need to live longer than the 
  /// duration of the call. It's contents will be copied.
  /// </summary>
  /// <param name="msg">the buffer of the message</param>
  /// <param name="reliability">does the message need to be reliably sent</param>
  public void Broadcast(Span<byte> msg, Reliability reliability = Reliability.Reliable) {
    foreach (var member in _members.Values) {
      member.SendMessage(msg, reliability: reliability);
    }
  }

  public Dictionary<AccountHandle, LobbyMember>.ValueCollection.Enumerator
    GetEnumerator() => _members.Values.GetEnumerator();

  IEnumerator<LobbyMember> IEnumerable<LobbyMember>.GetEnumerator() => GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

  public void Dispose() {
    Clear();
    OnMemberJoin = null;
    OnMemberLeave = null;
  }

}

}
