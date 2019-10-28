using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;
using System.Text;
using System;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace HouraiTeahouse.Networking {

public static class SerializationConstants {

  public static readonly Encoding Encoding = new UTF8Encoding();
  public static int kMaxMessageSize = 2048;

}

/// <summary>
/// High speed, no/low GC deserializer reading from fixed size buffers that is
/// guarenteed to be conssistent regardless of platform.
/// </summary>
/// <remarks>
/// This is a value type to avoid allocating GC, when passing it to other
/// funcitons, be sure to pass it by reference via ref parameters.
///
/// Calls to read data from the buffer do have bounds checking for safety
/// reasons.
///
/// Do not create these via "new Deserializer" or the program may crash from
/// segfaulting. Use Deserializer.Create instead.
///
/// This struct does not lock access to the underlying buffer or the pointers to
/// it. Shared use across multiple threads is not safe. Copies of the same
/// deserializer is threadsafe, so long as there is no process writing to the
/// underlying buffer.
///
/// This deserializer favors small message size and compatibility
/// over speed. If speed is imperative, it may be faster to directly copy
/// structs into the buffers. Such an alternative will likely not be portable as
/// it preserves the endianness of each value. Use in remote messaging may be
/// incorrect if the two cmmmunicating machines are using different endianness.
/// </remarks>
public unsafe struct Deserializer {

  byte* _start, _current, _end;

  /// <summary>
  /// The position the deserializer is currently at.
  /// </summary>
  public int Position => (int)(_current - _start);

  /// <summary>
  /// The total size of the underlying buffer.
  /// </summary>
  public int Size => (int)(_end - _start);

  /// <summary>
  /// Creates a Deserializer from a provided buffer.
  /// </summary>
  public static Deserializer Create(byte* buf, uint size) {
    return new Deserializer {
      _start = buf,
      _current = buf,
      _end = buf + size,
    };
  }

  /// <summary>
  /// Creates a Deserializer from a provided FixedBuffer.
  /// </summary>
  public static Deserializer Create(FixedBuffer buf) {
    return new Deserializer {
      _start = buf.Start,
      _current = buf.Start,
      _end = buf.End,
    };
  }

  /// <summary>
  /// Deserializes an object directly from a base64 string.
  ///
  /// This function allocates GC
  public static T FromBase64String<T>(string encoded) where T : INetworkSerializable, new() {
    var bytes = Convert.FromBase64String(encoded);
    fixed (byte* ptr = bytes) {
      var obj = new T();
      var deserializer = Create(ptr, (uint)bytes.Length);
      obj.Deserialize(ref deserializer);
      return obj;
    }
  }

  /// <summary>
  /// Returns the cursor to the start of the buffer.
  /// </summary>
  public void SeekZero() => _current = _start;

  void CheckRemainingSize(int size) {
    if (_current + size > _end) {
      throw new IndexOutOfRangeException("Buffer overflow: " + ToString());
    }
  }

  /// <summary>
  /// Writes a single byte from the buffer.
  /// </summary>
  public byte ReadByte() {
    CheckRemainingSize(1);
    return *_current++;
  }

  /// <summary>
  /// Writes a single signed byte from the buffer.
  /// </summary>
  public sbyte ReadSByte() => (sbyte)ReadByte();

  /// <summary>
  /// Reads a 2 byte ushort from the buffer, as a 1-3 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
  public ushort ReadUInt16() {
    byte a0 = ReadByte();
    if (a0 < 241) return a0;
    byte a1 = ReadByte();
    if (a0 >= 241 && a0 <= 248) return (ushort)(240 + 256 * (a0 - ((ushort)241)) + a1);
    byte a2 = ReadByte();
    if (a0 == 249) return (ushort)(2288 + (((ushort)256) * a1) + a2);
    throw new IndexOutOfRangeException("ReadPackedUInt16() failure: " + a0);
  }

  /// <summary>
  /// Reads a 4 byte uint from the buffer, as a 1-5 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
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

  /// <summary>
  /// Reads a 8 byte ulong from the buffer, as a 1-8 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
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

  /// <summary>
  /// Reads a 2 byte short from the buffer, as a 1-3 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// Signed integers are encoded via zigzag encoding for more efficient encoding
  /// of negatrive values. See:
  /// https://developers.google.com/protocol-buffers/docs/encoding#signed-integers
  /// for more information.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
  public short ReadInt16() => (short)DecodeZigZag(ReadUInt16());

  /// <summary>
  /// Reads a 4 byte integer from the buffer, as a 1-5 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// Signed integers are encoded via zigzag encoding for more efficient encoding
  /// of negatrive values. See:
  /// https://developers.google.com/protocol-buffers/docs/encoding#signed-integers
  /// for more information.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
  public int ReadInt32() => (int)DecodeZigZag(ReadUInt32());

  /// <summary>
  /// Reads a 8 byte integer from the buffer, as a 1-9 byte varint in big endian,
  /// regarldess of platform.
  ///
  /// Signed integers are encoded via zigzag encoding for more efficient encoding
  /// of negatrive values. See:
  /// https://developers.google.com/protocol-buffers/docs/encoding#signed-integers
  /// for more information.
  ///
  /// See: http://sqlite.org/src4/doc/trunk/www/varint.wiki
  /// </summary>
  public long ReadInt64() => (long)DecodeZigZag(ReadUInt64());

  /// <summary>
  /// Reads a 4 byte float from the buffer. This is always 4 bytes on the wire.
  /// </summary>
  public float ReadSingle() {
#if INCLUDE_IL2CPP
    return BitConverter.ToSingle(BitConverter.GetBytes(ReadUInt32()), 0);
#else
    uint value = ReadUInt32();
    return FloatConversion.ToSingle(value);
#endif
  }

  /// <summary>
  /// Reads a 8 byte float from the buffer. This is always 8 bytes on the wire.
  /// </summary>
  public double ReadDouble() {
#if INCLUDE_IL2CPP
    return BitConverter.ToDouble(BitConverter.GetBytes(ReadUInt64()), 0);
#else
    ulong value = ReadUInt64();
    return FloatConversion.ToDouble(value);
#endif
  }

  /// <summary>
  /// Reads a UTF-8 encoded string from the buffer. The maximum supported length
  /// of the encoded string is 65535 bytes.
  /// </summary>
  public string ReadString() {
    ushort count = ReadUInt16();
    if (count == 0) return "";
    var decodedString = SerializationConstants.Encoding.GetString(_current, (int)count);
    _current += count;
    return decodedString;
  }

  /// <summary>
  /// Reads a single character from the buffer.
  /// </summary>
  public char ReadChar() => (char)ReadUInt16();

  /// <summary>
  /// Reads a boolean from the buffer. This is 1 byte on the wire. It may be
  /// preferable to write and read from a bitmask to if there are multiple
  /// boolean values to be encoded/decoded.
  /// </summary>
  public bool ReadBoolean() => ReadByte() != 0;

  /// <summary>
  /// Reads fixed size byte array from the buffer. This will allocate garbage in
  /// the form of the byte array returned.  The maximum supported length of the
  /// byte array is 65535 bytes.
  /// </summary>
  public byte[] ReadBytes(int count) {
    if (count < 0) {
      throw new IndexOutOfRangeException("NetworkReader ReadBytes " + count);
    }
    byte[] value = new byte[count];
    fixed (byte* bufPtr = value) {
      UnsafeUtility.MemCpy(bufPtr, _current, count);
    }
    _current += count;
    return value;
  }

  /// <summary>
  /// Reads fixed sized buffer into a provided buffer. The maximum supported
  /// length of the byte array is 65535 bytes.
  ///
  /// This function only does bounds checking on the underlying read buffer. It's
  /// upon the caller to ensure the memory being written to is safe.
  /// </summary>
  public void ReadBytes(byte* buffer, int count) {
    if (count < 0) {
      throw new IndexOutOfRangeException("NetworkReader ReadBytes " + count);
    }
    UnsafeUtility.MemCpy(buffer, _current, count);
    _current += count;
  }

  /// <summary>
  /// Reads fixed size byte array from the buffer using the size encodedin the
  /// underlying buffer This will allocate garbage in the form of the byte array
  /// returned.  The maximum supported length of the byte array is 65535 bytes.
  /// </summary>
  public byte[] ReadBytesAndSize() {
    ushort sz = ReadUInt16();
    if (sz == 0) return null;
    return ReadBytes(sz);
  }

  /// <summary>
  /// Reads a Vector2 from the underlying buffer. This will always be 8 bytes on
  /// the wire.
  /// </summary>
  public Vector2 ReadVector2() {
    return new Vector2(ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Vector3 from the underlying buffer. This will always be 12 bytes on
  /// the wire.
  /// </summary>
  public Vector3 ReadVector3() {
    return new Vector3(ReadSingle(), ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Vector4 from the underlying buffer. This will always be 16 bytes on
  /// the wire.
  /// </summary>
  public Vector4 ReadVector4() {
    return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Color from the underlying buffer. This will always be 16 bytes on
  /// the wire.
  /// </summary>
  public Color ReadColor() {
    return new Color(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Color32 from the underlying buffer. This will always be 4 bytes on
  /// the wire.
  /// </summary>
  public Color32 ReadColor32() {
    return new Color32(ReadByte(), ReadByte(), ReadByte(), ReadByte());
  }

  /// <summary>
  /// Reads a Quaternion from the underlying buffer. This will always be 16 bytes
  /// on the wire.
  /// </summary>
  public Quaternion ReadQuaternion() {
    return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Rect from the underlying buffer. This will always be 16 bytes
  /// on the wire.
  /// </summary>
  public Rect ReadRect() {
    return new Rect(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
  }

  /// <summary>
  /// Reads a Plane from the underlying buffer. This will always be 16 bytes
  /// on the wire.
  /// </summary>
  public Plane ReadPlane() {
    return new Plane(ReadVector3(), ReadSingle());
  }

  /// <summary>
  /// Reads a Ray from the underlying buffer. This will always be 24 bytes
  /// on the wire.
  /// </summary>
  public Ray ReadRay() {
    return new Ray(ReadVector3(), ReadVector3());
  }

  /// <summary>
  /// Reads a Matrix4x4 from the underlying buffer. This will always be 64 bytes
  /// on the wire.
  /// </summary>
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

  public override string ToString() => $"Deserializer sz:{Size} pos:{Position}";

  /// <summary>
  /// Reads a serializable message from the underlying buffer. If the type is a
  /// reference type, this will allocate GC.
  /// </summary>
  public TMsg Read<TMsg>() where TMsg : INetworkSerializable, new() {
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

}

// -- helpers for float conversion --
// This cannot be used with IL2CPP because it cannot convert FieldOffset at the moment
// Until that is supported the IL2CPP codepath will use BitConverter instead of this. Use
// of BitConverter is otherwise not optimal as it allocates a byte array for each conversion.
#if !INCLUDE_IL2CPP
[StructLayout(LayoutKind.Explicit)]
internal struct UIntFloat {
    [FieldOffset(0)]
    public float floatValue;

    [FieldOffset(0)]
    public uint intValue;

    [FieldOffset(0)]
    public double doubleValue;

    [FieldOffset(0)]
    public ulong longValue;
}

internal class FloatConversion {

  public static float ToSingle(uint value) {
    UIntFloat uf = new UIntFloat();
    uf.intValue = value;
    return uf.floatValue;
  }

  public static double ToDouble(ulong value) {
    UIntFloat uf = new UIntFloat();
    uf.longValue = value;
    return uf.doubleValue;
  }

}
#endif // !INCLUDE_IL2CPP

}
