using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace Engi.Substrate.Identity;

public class RefreshToken : UserToken
{
    public static RefreshToken Encrypt(
        string userId,
        RSA encryption,
        TimeSpan validFor)
    {
        JsonObject payload = new()
        {
            [nameof(userId)] = userId,
            ["rng"] = GenerateRng(64)
        };

        var payloadBytes = Encoding.UTF8.GetBytes(payload.ToString());

        byte[] encrypted = encryption.Encrypt(payloadBytes, RSAEncryptionPadding.Pkcs1);
        
        return new()
        {
            Value = Convert.ToBase64String(encrypted),
            ExpiresOn = DateTime.UtcNow + validFor
        };
    }

    public static string? DecryptUserId(string value, RSA encryption)
    {
        byte[] data = Convert.FromBase64String(value);

        byte[] decrypted = encryption.Decrypt(data, RSAEncryptionPadding.Pkcs1);

        var decryptedJson = JsonNode.Parse(Encoding.UTF8.GetString(decrypted));

        return (string?) decryptedJson?["userId"];
    }

    private static string GenerateRng(int length)
    {
        byte[] numArray = new byte[length];
        
        Rng.GetBytes(numArray);

        return Convert.ToBase64String(numArray);
    }

    private static readonly RandomNumberGenerator Rng = RandomNumberGenerator.Create();
}