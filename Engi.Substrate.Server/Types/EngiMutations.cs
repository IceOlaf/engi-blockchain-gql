using System.Text;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Types;
using sr25519_dotnet.lib;

namespace Engi.Substrate.Server.Types;

public class EngiMutations : ObjectGraphType
{
    public EngiMutations()
    {
        Field<UserType>(
            "createUser",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateUserInputType>> { Name = "user" }
            ),
            resolve: context =>
            {
                var args = (Dictionary<string, object>)context.Arguments!["user"].Value!;

                args.TryGetValue("mnemonicSalt", out object? mnemonicSaltObject);
                args.TryGetValue("password", out object? passwordObject);

                string mnemonic = (string)args["mnemonic"];
                string? mnemonicSalt = (string?)mnemonicSaltObject;
                string? password = (string?)passwordObject;

                return CreateUser(
                    (string)args["name"],
                    mnemonic,
                    mnemonicSalt,
                    password
                );
            });
    }

    private User CreateUser(
        string name,
        string mnemonic,
        string? mnemonicSalt,
        string? password)
    {
        mnemonicSalt ??= string.Empty;

        byte[] secret;

        var mnemonicWords = mnemonic.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (mnemonicWords.Length is 12 or 15 or 18 or 21 or 24)
        {
            secret = KeypairFactory.CreateSecretKeyFromWordlistMnemonic(mnemonic, mnemonicSalt, Wordlists.English);
        }
        else
        {
            if (!string.IsNullOrEmpty(mnemonicSalt))
            {
                throw new ExecutionError(
                    "A raw seed cannot be used in conjuction with a mnemonic salt.");
            }

            if (mnemonicWords.Length > 32)
            {
                throw new ExecutionError(
                    "Specified phrase is not a valid mnemonic and is invalid as a raw seed at > 32 bytes");
            }

            secret = Encoding.UTF8.GetBytes(mnemonic.PadRight(32));
        }
        
        var sr25519Pair = SR25519.GenerateKeypairFromSeed(secret);

        bool isEncrypted = password != null;

        var pkcs8 = !isEncrypted
            ? sr25519Pair.ExportToPkcs8() 
            : sr25519Pair.ExportToPkcs8(password!);

        string address = Address.Encode(sr25519Pair.Public);

        return new User
        {
            Encoded = Convert.ToBase64String(pkcs8),
            Name = name,
            Address = address,
            Metadata = new AccountMetadata
            {
                Content = new[] { "pkcs8", "sr25519" },
                Type = isEncrypted ? new[] { "scrypt", "xsalsa20-poly1305" } : new[] { "none" },
                Version = 3
            }
        };
    }
}

