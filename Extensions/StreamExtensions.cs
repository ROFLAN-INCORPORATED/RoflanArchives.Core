using System;
using System.Buffers;
using System.IO;

namespace RoflanArchives.Core.Extensions;

internal static class StreamExtensions
{
    internal const int DefaultBufferSize = 81920; // 65536

    internal static long CopyBytesTo(
        this Stream source, Stream destination,
        long count, int bufferSize = DefaultBufferSize)
    {
        var bytesRemaining = count;

        do
        {
            var bytesCopied = CopyBytesTo(
                source, destination,
                (int)bytesRemaining,
                bufferSize);

            if (bytesCopied == 0)
                break;

            bytesRemaining -= bytesCopied;
        } while (bytesRemaining > 0);

        return count - bytesRemaining;
    }
    internal static int CopyBytesTo(
        this Stream source, Stream destination,
        int count, int bufferSize = DefaultBufferSize)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(
            bufferSize);

        var bytesRemaining = count;

        try
        {
            do
            {
                var bytesRead = source.Read(
                    buffer, 0,
                    Math.Min(buffer.Length, bytesRemaining));

                if (bytesRead == 0)
                    break;

                destination.Write(
                    buffer, 0,
                    bytesRead);

                bytesRemaining -= bytesRead;
            } while (bytesRemaining > 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }

        return count - bytesRemaining;
    }
}
