using System;
using System.Collections.Generic;
using HouraiTeahouse.Serialization;

namespace HouraiTeahouse.Networking {

public delegate void NetworkMessageHandler(ReadOnlySpan<byte> buffer);

public interface INetworkSender {

    void SendMessage(ReadOnlySpan<byte> msg, Reliability reliability = Reliability.Reliable);

}

public interface INetworkReciever {
    event NetworkMessageHandler OnNetworkMessage;
}

public interface INetworkConnection : INetworkSender, INetworkReciever {
}

public static unsafe class INetworkConnectionExtensions {

  public static void Send<T>(this INetworkSender connection, byte header, 
                             in T message, Reliability reliablity = Reliability.Reliable) 
                             where T : ISerializable {
    Span<byte> buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var serializer = FixedSizeSerializer.Create(buffer);
    serializer.Write(header);
    message.Serialize(ref serializer);
    connection.SendMessage(serializer.AsReadOnlySpan(), reliablity);
  }

  public static void SendToAll<T, TConnection>(this IEnumerable<TConnection> connections, byte header,
                                               in T message, 
                                               Reliability reliablity = Reliability.Reliable) 
                                               where T : ISerializable
                                               where TConnection : INetworkSender {
    Span<byte> buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var serializer = FixedSizeSerializer.Create(buffer);
    serializer.Write(header);
    message.Serialize(ref serializer);
    foreach (var connection in connections) {
      connection.SendMessage(serializer.AsReadOnlySpan(), reliablity);
    }
  }

}

}
