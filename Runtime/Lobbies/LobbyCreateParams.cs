using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public enum LobbyType {
  Private = 0,
  Public = 1
}

/// <summary>
/// A set of parameters for creating new lobbies.
/// </summary>
public struct LobbyCreateParams {
  /// <summary>
  /// The type of the lobby to create: Public or Private.
  /// </summary>
  public LobbyType Type;

  /// <summary>
  /// The maximum number of members that can join the lobby.
  /// </summary>
  public uint Capacity;

  /// <summary>
  /// An initial set of metadata to set. Keys must be string.
  /// Values will be converted to strings via ToString().
  /// Does nothing if null.
  /// </summary>
  public IDictionary<string, object> Metadata;
}

}
