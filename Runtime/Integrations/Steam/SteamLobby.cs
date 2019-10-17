using Steamworks;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace HouraiTeahouse.Networking.Steam {

public class SteamLobby : LobbyBase {

  // Dummy empty message to kick off connections
  static readonly byte[] kEmptyMessage = new byte[0];

  readonly CSteamID _id;
  public override ulong Id => _id.m_SteamID;
  // Steam lobbies have no way of querying this information. Assume all of them
  // to be public.
  public override LobbyType Type => LobbyType.Public;
  public override ulong OwnerId => SteamMatchmaking.GetLobbyOwner(_id).m_SteamID;
  public override uint Capacity => (uint)SteamMatchmaking.GetLobbyMemberLimit(_id);
  // Steam lobbies cannot be locked
  public override bool IsLocked => false;

  public SteamLobby(CSteamID id) => _id = id;

  protected override int MemberCount =>
    SteamMatchmaking.GetNumLobbyMembers(_id);
  protected override ulong GetMemberId(int idx) =>
    SteamMatchmaking.GetLobbyMemberByIndex(_id, idx).m_SteamID;

  public override string GetMetadata(string key) =>
    SteamMatchmaking.GetLobbyData(_id, key);

  public override string GetKeyByIndex(int idx) {
    string key, value;
    SteamMatchmaking.GetLobbyDataByIndex(_id, idx, 
      out key, Constants.k_nMaxLobbyKeyLength, 
      out value, Constants.k_cubChatMetadataMax);
    return key;
  }

  public override void SetMetadata(string key, string value) =>
    SteamMatchmaking.SetLobbyData(_id, key, value);

  public override void DeleteMetadata(string key) =>
    SteamMatchmaking.DeleteLobbyData(_id, key);

  public override int GetMetadataCount() =>
    SteamMatchmaking.GetLobbyDataCount(_id);

  public override async Task Join() {
    var entry = await SteamMatchmaking.JoinLobby(_id).ToTask<LobbyEnter_t>();
    foreach (var member in Members) {
      member.SendMessage(kEmptyMessage);
    }
  }

  public override void Leave() => SteamMatchmaking.LeaveLobby(_id);

  public override void Delete() {
    // Steam lobbies actually cannot be deleted and are auto deleted when all
    // members leave them.
    // TODO(james7132): Find a clean way to force close a lobby.
    Leave();
  }

  public override void SendNetworkMessage(AccountHandle target, byte[] msg, int size = -1,
                                          Reliabilty reliabilty = Reliabilty.Reliable) {
    var userId = new CSteamID(target.Id);
    var type = reliabilty == Reliabilty.Reliable ? EP2PSend.k_EP2PSendReliable : EP2PSend.k_EP2PSendUnreliable;
    size = size < 0 ? msg.Length : size;
    if (!SteamNetworking.SendP2PPacket(userId, msg, (uint)size, type)) {
      Debug.LogError($"Failed to send Steam P2P Packet to {userId}");
    }
  }

  public override void SendLobbyMessage(byte[] msg, int size = -1) {
    size = size < 0 ? msg.Length : size;
    if (!SteamMatchmaking.SendLobbyChatMsg(_id, msg, size)) {
      Debug.LogError($"Failed to send Steam Lobby Packet.");
    }
  }

}

}
