using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HouraiTeahouse.Networking {

internal static class SerializationConstants {

  public static readonly Encoding Encoding = new UTF8Encoding();

}

public struct Deserializer : IDisposable {

  NetBuffer _buffer;
  public int Size => _buffer.Size;
  public uint Position => _buffer.Position;

  public static Deserializer Create(int size = -1) {
    return new Deserializer { _buffer = new NetBuffer(size) };
  }

  public Deserializer(Serializer serializer) {
    _buffer = new NetBuffer(serializer.AsArray());
  }

  public Deserializer(byte[] buffer) {
    _buffer = new NetBuffer(buffer);
  }

  public void SeekZero() => _buffer.SeekZero();

  public void Replace(byte[] buffer) => _buffer.Replace(buffer);

  // http://sqlite.org/src4/doc/trunk/www/varint.wiki
  // NOTE: big endian.

  public UInt32 ReadUInt32() {
    byte a0 = ReadByte();
    if (a0 < 241) return a0;
    byte a1 = ReadByte();
    if (a0 >= 241 && a0 <= 248) return (UInt32)(240 + 256 * (a0 - 241) + a1);
    byte a2 = ReadByte();
    if (a0 == 249) return (UInt32)(2288 + 256 * a1 + a2);
    byte a3 = ReadByte();
    if (a0 == 250) return a1 + (((UInt32)a2) << 8) + (((UInt32)a3) << 16);
    byte a4 = ReadByte();
    if (a0 >= 251) return a1 + (((UInt32)a2) << 8) + (((UInt32)a3) << 16) + (((UInt32)a4) << 24);
    throw new IndexOutOfRangeException("ReadPackedUInt32() failure: " + a0);
  }

  public UInt64 ReadUInt64() {
      byte a0 = ReadByte();
      if (a0 < 241) return a0;
      byte a1 = ReadByte();
      if (a0 >= 241 && a0 <= 248) return 240 + 256 * (a0 - ((UInt64)241)) + a1;
      byte a2 = ReadByte();
      if (a0 == 249) return 2288 + (((UInt64)256) * a1) + a2;
      byte a3 = ReadByte();
      if (a0 == 250) return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16);
      byte a4 = ReadByte();
      if (a0 == 251) {
        return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24);
      }
      byte a5 = ReadByte();
      if (a0 == 252) {
        return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32);
      }
      byte a6 = ReadByte();
      if (a0 == 253) {
        return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40);
      }
      byte a7 = ReadByte();
      if (a0 == 254) {
        return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40) + (((UInt64)a7) << 48);
      }
      byte a8 = ReadByte();
      if (a0 == 255) {
        return a1 + (((UInt64)a2) << 8) + (((UInt64)a3) << 16) + (((UInt64)a4) << 24) + (((UInt64)a5) << 32) + (((UInt64)a6) << 40) + (((UInt64)a7) << 48)  + (((UInt64)a8) << 56);
      }
      throw new IndexOutOfRangeException("ReadPackedUInt64() failure: " + a0);
  }

  public byte ReadByte() => _buffer.ReadByte();
  public sbyte ReadSByte() => (sbyte)_buffer.ReadByte();

  public short ReadInt16() {
    ushort value = 0;
    value |= _buffer.ReadByte();
    value |= (ushort)(_buffer.ReadByte() << 8);
    return (short)value;
  }

  public ushort ReadUInt16() {
    ushort value = 0;
    value |= _buffer.ReadByte();
    value |= (ushort)(_buffer.ReadByte() << 8);
    return value;
  }

  public int ReadInt32() => (int)DecodeZigZag(ReadUInt32());

  public long ReadInt64() => (long)DecodeZigZag(ReadUInt64());

  public float ReadSingle() {
#if INCLUDE_IL2CPP
    return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt32()), 0);
#else
    uint value = ReadUInt32();
    return FloatConversion.ToSingle(value);
#endif
  }

  public double ReadDouble() {
#if INCLUDE_IL2CPP
    return BitConverter.ToDouble(BitConverter.GetBytes(ReadUInt64()), 0);
#else
    ulong value = ReadUInt64();
    return FloatConversion.ToDouble(value);
#endif
  }

  public string ReadString() {
    UInt16 numBytes = ReadUInt16();
    if (numBytes == 0) return "";
    return _buffer.ReadString(SerializationConstants.Encoding, numBytes);
  }

  public char ReadChar() => (char)_buffer.ReadByte();
  public bool ReadBoolean() => _buffer.ReadByte() != 0;

  public byte[] ReadBytes(int count) {
    if (count < 0) {
      throw new IndexOutOfRangeException("NetworkReader ReadBytes " + count);
    }
    byte[] value = new byte[count];
    _buffer.ReadBytes(value, (uint)count);
    return value;
  }

  public byte[] ReadBytesAndSize() {
    ushort sz = ReadUInt16();
    if (sz == 0) return null;
    return ReadBytes(sz);
  }

  public Vector2 ReadVector2() {
    return new Vector2(ReadSingle(), ReadSingle());
  }

  public Vector3 ReadVector3() {
    return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
  }

  public Vector4 ReadVector4() {
    return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  public Color ReadColor() {
    return new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  public Color32 ReadColor32() {
    return new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
  }

  public Quaternion ReadQuaternion() {
    return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  public Rect ReadRect() {
    return new Rect(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  public Plane ReadPlane() {
    return new Plane(ReadVector3(), ReadSingle());
  }

  public Ray ReadRay() {
    return new Ray(ReadVector3(), ReadVector3());
  }

  public Matrix4x4 ReadMatrix4x4() {
      Matrix4x4 m = new Matrix4x4();
      m.m00 = ReadSingle();
      m.m01 = ReadSingle();
      m.m02 = ReadSingle();
      m.m03 = ReadSingle();
      m.m10 = ReadSingle();
      m.m11 = ReadSingle();
      m.m12 = ReadSingle();
      m.m13 = ReadSingle();
      m.m20 = ReadSingle();
      m.m21 = ReadSingle();
      m.m22 = ReadSingle();
      m.m23 = ReadSingle();
      m.m30 = ReadSingle();
      m.m31 = ReadSingle();
      m.m32 = ReadSingle();
      m.m33 = ReadSingle();
      return m;
  }

  public override string ToString() => _buffer.ToString();

  public TMsg Read<TMsg>() where TMsg : struct, INetworkSerializable {
    var msg = new TMsg();
    msg.Deserialize(ref this);
    return msg;
  }

  public TMsg ReadMessage<TMsg>() where TMsg : class, INetworkSerializable, new() {
    var msg = new TMsg();
    msg.Deserialize(ref this);
    return msg;
  }

  static long DecodeZigZag(ulong value) {
    unchecked {
      if ((value & 0x1) == 0x1) {
        return -1 * ((long)(value >> 1) + 1);
      }
      return (long)(value >> 1);
    }
  }

  public void Dispose() => _buffer.Dispose();

}

}