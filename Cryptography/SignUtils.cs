using System;
using System.IO;

namespace RoflanArchives.Core.Cryptography;

internal static class SignUtils
{
    public static (byte[] PublicKey, byte[] PrivateKey) CreateKeys()
    {
        using var algorithm =
            new RSA();

        return (algorithm.PublicKey,
            algorithm.PrivateKey);
    }

    public static void CreateKeysFiles(
        string keyName,
        string directoryPath)
    {
        CreateKeysFiles(keyName,
            directoryPath,
            directoryPath);
    }
    public static void CreateKeysFiles(
        string keyName,
        string publicKeyDirectoryPath,
        string privateKeyDirectoryPath)
    {
        var (publicKey, privateKey) =
            CreateKeys();

        CreateKeysFiles(keyName,
            publicKeyDirectoryPath, publicKey,
            privateKeyDirectoryPath, privateKey);
    }
    public static void CreateKeysFiles(
        string keyName,
        string publicKeyDirectoryPath,
        byte[] publicKey,
        string privateKeyDirectoryPath,
        byte[] privateKey)
    {
        keyName = keyName.Trim().Trim('.');

        var publicKeyPath = Path.Combine(
            publicKeyDirectoryPath, $"{keyName}.roflkey");
        var privateKeyPath = Path.Combine(
            privateKeyDirectoryPath, $"{keyName}.roflpkey");

        File.WriteAllBytes(
            publicKeyPath,
            publicKey);
        File.WriteAllBytes(
            privateKeyPath,
            privateKey);
    }



    public static byte[] GetSign(
        string privateKeyPath,
        ReadOnlySpan<byte> data)
    {
        using var rsa = new RSA();

        rsa.ImportPrivateKey(
            File.ReadAllBytes(privateKeyPath));

        return rsa.GetSign(
            data);
    }

    public static bool VerifySign(
        string publicKeyPath,
        ReadOnlySpan<byte> data,
        ReadOnlySpan<byte> sign)
    {
        using var rsa = new RSA();

        rsa.ImportPublicKey(
            File.ReadAllBytes(publicKeyPath));

        return rsa.VerifySign(
            data, sign);
    }



    public static void CreateSignFile(
        string privateKeyPath,
        string dataFilePath,
        string signDirectoryPath = "")
    {
        var keyFileName = Path.GetFileNameWithoutExtension(
            privateKeyPath);
        var dataFileName = Path.GetFileName(
            dataFilePath);

        if (signDirectoryPath.Length == 0)
            signDirectoryPath = Path.GetDirectoryName(dataFilePath)
                                ?? string.Empty;

        var data = File.ReadAllBytes(
            dataFilePath);
        var sign = GetSign(
            privateKeyPath,
            data);

        var signFilePath = Path.Combine(
            signDirectoryPath,
            $"{dataFileName}.{keyFileName}.roflsign");

        File.WriteAllBytes(
            signFilePath,
            sign);
    }

    public static bool VerifySignFile(
        string publicKeyPath,
        string dataFilePath,
        string signFilePath = "")
    {
        if (signFilePath.Length == 0)
        {
            var keyFileName = Path.GetFileNameWithoutExtension(
                publicKeyPath);
            var dataFileName = Path.GetFileName(
                dataFilePath);
            var signDirectoryPath = Path.GetDirectoryName(dataFilePath)
                                    ?? string.Empty;

            signFilePath = Path.Combine(
                signDirectoryPath,
                $"{dataFileName}.{keyFileName}.roflsign");
        }

        var data = File.ReadAllBytes(
            dataFilePath);
        var sign = File.ReadAllBytes(
            signFilePath);

        return VerifySign(
            publicKeyPath,
            data,
            sign);
    }
}
