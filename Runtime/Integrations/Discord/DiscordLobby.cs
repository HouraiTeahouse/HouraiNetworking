using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordApp = Discord;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobby : Lobby {

  readonly DiscordIntegrationClient _integrationClient;
  readonly DiscordApp.LobbyManager _lobbyManager;
  readonly DiscordLobbyManager _manager;

  public override ulong Id => (ulong)_data.Id;
  public override LobbyType Type => _data.Type == DiscordApp.LobbyType.Public ? LobbyType.Public : LobbyType.Private;
  public override uint Capacity => _data.Capacity;
  public override ulong UserId => _integrationClient.ActiveUser.Id;
  public override ulong OwnerId => (ulong)_data.OwnerId;
  public override bool IsLocked {
    get => _data.Locked;
    set => GetUpdateTransaction().SetLocked(value);
  }

  internal string Secret => _data.Secret;
  internal DiscordApp.Lobby _data;

  DiscordApp.LobbyTransaction? _updateTxn;
  Dictionary<long, DiscordApp.LobbyMemberTransaction> _memberUpdateTxns;

  public DiscordLobby(DiscordLobbyManager manager, DiscordApp.Lobby lobby) : base() {
    _data = lobby;
    _manager = manager;
    _lobbyManager = manager._lobbyManager;
    _integrationClient = manager._integrationClient;

    _updateTxn = null;
    _memberUpdateTxns = null;
  }

  internal void Update(DiscordApp.Lobby data) {
    _data = data;
    DispatchUpdate();
  }

  public override int MemberCount => _lobbyManager.MemberCount((long)Id);
  protected override ulong GetMemberId(int idx) =>
    (ulong)_lobbyManager.GetMemberUserId(_data.Id, idx);

  public override string GetMetadata(string key) =>
    _lobbyManager.GetLobbyMetadataValue(_data.Id, key);

  public override string GetKeyByIndex(int idx) =>
    _lobbyManager.GetLobbyMetadataKey(_data.Id, idx);

  public override void SetMetadata(string key, string value) => 
    GetUpdateTransaction().SetMetadata(key, value);

  public override void DeleteMetadata(string key) => 
    GetUpdateTransaction().DeleteMetadata(key);

  public override string GetMemberMetadata(AccountHandle handle, string key) =>
    _lobbyManager.GetMemberMetadataValue(_data.Id, (long)handle.Id, key);

  public override void SetMemberMetadata(AccountHandle handle, string key, string value) =>
    GetMemberTransaction((long)handle.Id).SetMetadata(key, value);
  
  public override void DeleteMemberMetadata(AccountHandle handle, string key) =>
    GetMemberTransaction((long)handle.Id).DeleteMetadata(key);

  public override int GetMetadataCount() => _lobbyManager.LobbyMetadataCount(_data.Id);

  public override Task Join() => _manager.JoinLobby(this);

  public override void Leave() => _manager.LeaveLobby(this);

  public override void Delete() =>
    _lobbyManager.DeleteLobby(_data.Id, DiscordUtility.LogIfError);

  public override void SendLobbyMessage(byte[] msg, int size = -1) {
    if (size >= 0 && size != msg.Length) {
      var temp = new byte[size];
      Buffer.BlockCopy(msg, 0, temp, 0, size);
      msg = temp;
    }
    _lobbyManager.SendLobbyMessage(_data.Id, msg, DiscordUtility.LogIfError);
  }

  public override void FlushChanges() {
    if (_updateTxn != null) {
      _lobbyManager.UpdateLobby(_data.Id, _updateTxn.Value, (result) => {
        if (result == DiscordApp.Result.Ok) return;
        Debug.LogError($"[Discord] Error while updating lobby")
      });
      _updateTxn = null;
    }
    if (_memberUpdateTxns?.Count > 0) {
      foreach (var kvp in _memberUpdateTxns) {
        _lobbyManager.UpdateMember(_data.Id, kvp.Key, kvp.Value, (result) => {
          if (result == DiscordApp.Result.Ok) return;
          // TODO(james7132): Implement
        });
      }
      _memberUpdateTxns.Clear();
    }
  }

  internal override void SendNetworkMessage(AccountHandle target, byte[] msg, int size = -1,
                                          Reliability reliability = Reliability.Reliable) {
    Assert.IsTrue(size <= msg.Length);
    if (size >= 0 && size != msg.Length) {
      var temp = new byte[size];
      Buffer.BlockCopy(msg, 0, temp, 0, size);
      ArrayPool<byte>.Shared.Return(msg);
      msg = temp;
    }
    _lobbyManager.SendNetworkMessage(_data.Id, (long)target.Id, (byte)reliability, msg);
  }

  DiscordApp.LobbyTransaction GetUpdateTransaction() {
    _updateTxn = _updateTxn ?? _lobbyManager.GetLobbyUpdateTransaction(_data.Id);
    return _updateTxn.Value;
  }

  DiscordApp.LobbyMemberTransaction GetMemberTransaction(long userId) {
    _updateTxn = _updateTxn ?? _lobbyManager.GetLobbyUpdateTransaction(_data.Id);
    if (_memberUpdateTxns == null) {
      _memberUpdateTxns = new Dictionary<long, DiscordApp.LobbyMemberTransaction>();
    }
    DiscordApp.LobbyMemberTransaction txn;
    if (!_memberUpdateTxns.TryGetValue(userId, out txn)) {
      txn = _lobbyManager.GetMemberUpdateTransaction(_data.Id, userId);
      _memberUpdateTxns[userId] = txn;
    }
    return txn;
  }

}

}
