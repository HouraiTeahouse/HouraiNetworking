using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public class LobbyMemberMap {

  readonly Dictionary<AccountHandle, LobbyMember> _players;
  readonly LobbyBase _lobby;

  public LobbyMemberMap(LobbyBase lobby) {
    _players = new Dictionary<AccountHandle, LobbyMember>();
    _lobby = lobby;
  }

  public LobbyMember Get(AccountHandle handle) {
    LobbyMember player;
    if (!_players.TryGetValue(handle, out player)) {
      player = new LobbyMember(_lobby, handle);
      _players.Add(handle, player);
    }
    return player;
  }

  public bool Remove(AccountHandle handle) => _players.Remove(handle);

  public void Broadcast(byte[] msg, Reliabilty reliabilty = Reliabilty.Reliable) {
    foreach (var id in _players.Keys) {
      _lobby.SendNetworkMessage(id, msg, reliabilty: reliabilty);
    }
  }

}

}
