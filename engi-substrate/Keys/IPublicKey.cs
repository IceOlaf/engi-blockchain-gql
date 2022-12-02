using Blake2Core;

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
        if (message.Length > 256)
        {
            var config = new Blake2BConfig { OutputSizeInBits = 256 };
            message = Blake2B.ComputeHash(message, config);
        }

        return SR25519.Verify(signature, message, (uint)message.Length, key.PublicKey);
    }
}