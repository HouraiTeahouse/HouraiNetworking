using Steamworks;

namespace HouraiTeahouse.Networking.Steam {

public class SteamLobby : LobbyBase {

  // Dummy empty message to kick off connections
  static readonly byte[] kEmptyMessage = new byte[0];

  readonly CSteamID _id;
  public ulong Id => _id.m_SteamID;
  // Steam lobbies have no way of querying this information. Assume all of them
  // to be public.
  public LobbyType Type => LobbyType.Public;
  public ulong OwnerId => SteamMatchmaking.GetLobbyOwner(Id).m_SteamID;
  public int Capacity => SteamMatchmaking.GetLobbyMemberLimit(Id);
  // Steam lobbies cannot be locked
  public bool IsLocked => false;

  public override event Action<AccountHandle, byte[]> OnNetworkMessage;

  public SteamLobby(CSteamID id) {
    Id = id;
  }

  internal void Update() {
  }

  protected override int MemberCount =>
    SteamMatchmaking.GetNumLobbyMembers(Id);
  protected override ulong GetMemberId(int idx) =>
    SteamMatchmaking.GetLobbymemberByIndex(_id, idx).m_SteamID;

  public override string GetMetadata(string key) =>
    SteamMatchmaking.GetLobbyData(Id, key);

  public override string GetKeyByIndex(int idx) =>
    SteamMatchmaking.GetLobbyDataByIndex(_data.Id, idx);

  public override void SetMetadata(string key, string value) =>
    SteamMatchmaking.SetLobbyData(_id, key, value);

  public override string DeleteMetadata(string key) =>
    SteamMatchmaking.DeleteLobbyData(_id, key);

  public override int GetMetadataCount() =>
    SteamMatchmaking.GetLobbyDataCount(_id);

  public async Task Join() {
t    var entry = await SteamMatchmaking.JoinLobby(_id).ToTask<LobbyEnter_t>();
    for (var member in members) {
      member.SendNetworkMessage(kEmptyMessage);
    }
  }

  public void Leave() => SteamMatchmaking.LeaveLobby(Id)

  public void Delete() {
    // Steam lobbies actually cannot be deleted and are auto deleted when all
    // members leave them.
    // TODO(james7132): Find a clean way to force close a lobby.
    Leave();
  }

  public override void SendNetworkMessage(AccountHandle target, byte[] msg,
                                          Reliabilty reliabilty = Reliabilty.Reliable) {
    SteamNetworking.
  }

}

}
