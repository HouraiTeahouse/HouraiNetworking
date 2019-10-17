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

public static class INetworkConnectionExtensions {

  public static void Send<T>(this INetworkSender connection, byte header, 
                             in T message, Reliabilty reliablity = Reliabilty.Reliable) 
                             where T : INetworkSerializable {
    var writer = new Serializer();
    writer.Write(header);
    message.Serialize(ref writer);
    connection.SendMessage(writer.AsArray(), writer.Position, reliablity);
    (message as IDisposable)?.Dispose();
    writer.Dispose();
  }

  public static void SendToAll<T, TConnection>(this IEnumerable<TConnection> connections, byte header,
                                               in T message, 
                                               Reliabilty reliablity = Reliabilty.Reliable) 
                                               where T : INetworkSerializable
                                               where TConnection : INetworkSender {
    var writer = new Serializer();
    writer.Write(header);
    message.Serialize(ref writer);
    var bufferSize = writer.Position;
    var buffer = writer.AsArray();
    foreach (var connection in connections) {
      connection.SendMessage(buffer, bufferSize, reliablity);
    }
    (message as IDisposable)?.Dispose();
    writer.Dispose();
  }

}

}