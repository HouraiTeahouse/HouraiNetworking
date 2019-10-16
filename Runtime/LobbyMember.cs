using System;

namespace HouraiTeahouse.Networking {

public class LobbyMember : IMetadataContainer {

  public AccountHandle Id { get; }
  public LobbyBase Lobby { get; }

  public event Action<byte[]> OnNetworkMessage;

  public LobbyMember(LobbyBase lobby, AccountHandle userId) {
    Id = userId;
    Lobby = lobby;
  }

  public void SendNetworkMessage(byte[] msg,
                                 Reliabilty reliabilty = Reliabilty.Reliable) {
    Lobby.SendNetworkMessage(Id, msg, reliabilty: reliabilty);
  }

  internal void DispatchNetworkMessage(byte[] msg) {
    OnNetworkMessage?.Invoke(msg);
  }

  public string GetMetadata(string key) => Lobby.GetMemberMetadata(Id, key);
  public void SetMetadata(string key, string value) => Lobby.SetMemberMetadata(Id, key, value);
  public void DeleteMetadata(string key) => Lobby.DeleteMemberMetadata(Id, key);

  public int GetMetadataCount() => Lobby.GetMemberMetadataCount(Id);
  public string GetKeyByIndex(int idx) => Lobby.GetMemberMetadataKey(Id, idx);

}

}
