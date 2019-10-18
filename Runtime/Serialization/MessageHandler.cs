using System;
using System.Collections;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public sealed class MessageHandlers : IDisposable {

  readonly Action<NetworkMessage>[] _handlers;
  readonly Dictionary<Type, byte> _headers;
  readonly Dictionary<INetworkReciever, NetworkMessageHandler> _recievers;

  public MessageHandlers() {
    _handlers = new Action<NetworkMessage>[byte.MaxValue];
    _headers = new Dictionary<Type, byte>();
    _recievers = new Dictionary<INetworkReciever, NetworkMessageHandler>();
  }

  public void RegisterHandler(byte code, Action<NetworkMessage> handler) {
    if (handler == null) return;
    _handlers[code] += handler;
  }

  public void RegisterHandler<T>(byte header, Action<T> handler) where T : INetworkSerializable, new() {
    if (handler == null) throw new ArgumentNullException(nameof(handler));
    if (_headers.TryGetValue(typeof(T), out byte storedHeader)) {
      if (storedHeader != header) {
        throw new Exception("Type {typeof(T)} is already registered with the header {storedHeader}");
      }
    }
    _headers[typeof(T)] = header;
    RegisterHandler(header, dataMsg => {
      var message = dataMsg.ReadAs<T>();
      handler(message);
      (message as IDisposable)?.Dispose();
      ObjectPool<T>.Shared.Return(message);
    });
  }

  public void Serialize<T>(in T msg, ref Serializer serializer) where T : INetworkSerializable {
    byte header;
    if (_headers.TryGetValue(typeof(T), out header)) {
      serializer.Write(header);
      msg.Serialize(ref serializer);
    } else {
      throw new Exception("Type {typeof(T)} is not registered.");
    }
  }

  public void Send<T>(INetworkSender sender, in T msg,
                      Reliabilty reliability = Reliabilty.Reliable) 
                      where T : INetworkSerializable {
    var serializer = Serializer.Create();
    Serialize<T>(msg, ref serializer);
    sender.SendMessage(serializer.AsArray(), serializer.Position, reliability);
    serializer.Dispose();
  }

  public void Broadcast<T>(IEnumerable<INetworkSender> senders, in T msg,
                           Reliabilty reliability = Reliabilty.Reliable) 
                           where T : INetworkSerializable {
    var serializer = Serializer.Create();
    Serialize<T>(msg, ref serializer);
    foreach (var sender in senders) {
      sender.SendMessage(serializer.AsArray(), serializer.Position,
                        reliability);
    }
    serializer.Dispose();
  }

  void ThrowIfNotRegistered<T>() {
    if (!_headers.ContainsKey(typeof(T))) {
      throw new Exception("Type {typeof(T)} is not registered.");
    }
  }

  public void ClearHandlers(byte code) {
    _handlers[code] = null;
    var keys = new List<Type>();
    foreach (var kvp in _headers) {
      if (kvp.Value != code) continue;
      keys.Add(kvp.Key);
    }
    foreach (var key in keys) {
      _headers.Remove(key);
    }
  }

  public void Listen(INetworkReciever reciever) {
    if (_recievers.ContainsKey(reciever)) return;
    NetworkMessageHandler callback = (msg, size) => {
      if (size <= 0) return;
      var deserializer = new Deserializer(msg);
      byte header = deserializer.ReadByte();
      _handlers[header]?.Invoke(new NetworkMessage(reciever, deserializer));
    };
    reciever.OnNetworkMessage += callback;
    _recievers[reciever] = callback;
  }

  public bool IsListening(INetworkReciever reciever) =>
    _recievers.ContainsKey(reciever);

  public void StopListening(INetworkReciever reciever) {
    NetworkMessageHandler handler;
    if (_recievers.TryGetValue(reciever, out handler)) {
      reciever.OnNetworkMessage -= handler;
      _recievers.Remove(reciever);
    }
  }

  public void Dispose() {
    foreach (var kvp in _recievers) {
      kvp.Key.OnNetworkMessage -= kvp.Value;
    }
    for (byte i = 0; i < byte.MaxValue; i++) {
      _handlers[i] = null;
    }
    _recievers.Clear();
    _headers.Clear();
  }

}

}
