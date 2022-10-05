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

    public bool IsValid(User user, SignatureArguments args)
    {
        string expectedSignatureContent =
            $"{user.Address}|{new DateTimeOffset(args.SignedOn).ToUniversalTime().ToUnixTimeMilliseconds()}";

        string wrappedSignatureContent = $"<Bytes>{expectedSignatureContent}</Bytes>";

        byte[] valueBytes = Hex.GetBytes0X(args.Value);

        // first try to verify wrapped as most expected case, then raw

        Address address = user.Address;

        bool valid = address.Verify(valueBytes,
            Encoding.UTF8.GetBytes(wrappedSignatureContent));

        if (!valid)
        {
            valid = address.Verify(valueBytes,
                Encoding.UTF8.GetBytes(expectedSignatureContent));
        }

        return valid && args.SignedOn < DateTime.UtcNow.Add(options.SignatureSkew);
    }

    public void ValidateOrThrow(User user, SignatureArguments args)
    {
        if (!IsValid(user, args))
        {
            throw new AuthenticationError();
        }
    }

    public Keypair DecodeKeypair(User user)
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