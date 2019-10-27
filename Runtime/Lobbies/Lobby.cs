using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace HouraiTeahouse.Networking  {

public interface IMetadataContainer {

  string GetMetadata(string key);
  void SetMetadata(string key, string value);
  void DeleteMetadata(string key);

  // Returns -1 if the container does not support iteration.
  int GetMetadataCount();
  // Returns null if the container does not support iteration.
  string GetKeyByIndex(int idx);

}

/// <summary>
/// A matchmaking lobby.
/// </summary>
public abstract class Lobby : INetworkSender, IMetadataContainer, IDisposable {

  /// <summary>
  /// Getas an identifier for the lobby that is unique within the underlying
  /// integration.
  ///
  /// May not be unique from integration to integration.
  /// </summary>
  public abstract ulong Id { get; }

  /// <summary>
  /// Gets the type of the lobby. Generally any search results will come back
  /// with only public lobbies.
  /// </summary>
  public abstract LobbyType Type { get; }

  /// <summary>
  /// Gets the ID of the owner of the lobby.
  /// </summary>
  public abstract ulong OwnerId { get; }

  /// <summary>
  /// Gets the ID of the current user.
  ///
  /// Note that this may be set to a non-zero value even if the current user is
  /// not currently connected to the lobby.
  /// </summary>
  public abstract ulong UserId { get; }

  /// <summary>
  /// Gets the number of members currently in the lobby.
  /// </summary>
  public abstract int MemberCount { get; }

  /// <summary>
  /// Gets the maximum number of members lobby can suppoprt.
  /// </summary>
  public abstract int Capacity { get; set; }

  /// <summary>
  /// Gets or sets whether the lobby can accept new joins.
  ///
  /// Some integraations may not support this functionality.
  /// </summary>
  public virtual bool IsLocked {
    get => false;
    set => throw new NotSupportedException();
  }

  /// <summary>
  /// Gets the aggregate connection statistics for all connections acros each
  /// user in the lobby.
  /// </summary>
  public ConnectionStats ConnectionStats {
    get {
      var stats = new ConnectionStats();
      foreach (var member in Members) {
        stats += member.ConnectionStats;
      }
      return stats;
    }
  }

  /// <summary>
  /// Event fired whenever a new member joins the lobby.
  /// </summary>
  public event Action<LobbyMember> OnMemberJoin {
    add => Members.OnMemberJoin += value;
    remove => Members.OnMemberJoin -= value;
  }

  /// <summary>
  /// Event fired when a member leaves the lobby.
  ///
  /// This may include the currently logged in user.
  /// </summary>
  public event Action<LobbyMember> OnMemberLeave {
    add => Members.OnMemberLeave += value;
    remove => Members.OnMemberLeave -= value;
  }

  /// <summary>
  /// Event fired whenever the lobby's metadata or settings are changed.
  /// This is not fired when a member is updated. See OnMemberUpdated instead.
  /// </summary>
  public event Action OnUpdated;

  /// <summary>
  /// Event fired when the lobby is deleted.
  /// </summary>
  public event Action OnDeleted;

  /// <summary>
  /// Event fired upon recieving a lobby-level message from another user.
  /// </summary>
  public event Action<LobbyMember, byte[], uint> OnLobbyMessage;

  /// <summary>
  /// Event fired upon recieving a directed message from a given user.
  /// </summary>
  public event Action<LobbyMember, byte[], uint> OnNetworkMessage;

  /// <summary>
  /// Event fired each time a member has been updated.
  /// </summary>
  public event Action<LobbyMember> OnMemberUpdated;

  internal abstract ulong GetMemberId(int idx);

  /// <summary>
  /// A read-only map of members currently in the lobby.
  /// </summary>
  public LobbyMemberMap Members { get; }

  protected Lobby() {
    Members = new LobbyMemberMap(this);
    SetupListeners();
  }

  /// <summary>
  /// Checks if the lobby is currently joinable.
  /// </summary>
  public bool IsJoinable => !IsLocked && MemberCount < Capacity;

  /// <summary>
  /// Joins the lobby as the currently logged in user.
  ///
  /// Note that some integrations may have limitations on how many lobbies a
  /// given user is in at a single time.
  /// </summary>
  public abstract Task Join();

  /// <summary>
  /// Leaves the loby if the current user is already in it.
  /// </summary>
  public abstract void Leave();

  /// <summary>
  /// Deletes the lobby, closing all connections and kicking all others connected
  /// to it. This can only be called by the owner of the lobby.
  /// </summary>
  public abstract void Delete();

  /// <summary>
  /// Gets the lobby metadata value.
  /// </summary>
  /// <param name="key">the metadata key</param>
  /// <returns>the metadata value, may return null or empty strings if not set</returns>
  public abstract string GetMetadata(string key);

  /// <summary>
  /// Sets the metadata for the lobby.
  /// Generally requires being the owner of the lobby.
  /// </summary>
  /// <param name="key">the key of the metadata</param>
  /// <param name="value">the value of the metadata</param>
  public abstract void SetMetadata(string key, string value);

  /// <summary>
  /// Deletes a metadata entry from the lobby.
  /// </summary>
  /// <param name="key">the key of the metadata to delete from</param>
  public abstract void DeleteMetadata(string key);

  /// <summary>
  /// Gets the number of metadata entries on the lobby.
  /// This usually precedes calling GetKeyByIndex.
  /// </summary>
  /// <returns>the number of metadata elements in the lobby.</returns>
  public abstract int GetMetadataCount();

  /// <summary>
  /// Gets a metadata key by it's index.
  /// 
  /// GetMetadataCount must be called first.
  /// </summary>
  /// <param name="idx">the index to fetch the key for</param>
  /// <returns>the key for the index, or null/empty if out of range.</returns>
  public abstract string GetKeyByIndex(int idx);

  internal virtual string GetMemberMetadata(AccountHandle handle, string key) =>
    throw new NotSupportedException();
  internal virtual void SetMemberMetadata(AccountHandle handle, string key, string value) =>
    throw new NotSupportedException();
  internal virtual void DeleteMemberMetadata(AccountHandle handle, string key) =>
    throw new NotSupportedException();

  internal virtual int GetMemberMetadataCount(AccountHandle handle) =>
    throw new NotSupportedException();
  internal virtual string GetMemberMetadataKey(AccountHandle handle, int idx) =>
    throw new NotSupportedException();

  /// <summary>
  /// Sends a lobby level message to all other users in the lobby.
  /// </summary>
  /// <param name="msg">the buffer of the sent message</param>
  /// <param name="size">the size of the message, uses the size of the buffer if negative.</param>
  public abstract void SendLobbyMessage(byte[] msg, int size = -1);

  /// <summary>
  /// Flushes metadata changes to the network. If not called, changes 
  /// may be delayed. Manual flushing may not be available for all backends.
  /// </summary>
  public virtual void FlushChanges() {}

  internal abstract void SendNetworkMessage(AccountHandle handle, byte[] msg, int size = -1,
                                          Reliability reliability = Reliability.Reliable);

  internal void DispatchDelete() => OnDeleted?.Invoke();
  internal void DispatchLobbyMessage(LobbyMember handle, byte[] msg, uint size) =>
    OnLobbyMessage?.Invoke(handle, msg, size);
  internal void DispatchUpdate() => OnUpdated?.Invoke();

  void INetworkSender.SendMessage(byte[] msg, int size, Reliability reliability) =>
    SendLobbyMessage(msg, size);

  public virtual void Dispose() {
    Members.Dispose();
    OnUpdated = null;
    OnDeleted = null;
    OnNetworkMessage = null;
    OnMemberUpdated = null;
    SetupListeners();
  }

  void SetupListeners() {
    OnMemberJoin += (member) => {
      member.OnNetworkMessage += (buf, size) => {
        OnNetworkMessage?.Invoke(member, buf, size);
      };
      member.OnUpdated += () => OnMemberUpdated?.Invoke(member);
    };
    OnMemberLeave += (member) => member.DispatchDisconnect();
  }

}

public static class IMetadataContainerExtensions {

  public static Dictionary<string, string> GetAllMetadata(this IMetadataContainer container) {
    var count = container.GetMetadataCount();
    if (count < 0) {
      throw new ArgumentException("Metadata container does not support iteration");
    }
    var output = new Dictionary<string, string>();
    for (var i = 0; i < count; i++) {
      string key =  container.GetKeyByIndex(i);
      output[key] = container.GetMetadata(key);
    }
    return output;
  }

}

}
