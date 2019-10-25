namespace HouraiTeahouse.Networking.Topologies {

/// <summary>
/// A network topology that connects every pair of users in the lobby.
/// </summary>
public abstract class FullMeshPeer : Peer {

  protected FullMeshPeer(Lobby lobby) : base(lobby) {
  }

  protected override void InitConnection(LobbyMember member) =>
    MessageHandlers.Listen(member);

  protected override void DestroyConnection(LobbyMember member) =>
    MessageHandlers.StopListening(member);

}

}
