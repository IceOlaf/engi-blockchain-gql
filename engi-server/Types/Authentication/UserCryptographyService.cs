using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using GraphQL;
using Microsoft.Extensions.Options;

namespace Engi.Substrate.Server.Types.Authentication;

public class UserCryptographyService
{
    private readonly EngiOptions options;

    public UserCryptographyService(IOptions<EngiOptions> options)
    {
        this.options = options.Value;
    }

    public bool IsValid(Address address, SignatureArguments args)
    {
        string expectedSignatureContent =
            $"{address}|{new DateTimeOffset(args.SignedOn).ToUniversalTime().ToUnixTimeMilliseconds()}";

        string wrappedSignatureContent = $"<Bytes>{expectedSignatureContent}</Bytes>";

        byte[] valueBytes = Hex.GetBytes0X(args.Value);

        // first try to verify wrapped as most expected case, then raw

        bool valid = address.Verify(valueBytes,
            Encoding.UTF8.GetBytes(wrappedSignatureContent));

        if (!valid)
        {
            valid = address.Verify(valueBytes,
                Encoding.UTF8.GetBytes(expectedSignatureContent));
        }

        return valid && args.SignedOn < DateTime.UtcNow.Add(options.SignatureSkew);
    }

    public void ValidateOrThrow(Address address, SignatureArguments args)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        if (args == null)
        {
            throw new ArgumentNullException(nameof(args));
        }

        if (!IsValid(address, args))
        {
            throw new AuthenticationError();
        }
    }

    public void ValidateOrThrow(User user, SignatureArguments args) => ValidateOrThrow(user.Address, args);

    public Keypair DecryptKeypair(string encryptedPkcs8Key)
    {
        var privateKey = options.EncryptionCertificateAsX509.GetRSAPrivateKey()!;

        var encryptedData = Convert.FromBase64String(encryptedPkcs8Key);

        var decrypted = privateKey.Decrypt(encryptedData, RSAEncryptionPadding.Pkcs1);

        var keypairPkcs8 = Convert.FromBase64String(Encoding.UTF8.GetString(decrypted));

        return Keypair.FromPkcs8(keypairPkcs8);
    }

    public byte[] EncryptKeypair(Keypair keypair)
    {
        return keypair.ExportToPkcs8(options.EncryptionCertificateAsX509);
    }

    public Keypair DecryptKeypair(User user)
    {
        if (user.KeypairPkcs8 == null)
        {
            throw new ExecutionError("User has not imported their key into ENGI.")
            {
                Code = "NO_USER_KEY"
            };
        }

        return Keypair.FromPkcs8(user.KeypairPkcs8, options.EncryptionCertificateAsX509);
    }
}