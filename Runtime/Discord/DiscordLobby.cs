using System;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobby : LobbyBase, IDisposable {

  readonly DiscordApp.LobbyManager _lobbyManager;
  public override ulong Id => (ulong)_data.Id;
  public override LobbyType Type => _data.Type == DiscordApp.LobbyType.Public ? LobbyType.Public : LobbyType.Private;
  public override uint Capacity => _data.Capacity;
  public override ulong OwnerId => (ulong)_data.OwnerId;
  public override bool IsLocked => _data.Locked;

  public event Action<LobbyMember> OnMemberJoin;
  public event Action<LobbyMember> OnMemberLeave;
  public event Action<AccountHandle, byte[]> OnNetworkMessage;

  DiscordApp.Lobby _data;

  public DiscordLobby(DiscordLobbyManager manager, DiscordApp.Lobby lobby) : base() {
    _data = lobby;
    _lobbyManager = manager._lobbyManager;
    _lobbyManager.OnMemberConnect += _OnMemberJoin;
    _lobbyManager.OnMemberDisconnect += _OnMemberLeave;
    _lobbyManager.OnNetworkMessage += _OnNetworkMessage;
  }

  public void Dispose() {
    if (_lobbyManager == null) return;
    _lobbyManager.OnMemberConnect -= _OnMemberJoin;
    _lobbyManager.OnMemberDisconnect -= _OnMemberLeave;
    _lobbyManager.OnNetworkMessage -= _OnNetworkMessage;
  }

  protected override int MemberCount => _lobbyManager.MemberCount((long)Id);
  protected override ulong GetMemberId(int idx) =>
    (ulong)_lobbyManager.GetMemberUserId(_data.Id, idx);

  public override string GetMetadata(string key) =>
    _lobbyManager.GetLobbyMetadataValue(_data.Id, key);

  public override string GetKeyByIndex(int idx) =>
    _lobbyManager.GetLobbyMetadataKey(_data.Id, idx);

  public override void SetMetadata(string key, string value) {
    var txn = _lobbyManager.GetLobbyUpdateTransaction(_data.Id);
    txn.SetMetadata(key, value);
    _lobbyManager.UpdateLobby(_data.Id, txn, (result) => {
      if (result == DiscordApp.Result.Ok) return;
      // TODO(james7132): Implement
    });
  }

  public override void DeleteMetadata(string key) {
    var txn = _lobbyManager.GetLobbyUpdateTransaction(_data.Id);
    txn.DeleteMetadata(key);
    _lobbyManager.UpdateLobby(_data.Id, txn, (result) => {
      if (result == DiscordApp.Result.Ok) return;
      // TODO(james7132): Implement
    });
  }

  public override int GetMetadataCount() => _lobbyManager.LobbyMetadataCount(_data.Id);

  public override void Join() {
    // TODO(james7132): Add secrets
    _lobbyManager.ConnectLobby(_data.Id, "", (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
      if (result != DiscordApp.Result.Ok) {
        // TODO(james7132): Implement
        return;
      }
      _data = lobby;
      _lobbyManager.ConnectNetwork(lobby.Id);
      _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliabilty.Reliable, true);
      _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliabilty.Unreliable, false);
    });
  }

  public override void Leave() {
    _lobbyManager.DisconnectNetwork(_data.Id);
    _lobbyManager.DisconnectLobby(_data.Id, (result) => {
      if (result == DiscordApp.Result.Ok) return;
      // TODO(james7132): Implement
    });
  }

  public override void Delete() {
    _lobbyManager.DeleteLobby(_data.Id, (result) => {
      if (result == DiscordApp.Result.Ok) {
        Dispose();
        return;
      }
      // TODO(james7132): Implement
    });
  }

  public override void SendNetworkMessage(AccountHandle target, byte[] msg,
                                          Reliabilty reliability = Reliabilty.Reliable) {
    _lobbyManager.SendNetworkMessage(_data.Id, (long)target.Id, (byte)reliability, msg);
  }

  // Event Handlers

  void _OnMemberJoin(long lobbyId, long userId) {
    if (lobbyId != _data.Id) return;
    var member = Members.Get(new AccountHandle((ulong)userId));
    OnMemberJoin?.Invoke(member);
  }

  void _OnMemberLeave(long lobbyId, long userId) {
    if (lobbyId != _data.Id) return;
    var account = new AccountHandle((ulong)userId);
    var member = Members.Get(account);
    Members.Remove(account);
    OnMemberLeave?.Invoke(member);
  }

  void _OnNetworkMessage(long lobbyId, long userId, byte _, byte[] message) {
    if (lobbyId != _data.Id) return;
    var account = new AccountHandle((ulong)userId);
    Members.Get(account).DispatchNetworkMessage(message);
    OnNetworkMessage?.Invoke(account, message);
  }

}

}
