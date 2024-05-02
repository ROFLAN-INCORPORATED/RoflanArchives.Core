using System;
using System.Security.Cryptography;

namespace RoflanArchives.Core.Cryptography;

internal class RSA : IDisposable
{
    public const int KeySizeInBits = 2048;
    public const int KeySizeInBytes = 256;



    private System.Security.Cryptography.RSA Algorithm { get; }

    public byte[] PublicKey
    {
        get
        {
            return ExportPublicKey();
        }
        set
        {
            ImportPublicKey(value);
        }
    }
    public byte[] PrivateKey
    {
        get
        {
            return ExportPrivateKey();
        }
        set
        {
            ImportPrivateKey(value);
        }
    }
    public int KeySize
    {
        get
        {
            return Algorithm.KeySize;
        }
    }
    public RSAEncryptionPadding EncryptionPadding { get; }
    public RSASignaturePadding SignPadding { get; }
    public HashAlgorithmName SignHashAlgorithm { get; }
    public bool Initialized { get; }



    public RSA()
    {
        Algorithm =
            System.Security.Cryptography.RSA.Create(KeySizeInBits);

        EncryptionPadding = RSAEncryptionPadding.OaepSHA384;
        SignPadding = RSASignaturePadding.Pkcs1;
        SignHashAlgorithm = HashAlgorithmName.SHA384;

        Initialized = true;
    }



    public byte[] Encrypt(
        ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return Array.Empty<byte>();

        return Algorithm.Encrypt(
            data, EncryptionPadding);
    }
    public byte[] Decrypt(
        ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return Array.Empty<byte>();

        return Algorithm.Decrypt(
            data, EncryptionPadding);
    }



    public byte[] GetSign(
        ReadOnlySpan<byte> data)
    {
        if (data.Length == 0)
            return Array.Empty<byte>();

        return Algorithm.SignData(
            data,
            SignHashAlgorithm,
            SignPadding);
    }

    public bool VerifySign(
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> sign)
    {
        if (data.Length == 0)
            return false;

        return Algorithm.VerifyData(
            data, sign,
            SignHashAlgorithm,
            SignPadding);
    }



    public byte[] ExportPublicKey()
    {
        return Algorithm.ExportRSAPublicKey();
    }

    public byte[] ExportPrivateKey()
    {
        return Algorithm.ExportRSAPrivateKey();
    }


    public int ImportPublicKey(
        ReadOnlySpan<byte> key)
    {
        if (key.Length == 0)
            return 0;

        Algorithm.ImportRSAPublicKey(
            key, out var bytesRead);

        return bytesRead;
    }

    public int ImportPrivateKey(
        ReadOnlySpan<byte> key)
    {
        if (key.Length == 0)
            return 0;

        Algorithm.ImportRSAPrivateKey(
            key, out var bytesRead);

        return bytesRead;
    }



    public void Dispose()
    {
        Algorithm.Dispose();
    }
}
