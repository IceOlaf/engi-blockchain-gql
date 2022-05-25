using System.Text;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Types;

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
                var input = (CreateUserInput)context.Arguments!["user"].Value!;

                return CreateUser(input);
            });


            });
    }

    private User CreateUser(CreateUserInput input)
    {
        input.MnemonicSalt ??= string.Empty;

        byte[] seed;

        var mnemonicWords = input.Mnemonic.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        if (mnemonicWords.Length is 12 or 15 or 18 or 21 or 24)
        {
            seed = KeypairFactory.CreateSeedFromWordlistMnemonic(input.Mnemonic, input.MnemonicSalt, Wordlists.English);
        }
        else
        {
            if (!string.IsNullOrEmpty(input.MnemonicSalt))
            {
                throw new ExecutionError(
                    "A raw seed cannot be used in conjuction with a mnemonic salt.");
            }

            if (mnemonicWords.Length > 32)
            {
                throw new ExecutionError(
                    "Specified phrase is not a valid mnemonic and is invalid as a raw seed at > 32 bytes");
            }

            seed = Encoding.UTF8.GetBytes(input.Mnemonic.PadRight(32));
        }
        
        var sr25519Pair = KeypairFactory.CreateFromSeed(seed);

        bool isEncrypted = input.Password != null;

        var pkcs8 = !isEncrypted
            ? sr25519Pair.ExportToPkcs8() 
            : sr25519Pair.ExportToPkcs8(input.Password!);

        var address = Address.From(sr25519Pair.PublicKey);

        return new User
        {
            Encoded = Convert.ToBase64String(pkcs8),
            Name = input.Name,
            Address = address.Id,
            Metadata = new AccountMetadata
            {
                Content = new[] { "pkcs8", "sr25519" },
                Type = isEncrypted ? new[] { "scrypt", "xsalsa20-poly1305" } : new[] { "none" },
                Version = 3
            }
        };
    }
}

