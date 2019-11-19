using System;
using System.Text;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections.LowLevel.Unsafe;

namespace HouraiTeahouse.Networking {

public unsafe struct Serializer {

  byte* _start, _current, _end;

  public int Position => (int)(_current - _start);
  public int Size => (int)(_end - _start);

  public static Serializer Create(Span<byte> buffer) {
      fixed (byte* ptr = buffer) {
        return new Serializer {
            _start = ptr,
            _current = ptr,
            _end = ptr + buffer.Length,
        };
      }
  }

  public static implicit operator ReadOnlySpan<byte>(Serializer buffer) =>
    new ReadOnlySpan<byte>(buffer._start, buffer.Size);

  public string ToBase64String() {
    var array =  ((ReadOnlySpan<byte>)this).ToArray();
    return Convert.ToBase64String(array, 0, Position);
  }

  void CheckRemainingSize(int size) {
    if (_current + size > _end) {
      throw new IndexOutOfRangeException("Buffer overflow: " + ToString());
    }
  }

  // http://sqlite.org/src4/doc/trunk/www/varint.wiki

  public void Write(UInt32 value) {
    if (value <= 240) {
      CheckRemainingSize(1);
      *_current++ = (byte)value;
    } else if (value <= 2287) {
      CheckRemainingSize(2);
      *_current++ = (byte)((value - 240) / 256 + 241);
      *_current++ = (byte)((value - 240) % 256);
    } else if (value <= 67823) {
      CheckRemainingSize(3);
      *_current++ = 249;
      *_current++ = (byte)((value - 2288) / 256);
      *_current++ = (byte)((value - 2288) % 256);
    } else if (value <= 16777215) {
      CheckRemainingSize(4);
      *_current++ = 250;
      for (var i = 0; i <= 16; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else {
      CheckRemainingSize(5);
      *_current++ = 251;
      for (var i = 0; i <= 24; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    }
  }

  public void Write(UInt64 value) {
    if (value <= 240) {
      CheckRemainingSize(1);
      *_current++ = (byte)value;
    } else if (value <= 2287) {
      CheckRemainingSize(2);
      *_current++ = (byte)((value - 240) / 256 + 241);
      *_current++ = (byte)((value - 240) % 256);
    } else if (value <= 67823) {
      CheckRemainingSize(3);
      *_current++ = 249;
      *_current++ = (byte)((value - 2288) / 256);
      *_current++ = (byte)((value - 2288) % 256);
    } else if (value <= 16777215) {
      CheckRemainingSize(4);
      *_current++ = 250;
      for (var i = 0; i <= 16; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else if (value <= 4294967295) {
      CheckRemainingSize(5);
      *_current++ = 251;
      for (var i = 0; i <= 24; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else if (value <= 1099511627775) {
      CheckRemainingSize(6);
      *_current++ = 252;
      for (var i = 0; i <= 32; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else if (value <= 281474976710655) {
      CheckRemainingSize(7);
      *_current++ = 253;
      for (var i = 0; i <= 40; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else if (value <= 72057594037927935) {
      CheckRemainingSize(8);
      *_current++ = 254;
      for (var i = 0; i <= 48; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    } else {
      CheckRemainingSize(9);
      *_current++ = 255;
      for (var i = 0; i <= 56; i += 8) {
        *_current++ = (byte)(value >> i);
      }
    }
  }

  public void Write(byte value) {
    CheckRemainingSize(1);
    *_current++ = value;
  }

  public void Write(sbyte value) => Write((byte)value);
  public void Write(short value) => Write((ushort)EncodeZigZag(value, 16));
  public void Write(ushort value) {
    if (value <= 240) {
      CheckRemainingSize(1);
      *_current++ = (byte)value;
    } else if (value <= 2287) {
      CheckRemainingSize(2);
      *_current++ = (byte)((value - 240) / 256 + 241);
      *_current++ = (byte)((value - 240) % 256);
    } else {
      CheckRemainingSize(3);
      *_current++ = 249;
      *_current++ = (byte)((value - 2288) / 256);
      *_current++ = (byte)((value - 2288) % 256);
    }
  }

  public void Write(int value) => Write((uint)EncodeZigZag(value, 32));
  public void Write(long value) => Write(EncodeZigZag(value, 64));

#if !INCLUDE_IL2CPP
  static UIntFloat s_FloatConverter;
#endif

  public void Write(float value) {
#if INCLUDE_IL2CPP
    Write(BitConverter.ToUInt32(BitConverter.GetBytes(value), 0));
#else
    s_FloatConverter.floatValue = value;
    Write(s_FloatConverter.intValue);
#endif
  }

  public void Write(double value) {
#if INCLUDE_IL2CPP
    Write(BitConverter.ToUInt64(BitConverter.GetBytes(value), 0));
#else
    s_FloatConverter.doubleValue = value;
    Write(s_FloatConverter.longValue);
#endif
  }

  public void Write(string value) {
    if (value == null) {
      *_current++ = 0;
      *_current++ = 0;
      return;
    }

    var encoding = SerializationConstants.Encoding;
    int count = encoding.GetByteCount(value);
    Write((ushort)(count));
    CheckRemainingSize(count + 1);
    fixed (char* charPtr = value.ToCharArray()) {
      _current += encoding.GetBytes(charPtr, value.Length, _current, (int)(_end - _current));
    }
  }

  public void Write(bool value) => Write((byte)(value ? 1 : 0));

  public void Write(byte[] buffer, ushort count) {
    CheckRemainingSize(count);
    fixed (byte* bufPtr = buffer) {
      UnsafeUtility.MemCpy(_current, bufPtr, count);
    }
    _current += count;
  }

  public void Write(byte* buffer, ushort count) {
    CheckRemainingSize(count);
    UnsafeUtility.MemCpy(_current, buffer, count);
    _current += count;
  }

  public void WriteBytesAndSize(byte[] buffer, ushort count) {
    if (buffer == null || count == 0) {
      Write((ushort)0);
      return;
    }

    Write(count);
    Write(buffer, count);
  }

  public void WriteStruct<T>(ref T value) where T : struct {
    var size = UnsafeUtility.SizeOf<T>();
    CheckRemainingSize(size);
    UnsafeUtility.CopyStructureToPtr(ref value, _current);
    _current += size;
  }

  public void WriteStruct<T>(void* buffer, int count) where T : struct {
    var size = UnsafeUtility.SizeOf<T>();
    CheckRemainingSize(size * count);
    UnsafeUtility.MemCpy(_current, buffer, size * count);
    _current += size * count;
  }

  public void Write(Vector2 value) {
    Write(value.x);
    Write(value.y);
  }

  public void Write(Vector3 value) {
    Write(value.x);
    Write(value.y);
    Write(value.z);
  }

  public void Write(Vector4 value) {
    Write(value.x);
    Write(value.y);
    Write(value.z);
    Write(value.w);
  }

  public void Write(Color value) {
    Write(value.r);
    Write(value.g);
    Write(value.b);
    Write(value.a);
  }

  public void Write(Color32 value) {
    Write(value.r);
    Write(value.g);
    Write(value.b);
    Write(value.a);
  }

  public void Write(Quaternion value) {
    Write(value.x);
    Write(value.y);
    Write(value.z);
    Write(value.w);
  }

  public void Write(Rect value) {
    Write(value.xMin);
    Write(value.yMin);
    Write(value.width);
    Write(value.height);
  }

  public void Write(Plane value) {
    Write(value.normal);
    Write(value.distance);
  }

  public void Write(Ray value) {
    Write(value.direction);
    Write(value.origin);
  }

  public void Write(Matrix4x4 value) {
    Write(value.m00);
    Write(value.m01);
    Write(value.m02);
    Write(value.m03);
    Write(value.m10);
    Write(value.m11);
    Write(value.m12);
    Write(value.m13);
    Write(value.m20);
    Write(value.m21);
    Write(value.m22);
    Write(value.m23);
    Write(value.m30);
    Write(value.m31);
    Write(value.m32);
    Write(value.m33);
  }

  public void Write<T>(in T msg) where T : INetworkSerializable => msg.Serialize(ref this);

  public void SeekZero() => _current = _start;

  static ulong EncodeZigZag(long value, int bitLength) {
    unchecked {
      return (ulong)((value << 1) ^ (value >> (bitLength - 1)));
    }
  }

};

}
