using System;

namespace HouraiTeahouse.Networking {

public class LobbyMember : IMetadataContainer, IDisposable {

  public AccountHandle Id { get; }
  public LobbyBase Lobby { get; }

  public event Action<byte[], uint> OnNetworkMessage;
  public event Action OnUpdate;

  public LobbyMember(LobbyBase lobby, AccountHandle userId) {
    Id = userId;
    Lobby = lobby;
  }

  public void SendNetworkMessage(byte[] msg,
                                 Reliabilty reliabilty = Reliabilty.Reliable) {
    Lobby.SendNetworkMessage(Id, msg, reliabilty: reliabilty);
  }

  public string GetMetadata(string key) => Lobby.GetMemberMetadata(Id, key);
  public void SetMetadata(string key, string value) => Lobby.SetMemberMetadata(Id, key, value);
  public void DeleteMetadata(string key) => Lobby.DeleteMemberMetadata(Id, key);

  public int GetMetadataCount() => Lobby.GetMemberMetadataCount(Id);
  public string GetKeyByIndex(int idx) => Lobby.GetMemberMetadataKey(Id, idx);

  internal void DispatchNetworkMessage(byte[] msg, int size = -1) {
    size = size < 0 ? msg.Length : size;
    OnNetworkMessage?.Invoke(msg, (uint)size);
  }

  internal void DispatchUpdate() => OnUpdate?.Invoke();

  public void Dispose() {
    OnNetworkMessage = null;
    OnUpdate = null;
  }

}

}
