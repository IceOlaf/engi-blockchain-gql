namespace Engi.Substrate.Keys;

public interface IPublicKey
{
    byte[] PublicKey { get; }
}

public static class PublicKeyExtensions
{
    public static bool Verify(
        this IPublicKey key, 
        byte[] signature, 
        byte[] message)
    {
        return SR25519.Verify(signature, message, (uint)message.Length, key.PublicKey);
    }
}