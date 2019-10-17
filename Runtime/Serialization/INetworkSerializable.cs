using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HouraiTeahouse.Networking {

public interface INetworkSerializable {

  void Serialize(ref Serializer serializer);
  void Deserialize(ref Deserializer deserializer);

}

public interface ISerializationContract<T> {

  void Serialize(ref T msg, ref Serializer serializer);
  void Deserialize(ref T msg, ref Deserializer serializer);

}

}