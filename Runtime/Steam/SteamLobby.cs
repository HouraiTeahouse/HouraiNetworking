using Steamworks;

namespace HouraiTeahouse.Networking.Steam {

public class SteamLobby : ILobby {

  public CSteamID Id { get; }
  public LobbyType Type { get; }
  public ulong OwnerId => SteamMatchmaking.GetLobbyOwner(Id).m_SteamID;
  public int Capacity => SteamMatchmaking.GetLobbyMemberLimit(Id);
  // Steam lobbies cannot be locked
  public bool IsLocked => false;

  public int MemberCount => SteamMatchmaking.GetNumLobbyMembers(Id);

  // Callbacks

  public SteamLobby(CSteamID id) {
    Id = id;
  }

  public LobbyMember GetMember(int idx) {
    CSteamID id = SteamMatchmaking.GetNumLobbyMemberByIndex(Id, idx);
    return new LobbyMember(this, id.m_SteamID);
  }

  public string GetMetadata(string key) =>
    SteamMatchmaking.GetLobbyData(Id, key);

  public void SetMetadata(string key, string value) =>
    SteamMatchmaking.SetLobbyMetadata(Id, key, value);

  public string DeleteMetadata(string key) =>
    SteamMatchmaking.DeleteLobbyData(Id, key);

  public int GetMetadataCount() =>
    SteamMatchmaking.GetLobbyDataCount(Id);

  public void Join() {
  }

  public void Leave() => SteamMatchmaking.LeaveLobby(Id)

  public void Delete() {
    // Steam lobbies actually cannot be deleted and are auto deleted when all
    // members leave them.
    // TODO(james7132): Find a clean way to force close a lobby.
    Leave();
  }

}

}
