using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking.Steam {

internal class SteamLobby : Lobby {

  // Dummy empty message to kick off connections
  static readonly byte[] kEmptyMessage = new byte[0];

  internal Steamworks.Data.Lobby _lobby;
  public override ulong Id => _lobby.Id;
  // Steam lobbies have no way of querying this information. Assume all of them
  // to be public.
  public override LobbyType Type => LobbyType.Public;
  public override ulong OwnerId =>_lobby.Owner.Id;
  public override ulong UserId => SteamClient.SteamId;

  public override int Capacity {
    get => _lobby.MaxMembers;
    set => _lobby.MaxMembers = value;
  }

  readonly SteamLobbyManager _manager;

  public SteamLobby(Steamworks.Data.Lobby lobby, SteamLobbyManager manager, bool connected = false) : base() {
    Assert.IsNotNull(manager);
    _manager = manager;
    _lobby = lobby;
    if (connected)
    { 
        Members.Refresh();
    }
  }

  public override int MemberCount => _lobby.MemberCount;

  internal override IEnumerable<AccountHandle> GetMemberIds() {
    foreach (var member in _lobby.Members) {
        yield return (ulong)member.Id;
    }
  }

  public override string GetMetadata(string key) => _lobby.GetData(key);

  public override void SetMetadata(string key, string value) =>
    _lobby.SetData(key, value);

  public override void DeleteMetadata(string key) => _lobby.DeleteData(key);

  public override IReadOnlyDictionary<string, string> GetAllMetadata() {
      var metadata = new Dictionary<string, string>();
      foreach (var kvp  in _lobby.Data) {
          metadata[kvp.Key] = kvp.Value;
      }
      return metadata;
  }

  internal override string GetMemberMetadata(AccountHandle handle, string key) =>
    _lobby.GetMemberData(new Friend(handle.Id), key);

  internal override void SetMemberMetadata(AccountHandle handle, string key, string value) {
    if (handle.Id != UserId) {
      throw new InvalidOperationException("Cannnot set the metadata of a Steam lobby member other than the current user.");
    }
    _lobby.SetMemberData(key, value);
  }

  internal override void DeleteMemberMetadata(AccountHandle handle, string key) =>
    SetMemberMetadata(handle, key, string.Empty);

  public override Task Join() => _manager.JoinLobby(this);
  public override void Leave() => _manager.LeaveLobby(this);

  public override void Delete() {
    // Steam lobbies actually cannot be deleted and are auto deleted when all
    // members leave them.
    // TODO(james7132): Find a clean way to force close a lobby.
    Leave();
  }

  public override void FlushChanges() {
    Debug.LogWarning("[Steam] SteamLobby.FlushChanges(): Metadata changes are " +
                     "automatically flushed and cannot be manually flushed.");
  }

  internal override unsafe void SendNetworkMessage(AccountHandle target, ReadOnlySpan<byte> msg,
                                                   Reliability reliability = Reliability.Reliable) {
    var type = reliability == Reliability.Reliable ? P2PSend.Reliable : P2PSend.Unreliable;
    fixed (byte* ptr = msg) {
        if (!SteamNetworking.SendP2PPacket(target.Id, ptr, (uint)msg.Length, 0, type)) {
            Debug.LogError($"Failed to send Steam P2P Packet to {target.Id}");
        }
    }
  }

  public override unsafe void SendLobbyMessage(ReadOnlySpan<byte> msg) {
    throw new NotImplementedException("Steam Facepunch API no longer supports sending bytes");
  }

}

}
