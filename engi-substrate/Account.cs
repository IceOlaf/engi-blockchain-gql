using Engi.Substrate.Keys;

namespace Engi.Substrate;

public class Account
{
    public Account(Keypair keypair, string name, string password)
    {
        Encoded = Convert.ToBase64String(keypair.ExportToPkcs8(password));
        
        Address = keypair.Address;

        Encoding = new AccountEncoding
        {
            Content = new[] { "pkcs8", "sr25519" },
            Type = new[] { "scrypt", "xsalsa20-poly1305" },
            Version = 3
        };

        Meta = new()
        {
            ["name"] = name,
            ["whenCreated"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            ["isHardware"] = false
        };
    }

    public string Encoded { get; set; }

    public Address Address { get; set; }

    public AccountEncoding Encoding { get; set; }

    public Dictionary<string, object> Meta { get; }
}