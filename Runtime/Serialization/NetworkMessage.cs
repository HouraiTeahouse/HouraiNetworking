namespace HouraiTeahouse.Networking {

public struct NetworkMessage {

  public readonly INetworkReciever Reciever;
  public Deserializer NetworkReader;

  public NetworkMessage(INetworkReciever reciever, Deserializer reader) {
    Reciever = reciever;
    NetworkReader = reader;
  }

  public T ReadAs<T>() where T : INetworkSerializable, new() {
    var message = ObjectPool<T>.Shared.Rent();
    message.Deserialize(ref NetworkReader);
    return message;
  }

}

}