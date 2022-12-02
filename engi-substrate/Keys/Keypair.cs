using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Blake2Core;
using Chaos.NaCl;
using CryptSharp.Core.Utility;

namespace Engi.Substrate.Keys;

public class Keypair : IPublicKey
{
    public Address Address { get; internal init; } = null!;

    public byte[] PublicKey { get; internal init; } = null!;

    public byte[] SecretKey { get; internal init; } = null!;
    
    public byte[] Sign(byte[] message)
    {
        byte[] signature = new byte[64];

        if (message.Length > 256)
        {
            var config = new Blake2BConfig { OutputSizeInBits = 256 };
            message = Blake2B.ComputeHash(message, config);
        }

        SR25519.Sign(PublicKey, SecretKey, message, (uint)message.Length, signature);

        return signature;
    }

    public static Keypair FromPkcs8(byte[] data)
    {
        var secretKey = new byte[SECRET_KEY_LENGTH];
        var publicKey = new byte[PUBLIC_KEY_LENGTH];

        data.AsSpan(PKCS8_HEADER.Length, SECRET_KEY_LENGTH).CopyTo(secretKey);
        data.AsSpan(PKCS8_HEADER.Length + SECRET_KEY_LENGTH + PKCS8_DIVIDER.Length, PUBLIC_KEY_LENGTH).CopyTo(publicKey);

        return new()
        {
            Address = Address.From(publicKey),
            PublicKey = publicKey,
            SecretKey = secretKey
        };
    }

    public static Keypair FromPkcs8(string data)
    {
        return FromPkcs8(Convert.FromBase64String(data));
    }

    public static Keypair FromPkcs8(byte[] data, X509Certificate2 cert)
    {
        byte[] decrypted = cert.GetRSAPrivateKey()!
            .Decrypt(data, RSAEncryptionPadding.Pkcs1);

        return FromPkcs8(decrypted);
    }

    public byte[] ExportToPkcs8()
    {
        byte[] result = new byte[PKCS8_LENGTH];

        PKCS8_HEADER.CopyTo(result.AsSpan());
        SecretKey.CopyTo(result.AsSpan(PKCS8_HEADER.Length));
        PKCS8_DIVIDER.CopyTo(result.AsSpan(PKCS8_HEADER.Length + SECRET_KEY_LENGTH, PKCS8_DIVIDER.Length));
        PublicKey.CopyTo(result.AsSpan(PKCS8_HEADER.Length + SECRET_KEY_LENGTH + PKCS8_DIVIDER.Length, PUBLIC_KEY_LENGTH));

        return result;
    }

    public byte[] ExportToPkcs8(string passphrase)
    {
        byte[] salt = new byte[32];
        byte[] xsalsaNonce = new byte[24];

        RandomNumberGenerator.Fill(salt);
        RandomNumberGenerator.Fill(xsalsaNonce);

        return ExportToPkcs8(passphrase, salt, xsalsaNonce);
    }

    public byte[] ExportToPkcs8(X509Certificate2 cert)
    {
        var pkcs8 = ExportToPkcs8();

        return cert.GetRSAPrivateKey()!
            .Encrypt(pkcs8, RSAEncryptionPadding.Pkcs1);
    }

    /// <summary>
    /// Only used for testing
    /// </summary>
    internal byte[] ExportToPkcs8(string passphrase, byte[] scryptSalt, byte[] xsalsaNonce)
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
        var pkcs8 = ExportToPkcs8();

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

    static Keypair()
    {
        PKCS8_LENGTH = PKCS8_HEADER.Length + PKCS8_DIVIDER.Length + KEYPAIR_LENGTH;
    }

    private static readonly int PKCS8_LENGTH;

    private static readonly byte[] PKCS8_DIVIDER = { 161, 35, 3, 33, 0 };
    private static readonly byte[] PKCS8_HEADER = { 48, 83, 2, 1, 1, 48, 5, 6, 3, 43, 101, 112, 4, 34, 4, 32 };

    internal const int SECRET_KEY_LENGTH = 64;
    internal const int PUBLIC_KEY_LENGTH = 32;
    internal const int KEYPAIR_LENGTH = SECRET_KEY_LENGTH + PUBLIC_KEY_LENGTH;
}