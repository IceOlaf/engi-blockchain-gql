using System.Security.Cryptography;
using System.Text;
using Chaos.NaCl;
using CryptSharp.Core.Utility;
using sr25519_dotnet.lib.Models;

namespace Engi.Substrate.Keys;

public static class KeypairExtensions
{
    private static readonly byte[] PKCS8_DIVIDER = { 161, 35, 3, 33, 0 };
    private static readonly byte[] PKCS8_HEADER = { 48, 83, 2, 1, 1, 48, 5, 6, 3, 43, 101, 112, 4, 34, 4, 32 };

    public static byte[] ExportToPkcs8(this SR25519Keypair keypair)
    {
        int length = PKCS8_HEADER.Length + keypair.Secret.Length + PKCS8_DIVIDER.Length + keypair.Public.Length;

        byte[] result = new byte[length];

        Buffer.BlockCopy(PKCS8_HEADER, 0, result,
            0, PKCS8_HEADER.Length);
        Buffer.BlockCopy(keypair.Secret, 0, result,
            PKCS8_HEADER.Length, keypair.Secret.Length);
        Buffer.BlockCopy(PKCS8_DIVIDER, 0, result,
            PKCS8_HEADER.Length + keypair.Secret.Length, PKCS8_DIVIDER.Length);
        Buffer.BlockCopy(keypair.Public, 0, result,
            PKCS8_HEADER.Length + keypair.Secret.Length + PKCS8_DIVIDER.Length, keypair.Public.Length);

        return result;
    }

    public static byte[] ExportToPkcs8(this SR25519Keypair keypair, string passphrase)
    {
        byte[] salt = new byte[32];
        byte[] xsalsaNonce = new byte[24];

        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(xsalsaNonce);

        return ExportToPkcs8(keypair, passphrase, salt, xsalsaNonce);
    }

    /// <summary>
    /// Only used for testing
    /// </summary>
    public static byte[] ExportToPkcs8(this SR25519Keypair keypair, string passphrase, byte[] scryptSalt, byte[] xsalsaNonce)
    {
        if (string.IsNullOrEmpty(passphrase))
        {
            throw new ArgumentNullException(nameof(passphrase));
        }

        if (scryptSalt is not { Length: 32 })
        {
            throw new ArgumentException("Salt must be a 32-byte array.");
        }

        var passphraseBytes = Encoding.UTF8.GetBytes(passphrase);

        const int cost = 32768;
        const int parallel = 1;
        const int blockSize = 8;

        var password = SCrypt.ComputeDerivedKey(passphraseBytes, scryptSalt, cost, blockSize, parallel, null, 64);
        var pkcs8 = ExportToPkcs8(keypair);

        var encrypted = XSalsa20Poly1305.Encrypt(pkcs8, password.AsSpan(0, 32).ToArray(), xsalsaNonce);

        using var ms = new MemoryStream();

        ms.Write(scryptSalt.AsSpan());

        using (var writer = new BinaryWriter(ms, Encoding.UTF8, true))
        {
            writer.Write(cost);
            writer.Write(parallel);
            writer.Write(blockSize);
        }

        ms.Write(xsalsaNonce.AsSpan());
        ms.Write(encrypted.AsSpan());

        return ms.ToArray();
    }
}