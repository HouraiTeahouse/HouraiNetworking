using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking.Topologies {

/// <summary>
/// A higher level wrapper around an existing lobby that simplifies network
/// topology setup, and provides helper functions and callbacks for
/// serialization.
/// </summary>
public abstract class Peer : IDisposable {

  public readonly LobbyBase Lobby;
  protected readonly MessageHandlers MessageHandlers;

  protected Peer(LobbyBase lobby) {
    Lobby = lobby;
    MessageHandlers = new MessageHandlers();

    Lobby.OnNemberJoin += InitConnection;
    Lobby.OnMemberleave += DestoryConnection;

    foreach (var member in Lobby.Members) {
      InitConnection(member);
    }
  }

  /// <summary>
  /// Event handler for the start of a new connection. Fired upon joining for
  /// every already present lobby member and upon a new member joining.
  /// </summary>
  /// <remarks>
  /// In order to process messages from the peer, MessageHandlers.Listen must be
  /// called with the provided LobbyMember.
  /// </remarks>
  protected abstract void InitConnection(LobbyMember member);

  /// <summary>
  /// Event handler for the end of an existing connection. Fired upon lobby
  /// deletion for every present lobby member and upon a member disconnecting.
  /// </summary>
  /// <remarks>
  /// In order to stop processsing messages from the peer,
  /// MessageHandlers.StopListening must be called with the provided LobbyMember.
  /// </remarks>
  protected abstract void DestoryConnection(LobbyMember member);

  /// <summary>
  /// Helper method to ensure packets are being sent with the correct headers
  /// when sending a message.
  ///
  /// The type T must be registered in MessageHandlers before this method can be
  /// used.
  /// </summary>
  protected void Send<T>(INetworkSender sender, in T msg,
                         Reliability reliability = Reliability.Reliable)
                         where T : INetworkSerializable {
    MessageHandlers.Broadcast<T>(sender, msg, reliability);
  }

  /// <summary>
  /// Helper method to ensure packets are being sent with the correct headers
  /// when broadcasting a message to all members of the lobby.
  ///
  /// The type T must be registered in MessageHandlers before this method can be
  /// used.
  /// </summary>
  protected void Broadcast<T>(in T msg,
                              Reliability reliability = Reliability.Reliable)
                              where T : INetworkSerializable {
    MessageHandlers.Broadcast<T>(Lobby.Members, msg, reliability);
  }

  public virtual void Dispose() {
    foreach (var member in Lobby.Members) {
      DestroyConnection(member);
    }
    Lobby.OnNemberJoin -= InitConnectionImpl;
    Lobby.OnMemberleave -= DestoryConnectionImpl;
    MessageHandlers.Dispose();
  }

}

}
