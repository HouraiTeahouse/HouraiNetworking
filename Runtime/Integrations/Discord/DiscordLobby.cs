using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;
using DiscordApp = Discord;

namespace HouraiTeahouse.Networking.Discord {

internal class DiscordLobby : Lobby {

  readonly DiscordIntegrationClient _integrationClient;
  readonly DiscordApp.LobbyManager _lobbyManager;
  readonly DiscordLobbyManager _manager;

  public override ulong Id => (ulong)_data.Id;
  public override LobbyType Type => _data.Type == DiscordApp.LobbyType.Public ? LobbyType.Public : LobbyType.Private;

  public override int Capacity {
    get => (int)_data.Capacity;
    set {
      GetUpdateTransaction().SetCapacity((uint)value);
      _data.Capacity = (uint)value;
    }
  }

  public override ulong UserId => _integrationClient.ActiveUser.Id;
  public override ulong OwnerId => (ulong)_data.OwnerId;
  public override bool IsLocked {
    get => _data.Locked;
    set {
      GetUpdateTransaction().SetLocked(value);
      _data.Locked = value;
    }
  }

  internal string Secret => _data.Secret;
  internal DiscordApp.Lobby _data;

  DiscordApp.LobbyTransaction? _updateTxn;
  Dictionary<long, DiscordApp.LobbyMemberTransaction> _memberUpdateTxns;

  public DiscordLobby(DiscordLobbyManager manager, DiscordApp.Lobby lobby) : base() {
    _data = lobby;
    _manager = manager;
    _lobbyManager = manager._lobbyManager;
    Debug.Log(_lobbyManager);
    _integrationClient = manager._integrationClient;

    _updateTxn = null;
    _memberUpdateTxns = null;
    Members.Refresh();
  }

  internal void Update(DiscordApp.Lobby data) {
    _data = data;
    DispatchUpdate();
  }

  public override int MemberCount => _lobbyManager.MemberCount(_data.Id);
  internal override IEnumerable<AccountHandle> GetMemberIds()  {
      var count = _lobbyManager.MemberCount(_data.Id);
      for (var i = 0; i < count; i++) {
          yield return (ulong)_lobbyManager.GetMemberUserId(_data.Id, i);
      }
  }

  public override string GetMetadata(string key) =>
    _lobbyManager.GetLobbyMetadataValue(_data.Id, key);

  public override void SetMetadata(string key, string value) => 
    GetUpdateTransaction().SetMetadata(key, value);

  public override void DeleteMetadata(string key) => 
    GetUpdateTransaction().DeleteMetadata(key);

  public override IReadOnlyDictionary<string, string> GetAllMetadata() {
      var metadata = new Dictionary<string, string>();
      var count = _lobbyManager.LobbyMetadataCount(_data.Id);
      for (var i = 0; i < count; i++) {
        var key = _lobbyManager.GetLobbyMetadataKey(_data.Id, i);
        var value = _lobbyManager.GetLobbyMetadataValue(_data.Id, key);
        metadata[key] = value;
      }
      return metadata;
  }

  internal override string GetMemberMetadata(AccountHandle handle, string key) =>
    _lobbyManager.GetMemberMetadataValue(_data.Id, (long)handle.Id, key);

  internal override void SetMemberMetadata(AccountHandle handle, string key, string value) =>
    GetMemberTransaction((long)handle.Id).SetMetadata(key, value);
  
  internal override void DeleteMemberMetadata(AccountHandle handle, string key) =>
    GetMemberTransaction((long)handle.Id).DeleteMetadata(key);

  public override Task Join() => _manager.JoinLobby(this);

  public override void Leave() => _manager.LeaveLobby(this);

  public override void Delete() =>
    _lobbyManager.DeleteLobby(_data.Id, DiscordUtility.LogIfError);

  public override void SendLobbyMessage(ReadOnlySpan<byte> msg) {
    _lobbyManager.SendLobbyMessage(_data.Id, msg.ToArray(), DiscordUtility.LogIfError);
  }

  public override void FlushChanges() {
    if (_updateTxn != null) {
      _lobbyManager.UpdateLobby(_data.Id, _updateTxn.Value, (result) => {
        if (result == DiscordApp.Result.Ok) return;
        Debug.LogError($"[Discord] Error while updating lobby ({_data.Id}): {result}");
      });
      _updateTxn = null;
    }
    if (_memberUpdateTxns?.Count > 0) {
      foreach (var kvp in _memberUpdateTxns) {
        var userId = kvp.Key;
        _lobbyManager.UpdateMember(_data.Id, userId, kvp.Value, (result) => {
          if (result == DiscordApp.Result.Ok) return;
          Debug.LogError($"[Discord] Error while updating lobby ({_data.Id}), member ({userId}): {result}");
        });
      }
      _memberUpdateTxns.Clear();
    }
  }

  internal override void SendNetworkMessage(AccountHandle target, ReadOnlySpan<byte> buffer,
                                            Reliability reliability = Reliability.Reliable) {
    _lobbyManager.SendNetworkMessage(_data.Id, (long)target.Id, (byte)reliability, buffer.ToArray());
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
