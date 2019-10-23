using System;

namespace HouraiTeahouse.Networking {

public class LobbyMember : INetworkConnection, IMetadataContainer, IDisposable {

  public static IMessageProcessor MessageProcessor;

  static LobbyMember() {
    MessageProcessor = new LZFCompressor();
  }

  public AccountHandle Id { get; }
  public LobbyBase Lobby { get; }

  public event NetworkMessageHandler OnNetworkMessage;
  public event Action OnUpdate;

  public LobbyMember(LobbyBase lobby, AccountHandle userId) {
    Id = userId;
    Lobby = lobby;
  }

  public string GetMetadata(string key) => Lobby.GetMemberMetadata(Id, key);
  public void SetMetadata(string key, string value) => Lobby.SetMemberMetadata(Id, key, value);
  public void DeleteMetadata(string key) => Lobby.DeleteMemberMetadata(Id, key);

  public int GetMetadataCount() => Lobby.GetMemberMetadataCount(Id);
  public string GetKeyByIndex(int idx) => Lobby.GetMemberMetadataKey(Id, idx);

  public void SendMessage(byte[] msg, int size = -1,
                          Reliability reliability = Reliability.Reliable) {
    size = size < 0 ? msg.Length : size;
    if (MessageProcessor != null) {
      MessageProcessor.Apply(ref msg, ref size);
    }
    Lobby.SendNetworkMessage(Id, msg, size, reliability: reliability);
  }

  internal void DispatchNetworkMessage(byte[] msg, int size = -1) {
    size = size < 0 ? msg.Length : size;
    if (MessageProcessor != null) {
      MessageProcessor.Unapply(ref msg, ref size);
    }
    OnNetworkMessage?.Invoke(msg, (uint)size);
  }

  internal void DispatchUpdate() => OnUpdate?.Invoke();

  public void Dispose() {
    OnNetworkMessage = null;
    OnUpdate = null;
  }

}

}
