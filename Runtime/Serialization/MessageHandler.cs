using System;
using System.Collections;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public sealed class MessageHandlers : IDisposable {

  readonly Action<NetworkMessage>[] _handlers;
  readonly Dictionary<INetworkReciever, NetworkMessageHandler> _recievers;

  public MessageHandlers() {
    _handlers = new Action<NetworkMessage>[byte.MaxValue];
  }

  public void RegisterHandler(byte code, Action<NetworkMessage> handler) {
    if (handler == null) return;
    _handlers[code] += handler;
  }

  public void RegisterHandler<T>(byte code, Action<T> handler) where T : INetworkSerializable, new() {
    if (handler == null) return;
    RegisterHandler(code, dataMsg => {
      var message = dataMsg.ReadAs<T>();
      handler(message);
      (message as IDisposable)?.Dispose();
      ObjectPool<T>.Shared.Return(message);
    });
  }

  public void ClearHandlers(byte code) => _handlers[code] = null;

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
  }

}

}