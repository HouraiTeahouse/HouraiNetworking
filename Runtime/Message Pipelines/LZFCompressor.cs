using HouraiTeahouse.Compression;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking {

public class LZFCompressor : IMessageProcessor {

    public const byte kHeaderCompresssed = 1;
    public const byte kHeaderUncompressed = 0;

    public unsafe void Apply(ref byte[] data, ref int size) {
        Assert.IsTrue(data.Length >= size);
        fixed (byte* ptr = data) {
            var temp = stackalloc byte[size + 1];
            var count = CLZF2.TryCompress(ptr, temp + 1, size, size);
            if (count <= 0 || count >= size) {
                // Compression failed. Prepend header and send.
                if (size + 1 >= data.Length) {
                    var pool = ArrayPool<byte>.Shared;
                    var tempBuf = pool.Rent(size + 1);
                    fixed (byte* outPtr = tempBuf) {
                        UnsafeUtility.MemCpy(outPtr + 1, ptr, size);
                        *outPtr = kHeaderUncompressed;
                    }
                    pool.Return(data);
                    data = tempBuf;
                } else {
                    UnsafeUtility.MemMove(ptr + 1, ptr, size);
                    *ptr = kHeaderUncompressed;
                }
                size++;
            } else {
                // Compression failed. Prepend header and send.
                Assert.IsTrue(count <= data.Length);
                *temp = kHeaderCompresssed;
                UnsafeUtility.MemCpy(ptr, temp, count + 1);
                size = count + 1;
            }
        }
    }

    public unsafe void Unapply(ref byte[] data, ref int size) {
        Assert.IsTrue(data.Length >= size);
        if (data.Length <= 0) return;
        var header = data[0];
        fixed (byte* ptr = data) {
            if (header == 0) {
                UnsafeUtility.MemMove(ptr, ptr + 1, size - 1);
                size--;
            } else {
                // Compressed need to decompress
                var outputSize = size;
                while (true) {
                    outputSize *= 2;
                    var temp = stackalloc byte[outputSize];
                    var count = CLZF2.TryDecompress(ptr + 1, temp, size - 1, outputSize);
                    if (count == 0) continue;
                    if (count >= data.Length) {
                       var pool = ArrayPool<byte>.Shared;
                       pool.Return(data);
                       data = pool.Rent(count);
                    }
                    fixed (byte* outPtr = data) {
                        UnsafeUtility.MemCpy(outPtr, temp, count);
                    }
                    size = count;
                }
            }
        } 
    }

}

}