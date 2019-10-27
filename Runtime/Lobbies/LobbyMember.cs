using System;

namespace HouraiTeahouse.Networking {

/// <summary>
/// An member within a lobby.
/// Could be local or remote.
/// </summary>
public class LobbyMember : INetworkConnection, IMetadataContainer, IDisposable {

  /// <summary>
  /// A global processor for all messages sent to and recieved from users.
  /// </summary>
  public static IMessageProcessor MessageProcessor;

  static LobbyMember() {
    MessageProcessor = new LZFCompressor();
  }

  public enum ConnectionState {
    Connected,
    Interrupted,
    Disconnected
  }

  /// <summary>
  /// The account ID of the member.
  /// </summary>
  public AccountHandle Id { get; }

  /// <summary>
  /// The lobby the member belongs to.
  /// </summary>
  public Lobby Lobby { get; }

  /// <summary>
  /// Gets the underlying connection statistics with this
  /// user.
  /// </summary>
  public ConnectionStats ConnectionStats => _stats;

  /// <summary>
  /// The state of the connection with this user.
  /// </summary>
  public ConnectionState State { get; private set; }

  /// <summary>
  /// An event fired upon recieving a network message from the member.
  /// </summary>
  public event NetworkMessageHandler OnNetworkMessage;

  /// <summary>
  /// An event when the member's state has been updated (i.e. metadata)
  /// </summary>
  public event Action OnUpdated;

  /// <summary>
  /// An event fired when the member has disconnected from the lobby.
  /// </summary>
  public event Action OnDisconnected;

  ConnectionStats _stats;

  internal LobbyMember(Lobby lobby, AccountHandle userId) {
    Id = userId;
    Lobby = lobby;
    State = ConnectionState.Connected;
  }

  /// <summary>
  /// Gets the member metadata value.
  /// </summary>
  /// <param name="key">the metadata key</param>
  /// <returns>the metadata value, may return null or empty strings if not set</returns>
  public string GetMetadata(string key) => Lobby.GetMemberMetadata(Id, key);

  /// <summary>
  /// Sets the metadata for the member.
  /// Generally requires being that user.
  /// </summary>
  /// <param name="key">the key of the metadata</param>
  /// <param name="value">the value of the metadata</param>
  public void SetMetadata(string key, string value) => Lobby.SetMemberMetadata(Id, key, value);

  /// <summary>
  /// Deletes a metadata entry from the member.
  /// </summary>
  /// <param name="key">the key of the metadata to delete from</param>
  public void DeleteMetadata(string key) => Lobby.DeleteMemberMetadata(Id, key);

  /// <summary>
  /// Gets the number of metadata entries on the member.
  /// This usually precedes calling GetKeyByIndex.
  /// </summary>
  /// <returns>the number of metadata elements in the member.</returns>
  public int GetMetadataCount() => Lobby.GetMemberMetadataCount(Id);

  /// <summary>
  /// Gets a metadata key by it's index.
  /// 
  /// GetMetadataCount must be called first.
  /// </summary>
  /// <param name="idx">the index to fetch the key for</param>
  /// <returns>the key for the index, or null/empty if out of range.</returns>
  public string GetKeyByIndex(int idx) => Lobby.GetMemberMetadataKey(Id, idx);

  /// <summary>
  /// Sends a message to the user over the network.
  /// 
  /// In general, the msg buffer does not need to live longer than the 
  /// duration of the call. It's contents will be copied.
  /// </summary>
  /// <param name="msg">the buffer of the message</param>
  /// <param name="size">the size of the message, uses the size of the buffer if negative.</param>
  /// <param name="reliability">does the message need to be reliably sent</param>
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
    State = ConnectionState.Disconnected;
  }

  public override string ToString() =>
    $"LobbyMember(Lobby: {Lobby.Id}, Id: {Id})";

}

}
