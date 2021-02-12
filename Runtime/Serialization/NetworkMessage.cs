using HouraiTeahouse.Serialization;

namespace HouraiTeahouse.Networking {

public struct NetworkMessage {

  public readonly INetworkReciever Reciever;
  public FixedSizeDeserializer NetworkReader;

  public NetworkMessage(INetworkReciever reciever, FixedSizeDeserializer reader) {
    Reciever = reciever;
    NetworkReader = reader;
  }

  public T ReadAs<T>() where T : ISerializable, new() {
    var message = ObjectPool<T>.Shared.Rent();
    message.Deserialize(ref NetworkReader);
    return message;
  }

}

}