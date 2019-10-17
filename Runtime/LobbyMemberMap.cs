using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public class LobbyMemberMap : IEnumerable<LobbyMember> {

  readonly Dictionary<AccountHandle, LobbyMember> _players;
  readonly LobbyBase _lobby;

  public event Action<LobbyMember> OnMemberJoin;
  public event Action<LobbyMember> OnMemberLeave;

  public LobbyMemberMap(LobbyBase lobby) {
    _players = new Dictionary<AccountHandle, LobbyMember>();
    _lobby = lobby;
  }

  public LobbyMember Add(AccountHandle handle) {
    LobbyMember player;
    if (!_players.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _players.Add(handle, player);
      OnMemberJoin?.Invoke(player);
      return player;
    }
    throw new Exception("Cannot add a member that already exists in );
  }

  public LobbyMember Get(AccountHandle handle) => _players[handle];

  public LobbyMember GetOrAdd(AccountHandle handle) {
    LobbyMember player;
    if (!_players.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _players.Add(handle, player);
      OnMemberJoin?.Invoke(player);
    }
    return player;
  }

  public LobbyMember TryGetValue(AccountHandle handle, LobbyMember member) =>
    _players.TryGetValue(handle, out member);

  public bool Contains(AccountHandle handle) => _players.ContainsKey(handle);

  public bool Remove(AccountHandle handle) {
    LobbyMember player;
    if (_players.TryGetValue(handle, out player)) {
      _players.Remove(handle);
      OnMemberLeave?.Invoke(player);
      return true;
    }
    return false;
  }

  public IEnumerator<LobbyMember> GetEnumerator() => _players.Values.GetEnumerator();
  IEnumerator IEnumerable.GetEnumerator() => _players.Values.GetEnumerator();

  public void Broadcast(byte[] msg, Reliabilty reliabilty = Reliabilty.Reliable) {
    foreach (var id in _players.Keys) {
      _lobby.SendNetworkMessage(id, msg, reliabilty: reliabilty);
    }
  }

}

}
