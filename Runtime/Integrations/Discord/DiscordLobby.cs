using System;
using System.Threading.Tasks;
using DiscordApp = Discord;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Discord {

public class DiscordLobby : LobbyBase {

  readonly DiscordIntegrationClient _integrationClient;
  readonly DiscordApp.LobbyManager _lobbyManager;
  public override ulong Id => (ulong)_data.Id;
  public override LobbyType Type => _data.Type == DiscordApp.LobbyType.Public ? LobbyType.Public : LobbyType.Private;
  public override uint Capacity => _data.Capacity;
  public override ulong UserId => _integrationClient.ActiveUser.Id;
  public override ulong OwnerId => (ulong)_data.OwnerId;
  public override bool IsLocked => _data.Locked;

  DiscordApp.Lobby _data;

  public DiscordLobby(DiscordLobbyManager manager, DiscordApp.Lobby lobby) : base() {
    _data = lobby;
    _lobbyManager = manager._lobbyManager;
    _integrationClient = manager._integrationClient;
  }

  internal void Update(DiscordApp.Lobby data) {
    _data = data;
    DispatchUpdate();
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

  public override Task Join() {
    var future = new TaskCompletionSource<bool>();
    // TODO(james7132): Make sure this works
    _lobbyManager.ConnectLobby(_data.Id, _data.Secret, (DiscordApp.Result result, ref DiscordApp.Lobby lobby) => {
      if (result != DiscordApp.Result.Ok) {
        // TODO(james7132): Implement
        return;
      }
      _data = lobby;
      _lobbyManager.ConnectNetwork(lobby.Id);
      _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliability.Reliable, true);
      _lobbyManager.OpenNetworkChannel(lobby.Id, (byte)Reliability.Unreliable, false);
    });
    return future.Task;
  }

  public override void Leave() {
    _lobbyManager.DisconnectNetwork(_data.Id);
    _lobbyManager.DisconnectLobby(_data.Id, DiscordUtility.LogIfError);
  }

  public override void Delete() {
    _lobbyManager.DeleteLobby(_data.Id, DiscordUtility.LogIfError);
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

  public override void SendLobbyMessage(byte[] msg, int size = -1) {
    if (size >= 0 && size != msg.Length) {
      var temp = new byte[size];
      Buffer.BlockCopy(msg, 0, temp, 0, size);
      msg = temp;
    }
    _lobbyManager.SendLobbyMessage(_data.Id, msg, DiscordUtility.LogIfError);
  }

}

}
