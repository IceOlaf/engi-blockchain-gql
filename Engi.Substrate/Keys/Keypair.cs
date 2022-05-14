namespace Engi.Substrate.Keys;

public class Keypair
{
    public Address Address { get; set; } = null!;

    public byte[] PublicKey { get; private init; } = null!;

    public byte[] SecretKey { get; private init; } = null!;

    private Keypair() { }

    public byte[] Sign(byte[] message)
    {
        byte[] signature = new byte[64];

        SR25519.Sign(PublicKey, SecretKey, message, (uint)message.Length, signature);

        return signature;
    }

    public bool Verify(byte[] signature, byte[] message)
    {
        return SR25519.Verify(signature, message, (uint)message.Length, PublicKey);
    }

    public static Keypair Create(byte[] raw)
    {
        var publicKey = raw.AsSpan(64, 32).ToArray();

        return new()
        {
            Address = Address.From(publicKey),
            SecretKey = raw.AsSpan(0, 64).ToArray(),
            PublicKey = publicKey
        };
    }
}