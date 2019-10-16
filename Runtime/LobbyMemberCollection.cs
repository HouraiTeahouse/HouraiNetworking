namespace HouraiTeahouse.Networking {

public class LobbyMemberMap {

  readonly Dictionary<AccountHandle, LobbyMember> _players;
  readonly ILobby _lobby;

  public LobbyMemberMap(ILobby lobby) {
    _players = new Dictionary<AccountHandle, LobbyPlayer>();
    _lobby = lobby;
  }

  public LobbyPlayer Get(AccountHandle handle) {
    if (_players.TryGet(handle, out LobbyPlayer player)) {
      return player;
    }
    var player = new LobbyPlayer(_lobby, handle);
    _players.Add(handle, player);
    return player;
  }

  public bool Remove(AccountHandle handle) => _players.Remove(handle);

  public void Broadcast(byte[] msg, Reliabilty reliabilty = Reliabilty.Reliable) {
    foreach (var id kin _players.Keys) {
      _lobby.SendNetworkMessage(id, msg, reliabilty: reliabilty);
    }
  }

}

}
