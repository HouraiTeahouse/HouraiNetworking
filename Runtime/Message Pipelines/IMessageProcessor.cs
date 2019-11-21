using System;

namespace HouraiTeahouse.Networking {

public interface IMessageProcessor {

    /// <summary>
    /// Applies the transformation.
    /// 
    /// Throws an exception if an irrecoverable error occurs.
    /// </summary>
    /// <param name="src">the input buffer to read from</param>
    /// <param name="dst">the output buffer to write to. Should be resized the exact size.</param>
    /// <returns>true if the application succeeded, false if the output buffer is too small.</returns>
    bool Apply(ReadOnlySpan<byte> src, ref Span<byte> dst);

    /// <summary>
    /// Unapplies the transformation.
    /// 
    /// Throws an exception if an irrecoverable error occurs.
    /// </summary>
    /// <param name="src">the input buffer to read from</param>
    /// <param name="dst">the output buffer to write to. Should be resized the exact size.</param>
    /// <returns>true if the processing succeeded, false if the output buffer is too small.</returns>
    bool Unapply(ReadOnlySpan<byte> src, ref Span<byte> dst);

}

}
