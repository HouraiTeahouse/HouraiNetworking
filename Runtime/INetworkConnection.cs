using System;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public delegate void NetworkMessageHandler(byte[] msg, uint size);

public interface INetworkSender {

    void SendMessage(byte[] msg, int size = -1,
                     Reliabilty reliabilty = Reliabilty.Reliable);

}

public interface INetworkReciever {
    event NetworkMessageHandler OnNetworkMessage;
}

public interface INetworkConnection : INetworkSender, INetworkReciever {
}

public static unsafe class INetworkConnectionExtensions {

  public static void Send<T>(this INetworkSender connection, byte header, 
                             in T message, Reliabilty reliablity = Reliabilty.Reliable) 
                             where T : INetworkSerializable {
    var buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var writer = Serializer.Create(buffer, (uint)SerializationConstants.kMaxMessageSize);
    writer.Write(header);
    message.Serialize(ref writer);
    connection.SendMessage(writer.ToArray(), writer.Position, reliablity);
    (message as IDisposable)?.Dispose();
  }

  public static void SendToAll<T, TConnection>(this IEnumerable<TConnection> connections, byte header,
                                               in T message, 
                                               Reliabilty reliablity = Reliabilty.Reliable) 
                                               where T : INetworkSerializable
                                               where TConnection : INetworkSender {
    var buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var writer = Serializer.Create(buffer, (uint)SerializationConstants.kMaxMessageSize);
    writer.Write(header);
    message.Serialize(ref writer);
    var bufferSize = writer.Position;
    var buf = writer.ToArray();
    foreach (var connection in connections) {
      connection.SendMessage(buf, writer.Position, reliablity);
    }
    (message as IDisposable)?.Dispose();
  }

}

}