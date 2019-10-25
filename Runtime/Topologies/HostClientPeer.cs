namespace HouraiTeahouse.Networking.Topologies {

/// <summary>
/// A network topology that establishes the owner of the lobby as the
/// authoritative host.
/// </summary>
/// <remarks>
/// Under this toplogy, the owner of the lobby will accept any message, and
/// clients will only accept messages from
///
/// Note that while there is always be at most one owner of the lobby acting as
/// host, each member of the lobby's status as client or host is not static. In
/// the case of a host disconnecting or intentionally leaving the lobby, some
/// backends will transfer ownership to another member of the lobby. Concrete
/// implementations of this class must be able ot handle these situations.
///
/// Lobby behavior may also be backend specific. If the backend does not support
/// lobby owners leaving without destroying the lobby, host migration will not
/// be supported.
/// </remarks>
public abstract class HostClientPeer : Peer {

  LobbyMember _lastSeenHost;

  public bool IsHost => Lobby.Members.Me == Lobby.Members.Owner;

  protected HostClientPeer(Lobby lobby) : base(lobby) {
    _lastSeenHost = Lobby.Members.Owner;

    Lobby.OnUpdated += OnLobbyUpdated;
  }

  /// <inheritdoc/>
  protected override void InitConnection(LobbyMember member) {
    if (!IsHost && member != Lobby.Members.Owner) return;
    MessageHandlers.Listen(member);
  }

  /// <inheritdoc/>
  protected override void DestroyConnection(LobbyMember member) =>
    MessageHandlers.StopListening(member);

  /// <summary>
  /// Event handler method for when the lobby ownership has changed.
  /// </summary>
  protected abstract void OnHostChanged(LobbyMember host);

  /// <summary>
  /// Sends a message as a client to the host. If the current user is the host,
  /// this call does nothing.
  /// </summary>
  protected void ClientSend<T>(in T msg,
                               Reliability reliability = Reliability.Reliable)
                               where T : INetworkSerializable {
    if (IsHost) return;
    MessageHandlers.Send<T>(Lobby.Members.Owner, msg, reliability);
  }

  public override void Dispose() {
    base.Dispose();
    Lobby.OnUpdated -= OnLobbyUpdated;
  }

  // Callbacks

  void OnLobbyUpdated() {
    if (_lastSeenHost == Lobby.Members.Owner) return;
    _lastSeenHost = Lobby.Members.Owner;
    // Restructure the listeners so that the topology holds.
    foreach(var member in Lobby.Members) {
      if (IsHost || member == Lobby.Members.Owner) {
        if (!MessageHandlers.IsListening(member)) {
          MessageHandlers.Listen(member);
        }
      } else {
        if (MessageHandlers.IsListening(member)) {
          MessageHandlers.StopListening(member);
        }
      }
    }
    OnHostChanged(Lobby.Members.Owner);
  }

}

}
