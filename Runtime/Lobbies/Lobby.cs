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
  public abstract int Capacity { get; }

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
  ///
  public event Action<LobbyMember> OnMemberUpdated;

  protected abstract ulong GetMemberId(int idx);

  /// <summary>
  /// A read-only map of members currently in the lobby.
  /// </summary>
  public LobbyMemberMap Members { get; }

  protected Lobby() {
    Members = new LobbyMemberMap(this);
    OnMemberJoin += (member) => {
      member.OnNetworkMessage += (buf, size) => {
        OnNetworkMessage?.Invoke(member, buf, size);
      };

      member.OnUpdated += () => OnMemberUpdated?.Invoke(member);
    };
    OnMemberLeave += (member) => member.DispatchDisconnect();
    var count = MemberCount;
    for (int i = 0; i < count; i++) {
      Members.GetOrAdd(GetMemberId(i));
    }
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

  public abstract string GetMetadata(string key);
  public abstract void SetMetadata(string key, string value);
  public abstract void DeleteMetadata(string key);

  public abstract int GetMetadataCount();
  public abstract string GetKeyByIndex(int idx);

  public virtual string GetMemberMetadata(AccountHandle handle, string key) =>
    throw new NotSupportedException();
  public virtual void SetMemberMetadata(AccountHandle handle, string key, string value) =>
    throw new NotSupportedException();
  public virtual void DeleteMemberMetadata(AccountHandle handle, string key) =>
    throw new NotSupportedException();

  public virtual int GetMemberMetadataCount(AccountHandle handle) =>
    throw new NotSupportedException();
  public virtual string GetMemberMetadataKey(AccountHandle handle, int idx) =>
    throw new NotSupportedException();

  public abstract void SendLobbyMessage(byte[] msg, int size = -1);

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
