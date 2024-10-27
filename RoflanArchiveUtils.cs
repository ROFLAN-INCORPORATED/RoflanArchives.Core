using System;
using System.Text;
using RoflanArchives.Core.Cryptography;

namespace RoflanArchives.Core;

public static class RoflanArchiveUtils
{
    internal static UTF8Encoding UTF8 { get; }
    internal static XxHash32 IdHashAlgorithm { get; }



    static RoflanArchiveUtils()
    {
        UTF8 = new UTF8Encoding(false, true);
        IdHashAlgorithm = new XxHash32();
    }



    public static uint GenerateId(
        string relativePath)
    {
        return BitConverter.ToUInt32(
            IdHashAlgorithm.GetHash(
                    UTF8.GetBytes(relativePath))
                .Span);
    }
}
