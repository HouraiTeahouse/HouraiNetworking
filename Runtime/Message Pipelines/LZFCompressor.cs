using HouraiTeahouse.Compression;
using System;
using System.Buffers;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Assertions;

namespace HouraiTeahouse.Networking {

public class LZFCompressor : IMessageProcessor {

    public const byte kHeaderCompresssed = 1;
    public const byte kHeaderUncompressed = 0;

    public unsafe bool Apply(ReadOnlySpan<byte> src, ref Span<byte> dst) {
        if (dst.Length <= src.Length + 1) return false;
        fixed (byte* srcPtr = src, dstPtr = dst) {
            var count = CLZF2.TryCompress(srcPtr, dstPtr + 1, src.Length, src.Length);
            if (count <= 0 || count >= src.Length) {
                // Compression failed. Prepend header and send.
                *dstPtr = kHeaderUncompressed;
                UnsafeUtility.MemCpy(dstPtr + 1, srcPtr, src.Length);
                count = src.Length;
            } else {
                // Compression succeeded. Prepend header and send.
                if (count + 1 < dst.Length) return false;
                Assert.IsTrue(count + 1 <= dst.Length);
                *dstPtr = kHeaderCompresssed;
            }
            dst = dst.Slice(0, count + 1);
        }
        return true;
    }

    public unsafe bool Unapply(ReadOnlySpan<byte> src, ref Span<byte> dst) {
        var header = src[0];
        fixed (byte* srcPtr = src, dstPtr = dst) {
            if (header == kHeaderUncompressed) {
                if (dst.Length < src.Length - 1) return false;
                UnsafeUtility.MemCpy(dstPtr, srcPtr + 1, src.Length - 1);
                return true;
            }
            // Compressed need to decompress
            var count = CLZF2.TryDecompress(srcPtr + 1, dstPtr, src.Length - 1, dst.Length);
            if (count <= 0 || count > dst.Length) return false;
            dst = dst.Slice(0, count);
            return true;
        } 
    }

}

}
