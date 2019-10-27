using System;

namespace HouraiTeahouse.Networking {

public class LobbyMember : INetworkConnection, IMetadataContainer, IDisposable {

  public static IMessageProcessor MessageProcessor;

  static LobbyMember() {
    MessageProcessor = new LZFCompressor();
  }

  public enum ConnectionState {
    Connected,
    Interrupted,
    Disconnected
  }

  public AccountHandle Id { get; }
  public Lobby Lobby { get; }
  public ConnectionStats ConnectionStats => _stats;
  public ConnectionState State { get; private set; }

  public event NetworkMessageHandler OnNetworkMessage;
  public event Action OnUpdated;
  public event Action OnDisconnected;

  ConnectionStats _stats;

  public LobbyMember(Lobby lobby, AccountHandle userId) {
    Id = userId;
    Lobby = lobby;
    State = ConnectionState.Connected;
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

    _stats.PacketsSent++;
    _stats.BytesSent += (ulong)size;

    Lobby.SendNetworkMessage(Id, msg, size, reliability: reliability);
  }

  internal void DispatchNetworkMessage(byte[] msg, int size = -1) {
    size = size < 0 ? msg.Length : size;

    _stats.PacketsRecieved++;
    _stats.BytesRecieved += (ulong)size;

    if (MessageProcessor != null) {
      MessageProcessor.Unapply(ref msg, ref size);
    }
    OnNetworkMessage?.Invoke(msg, (uint)size);
  }

  internal void DispatchUpdate() => OnUpdated?.Invoke();

  internal void DispatchDisconnect() {
    if (State == ConnectionState.Disconnected) return;
    OnDisconnected?.Invoke();
    State = ConnectionState.Disconnected;
    Dispose();
  }

  public void Dispose() {
    OnNetworkMessage = null;
    OnUpdated = null;
    OnDisconnected = null;
  }

  public override string ToString() =>
    $"LobbyMember(Lobby: {Lobby.Id}, Id: {Id})";

}

}
