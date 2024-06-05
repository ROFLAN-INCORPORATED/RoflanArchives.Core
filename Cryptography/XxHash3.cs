using System;
using System.IO;

namespace RoflanArchives.Core.Cryptography;

internal sealed class XxHash3
{
    public const int SizeInBits = 64;
    public const int SizeInBytes = 8;



    private System.IO.Hashing.XxHash3 Algorithm { get; }

    public bool Initialized { get; }



    public XxHash3()
    {
        Algorithm =
            new System.IO.Hashing.XxHash3();

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
