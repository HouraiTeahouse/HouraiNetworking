using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using System.Text;

namespace HouraiTeahouse.Networking {

internal struct NetBuffer : IDisposable {

  internal byte[] _buffer;
  uint position;
  const int kInitialSize = 2048;
  const float kGrowthFactor = 2f;
  const int kBufferSizeWarning = 1024 * 1024 * 128;

  public uint Position => position; 
  public int Size => _buffer.Length;

  public NetBuffer(int size = -1) {
    size = size < 0 ? kInitialSize : size;
    _buffer = ArrayPool<byte>.Shared.Rent(size);
    position = 0;
  }

  public void Dispose() {
    if (_buffer == null) return;
    ArrayPool<byte>.Shared.Return(_buffer);
    _buffer = null;
  }

  // this does NOT copy the buffer
  public NetBuffer(byte[] buffer) {
    _buffer = buffer;
    position = 0;
  }

  public byte ReadByte() {
    if (position >= _buffer.Length) {
      throw new IndexOutOfRangeException("NetworkReader:ReadByte out of range:" + ToString());
    }
    return _buffer[position++];
  }

  public void ReadBytes(byte[] buffer, uint count) {
    if (position + count > _buffer.Length) {
      throw new IndexOutOfRangeException("NetworkReader:ReadBytes out of range: (" + count + ") " + ToString());
    }
    Buffer.BlockCopy(_buffer, (int)position, buffer, 0, (int)count);
    position += count;
  }

  public string ReadString(Encoding encoding, uint count) {
    if (position + count > _buffer.Length) {
      throw new IndexOutOfRangeException("NetworkReader:ReadString out of range: (" + count + ") " + ToString());
    }

    var decodedString = encoding.GetString(_buffer, (int)position, (int)count);
    position += count;
    return decodedString;
  }

  public void ReadChars(char[] buffer, uint count) {
    if (position + count > _buffer.Length) {
      throw new IndexOutOfRangeException("NetworkReader:ReadChars out of range: (" + count + ") " + ToString());
    }
    for (ushort i = 0; i < count; i++) {
      buffer[i] = (char)_buffer[position + i];
    }
    position += count;
  }

  internal ArraySegment<byte> AsArraySegment() {
    return new ArraySegment<byte>(_buffer, 0, (int)position);
  }

  public void WriteByte(byte value) {
    WriteCheckForSpace(1);
    _buffer[position] = value;
    position += 1;
  }

  public void WriteByte2(byte value0, byte value1) {
    WriteCheckForSpace(2);
    _buffer[position] = value0;
    _buffer[position + 1] = value1;
    position += 2;
  }

  public void WriteByte4(byte value0, byte value1, byte value2, byte value3) {
    WriteCheckForSpace(4);
    _buffer[position] = value0;
    _buffer[position + 1] = value1;
    _buffer[position + 2] = value2;
    _buffer[position + 3] = value3;
    position += 4;
  }

  public void WriteByte8(byte value0, byte value1, byte value2, byte value3, byte value4, byte value5, byte value6, byte value7) {
    WriteCheckForSpace(8);
    _buffer[position] = value0;
    _buffer[position + 1] = value1;
    _buffer[position + 2] = value2;
    _buffer[position + 3] = value3;
    _buffer[position + 4] = value4;
    _buffer[position + 5] = value5;
    _buffer[position + 6] = value6;
    _buffer[position + 7] = value7;
    position += 8;
  }

  // every other Write() function in this class writes implicitly at the end-marker m_Pos.
  // this is the only Write() function that writes to a specific location within the buffer
  public void WriteBytesAtOffset(byte[] buffer, ushort targetOffset, ushort count) {
    uint newEnd = (uint)(count + targetOffset);
    WriteCheckForSpace((ushort)newEnd);
    if (targetOffset == 0 && count == buffer.Length) {
      buffer.CopyTo(_buffer, position);
    } else {
      //CopyTo doesnt take a count :(
      for (int i = 0; i < count; i++) {
        _buffer[targetOffset + i] = buffer[i];
      }
    }

    // although this writes within the buffer, it could move the end-marker
    if (newEnd > position) {
      position = newEnd;
    }
  }

  public void WriteBytes(byte[] buffer, ushort count) {
    WriteCheckForSpace(count);
    Buffer.BlockCopy(buffer, 0, _buffer, (int)position, (int)count);
    position += count;
  }

  public void WriteString(Encoding encoding, string value) {
    int count = encoding.GetByteCount(value);
    WriteCheckForSpace((ushort)count);
    encoding.GetBytes(value, 0, value.Length, _buffer, 0);
    position += (uint)count;
  }

  void WriteCheckForSpace(ushort count) {
    if (position + count < _buffer.Length) return;

    int newLen = (int)(_buffer.Length * kGrowthFactor);
    while (position + count >= newLen) {
      newLen = (int)(newLen * kGrowthFactor);
      if (newLen > kBufferSizeWarning) {
        Debug.LogWarning("NetworkBuffer size is " + newLen + " bytes!");
      }
    }

    // only do the copy once, even if newLen is increased multiple times
    var pool = ArrayPool<byte>.Shared;
    byte[] tmp = pool.Rent(newLen);
    _buffer.CopyTo(tmp, 0);
    pool.Return(_buffer);
    _buffer = tmp;
  }

  public void FinishMessage() {
    // two shorts (size and msgType) are in header.
    ushort sz = (ushort)(position - (sizeof(ushort) * 2));
    _buffer[0] = (byte)(sz & 0xff);
    _buffer[1] = (byte)((sz >> 8) & 0xff);
  }

  public void SeekZero() => position = 0;

  public void Replace(byte[] buffer) {
    ArrayPool<byte>.Shared.Return(_buffer);
    _buffer = buffer;
    position = 0;
  }

  public override string ToString() => $"Netbuf sz:{_buffer.Length} pos:{position}";
} // end NetBuffer

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