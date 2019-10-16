using System;
using Discord;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobby : LobbyBase, IDisposable {

  readonly Discord.LobbyManager _lobbyManager;
  public ulong Id { get; }

  public event Action<LobbyMember> OnMemberJoin;
  public event Action<LobbyMember> OnMemberLeave;
  public event Action<AccountHandle, byte[]> OnNetworkMessage;

  public DiscordLobby(DiscordLobbyManager manager, uint id) : base() {
    Id = id;
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

  protected uint MemberCount => _lobbyManager.MemberCount(Id);
  protected ulong GetMemberId(int idx) =>
    _lobbyManager.GetMemberUserId(Id, i);

  public override string GetMetadata(string key) =>
    _lobbyManager.GetLobbyMetadataValue(Id, key);

  public override string GetMetadataByIndex(int idx) =>
    _lobbyManager.GetLobbyMetadataKey(Id, idx);

  public override void SetMetadata(string key, string value) {
    var txn = _lobbyManager.GetLobbyUpdateTransaction();
    txn.SetMetadata(key, value);
    _lobbyManager.UpdateLobby(Id, txn, (Discord.Result result, ref Discord.Lobby lobby) {
      if (result == Discord.Result.OK) return;
      // TODO(james7132): Implement
    });
  }

  public override string DeleteMetadata(string key) {
    var txn = _lobbyManager.GetLobbyUpdateTransaction();
    txn.DeleteMetadata(key);
    _lobbyManager.UpdateLobby(Id, txn, (Discord.Result result, ref Discord.Lobby lobby) {
      if (result == Discord.Result.OK) return;
      // TODO(james7132): Implement
    });
  }

  public override int GetMetadataCount() => _lobbyManager.LobbyMetadataCount(Id);

  public override void Join() {
    _lobbyManager.ConnectLobby(Id, txn, (Discord.Result result, ref Discord.Lobby lobby) {
      if (result == Discord.Result.OK) return;
      // TODO(james7132): Implement
      _lobbyManager.ConnectNetwork(lobby.id);
      _lobbyManager.OpenNetworkChannel(lobby.id, (byte)Reliabilty.Reliable, true);
      _lobbyManager.OpenNetworkChannel(lobby.id, (byte)Reliabilty.Unreliable, false);
    });
  }

  public override void Leave() {
    _lobbyManager.DisconectLobby(Id, txn, (Discord.Result result, ref Discord.Lobby lobby) {
      if (result == Discord.Result.OK) return;
      // TODO(james7132): Implement
    });
  }

  public override void Delete() {
    _lobbyManager.DeleteLobby(Id, (result) => {
      if (result == Discord.Result.OK) {
        Dispose();
        return;
      }
      // TODO(james7132): Implement
    })
  }

  public override void SendNetworkMessage(AccountHandle target, byte[] msg,
                                          Reliabilty reliability = Reliabilty.Reliable) {
    _lobbyManager.SendNetworkMessage(Id, target.Id, (byte)reliability, msg);
  }

  // Event Handlers

  void _OnMemberJoin(ulong lobbyId, ulong userId) {
    if (lobbyId != Id) return;
    var member = Members.Get(new AccountHandle(userId));
    OnMemberJoin?.Invoke(member);
  }

  void _OnMemberLeave(ulong lobbyId, ulong userId) {
    if (lobbyId != Id) return;
    var member = Members.Get(new AccountHandle(userId));
    Members.Remove(member);
    OnMemberLeave?.Invoke(member);
  }

  void _OnNetworkMessage(ulong lobbyId, ulong userId, byte _, byte[] message) {
    if (lobbyId != Id) return;
    var account = new AccountHandle(userId);
    Members.Get(account).DispatchNetworkMessage(
    OnNetworkMessage?.Invoke(account, message);
  }

}

}
