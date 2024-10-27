using System;
using System.IO;

namespace RoflanArchives.Core.Cryptography;

internal sealed class XxHash32
{
    public const int SizeInBits = 32;
    public const int SizeInBytes = 4;



    private System.IO.Hashing.XxHash32 Algorithm { get; }

    public bool Initialized { get; }



    public XxHash32()
    {
        Algorithm =
            new System.IO.Hashing.XxHash32();

        Initialized = true;
    }



    public ReadOnlyMemory<byte> GetHash(
        ReadOnlySpan<byte> data)
    {
        Algorithm.Append(
            data);

        return Algorithm
            .GetHashAndReset();
    }
    public ReadOnlyMemory<byte> GetHash(
        Stream data)
    {
        Algorithm.Append(
            data);

        return Algorithm
            .GetHashAndReset();
    }

    public bool VerifyHash(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> hash)
    {
        var dataHash = GetHash(
            data);

        return HashUtils.SecureEqualsUnsafe(
            dataHash.Span,
            hash);
    }
    public bool VerifyHash(
        Stream data,
        ReadOnlySpan<byte> hash)
    {
        var dataHash = GetHash(
            data);

        return HashUtils.SecureEqualsUnsafe(
            dataHash.Span,
            hash);
    }
}
