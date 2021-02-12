﻿using HouraiTeahouse.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;

namespace HouraiTeahouse.Networking {

public sealed class MessageHandlers : IDisposable {

  readonly Action<NetworkMessage>[] _handlers;
  readonly Dictionary<Type, byte> _headers;
  readonly Dictionary<INetworkReciever, NetworkMessageHandler> _recievers;

  public delegate void Handler<T>(ref T message);
  public delegate void ReceiverHandler<T>(INetworkReciever reciever, ref T message);

  public MessageHandlers() {
    _handlers = new Action<NetworkMessage>[byte.MaxValue];
    _headers = new Dictionary<Type, byte>();
    _recievers = new Dictionary<INetworkReciever, NetworkMessageHandler>();
  }

  public bool CanHandle(byte header) => _handlers[header] != null;

  public void RegisterHandler(byte code, Action<NetworkMessage> handler) {
    if (handler == null) return;
    _handlers[code] += handler;
  }

  public void RegisterHandler<T>(byte header, Handler<T> handler) where T : ISerializable, new() {
    if (handler == null) throw new ArgumentNullException(nameof(handler));
    if (_headers.TryGetValue(typeof(T), out byte storedHeader)) {
      if (storedHeader != header) {
        throw new InvalidOperationException($"Type {typeof(T)} is already registered with the header {storedHeader}");
      }
    }
    _headers[typeof(T)] = header;
    RegisterHandler(header, dataMsg => {
      var message = dataMsg.ReadAs<T>();
      handler(ref message);
      (message as IDisposable)?.Dispose();
      ObjectPool<T>.Shared.Return(message);
    });
  }

  public void RegisterHandler<T>(byte header, ReceiverHandler<T> handler) where T : ISerializable, new() {
    if (handler == null) throw new ArgumentNullException(nameof(handler));
    if (_headers.TryGetValue(typeof(T), out byte storedHeader)) {
      if (storedHeader != header) {
        throw new InvalidOperationException($"Type {typeof(T)} is already registered with the header {storedHeader}");
      }
    }
    _headers[typeof(T)] = header;
    RegisterHandler(header, dataMsg => {
      var message = dataMsg.ReadAs<T>();
      handler(dataMsg.Reciever, ref message);
      (message as IDisposable)?.Dispose();
      ObjectPool<T>.Shared.Return(message);
    });
  }

  public void Serialize<TMsg, TSerializer>(in TMsg msg, ref TSerializer serializer) 
                                            where TMsg : ISerializable
                                            where TSerializer : struct, ISerializer {
    byte header;
    if (_headers.TryGetValue(typeof(TMsg), out header)) {
      serializer.Write(header);
      msg.Serialize(ref serializer);
    } else {
      throw new InvalidOperationException($"Type {typeof(TMsg)} is not registered.");
    }
  }

  public unsafe void Send<T>(INetworkSender sender, in T msg,
                      Reliability reliability = Reliability.Reliable) 
                      where T : ISerializable {
    Span<byte> buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var serializer = FixedSizeSerializer.Create(buffer);
    Serialize<T, FixedSizeSerializer>(msg, ref serializer);
    sender.SendMessage(serializer.AsReadOnlySpan(), reliability);
  }

  public unsafe void Broadcast<T>(IEnumerable<INetworkSender> senders, in T msg,
                           Reliability reliability = Reliability.Reliable) 
                           where T : ISerializable {
    Span<byte> buffer = stackalloc byte[SerializationConstants.kMaxMessageSize];
    var serializer = FixedSizeSerializer.Create(buffer);
    Serialize<T, FixedSizeSerializer>(msg, ref serializer);
    foreach (var sender in senders) {
      sender.SendMessage(serializer.AsReadOnlySpan(), reliability);
    }
  }

  void ThrowIfNotRegistered<T>() {
    if (!_headers.ContainsKey(typeof(T))) {
      throw new InvalidOperationException($"Type {typeof(T)} is not registered.");
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

  public unsafe void Listen(INetworkReciever reciever) {
    if (_recievers.ContainsKey(reciever)) return;
    NetworkMessageHandler callback = (msg) => {
      if (msg.Length <= 0) return;
      var deserializer = FixedSizeDeserializer.Create(msg);
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
