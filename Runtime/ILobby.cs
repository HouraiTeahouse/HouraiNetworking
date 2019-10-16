using System.Collections.Generic;

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

public abstract class LobbyBase : IMetadataContainer {

  public abstract ulong Id { get; }
  public abstract LobbyType Type { get; }
  public abstract ulong OwnerId { get; }
  public abstract uint Capacity { get; }
  public abstract bool IsLocked { get; }

  protected abstract int MemberCount { get; }
  protected abstract ulong GetMemberId(int idx);

  public LobbyMemberMap Members { get; }

  protected LobbyBase() {
    Members = new LobbyPlayerCollection(this);
    var count = MemberCount;
    for (int i = 0; i < memberCount; i++) {
      Members.Get(GetMemberId(i));
    }
  }

  public bool IsJoinable =>
    !lobby.IsLocked && lobby.MemberCount < lobby.Capacity;

  public abstract void Join();
  public abstract void Leave();
  public abstract void Delete();

  public abstract string GetMetadata(string key);
  public abstract void SetMetadata(string key, string value);
  public abstract void DeleteMetadata(string key);

  // Returns -1 if the container does not support iteration.
  public abstract int GetMetadataCount();
  // Returns null if the container does not support iteration.
  public abstract string GetKeyByIndex(int idx);

}

public static class IMetadataContainerExtensions {

  public static Dictionary<string, string> GetAllMetadata(this IMetadataContainer container) {
    var count = container.GetMetadataCount();
    if (count < 0) {
      throw new ArgumentException("Metadata container does not support iteration");
    }
    var output = new Dictionary<string, string>()
    for (var i = 0; i < count; i++) {
      string key =  container.GetKeyByIndex(i);
      output[key] = container.GetMetadata(key);
    }
    return output;
  }

}

}
