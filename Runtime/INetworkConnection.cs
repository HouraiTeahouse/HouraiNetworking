using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public delegate void NetworkMessageHandler(FixedBuffer buffer);

public interface INetworkSender {

    void SendMessage(FixedBuffer buffer, Reliability reliability = Reliability.Reliable);

}

public interface INetworkReciever {
    event NetworkMessageHandler OnNetworkMessage;
}

public interface INetworkConnection : INetworkSender, INetworkReciever {
}

public static unsafe class INetworkConnectionExtensions {

  public static void Send<T>(this INetworkSender connection, byte header, 
                             in T message, Reliability reliablity = Reliability.Reliable) 
                             where T : INetworkSerializable {
    var buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var writer = Serializer.Create(buffer, (uint)SerializationConstants.kMaxMessageSize);
    writer.Write(header);
    message.Serialize(ref writer);
    connection.SendMessage(writer.ToFixedBuffer(), reliablity);
    (message as IDisposable)?.Dispose();
  }

  public static void SendToAll<T, TConnection>(this IEnumerable<TConnection> connections, byte header,
                                               in T message, 
                                               Reliability reliablity = Reliability.Reliable) 
                                               where T : INetworkSerializable
                                               where TConnection : INetworkSender {
    var buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var writer = Serializer.Create(buffer, (uint)SerializationConstants.kMaxMessageSize);
    writer.Write(header);
    message.Serialize(ref writer);
    foreach (var connection in connections) {
      connection.SendMessage(writer.ToFixedBuffer(), reliablity);
    }
    (message as IDisposable)?.Dispose();
  }

}

}