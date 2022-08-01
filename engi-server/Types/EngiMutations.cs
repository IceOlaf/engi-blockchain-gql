using System.Text;
using Engi.Substrate.Keys;
using GraphQL;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiMutations : ObjectGraphType
{
    private readonly IServiceProvider serviceProvider;

    public EngiMutations(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        Field<UserType>(
            "createUser",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateUserInputType>> { Name = "user" }
            ),
            resolve: context =>
            {
                var input = (CreateUserInput)context.Arguments!["user"].Value!;

                return CreateUser(input);
            }
        );

        Field<StringGraphType>(
            "balanceTransfer",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<BalanceTransferInputType>> { Name = "transfer" }
            ),
            resolve: context =>
            {
                var input = (BalanceTransferInput)context.Arguments!["transfer"].Value!;
                var chainState = GetLatestChainState();

                return BalanceTransferAsync(chainState, input);
            }
        );

        Field<StringGraphType>(
            "createJob",
            arguments: new QueryArguments(
                new QueryArgument<NonNullGraphType<CreateJobInputType>> {Name = "job"}
            ),
            resolve: context =>
            {
                var input = (CreateJobInput) context.Arguments!["job"].Value!;
                var chainState = GetLatestChainState();

                return CreateJobAsync(chainState, input);
            }
        );
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

    private async Task<string> BalanceTransferAsync(
        ChainState chainState, 
        BalanceTransferInput input)
    {
        var sender = KeypairFactory.CreateFromAny(input.SenderSecret);
        var dest = Address.From(input.RecipientAddress);

        var client = serviceProvider.GetRequiredService<SubstrateClient>();

        var account = await client.GetSystemAccountAsync(sender.Address);

        return await client.BalanceTransferAsync(
            chainState, sender, account, dest, input.Amount, ExtrinsicEra.Immortal, input.Tip);
    }

    private async Task<string> CreateJobAsync(
        ChainState chainState,
        CreateJobInput input)
    {
        var sender = KeypairFactory.CreateFromAny(input.SenderSecret);

        var client = serviceProvider.GetRequiredService<SubstrateClient>();

        var account = await client.GetSystemAccountAsync(sender.Address);

        return await client.CreateJobAsync(
            chainState, sender, account, input.Funding, ExtrinsicEra.Immortal, input.Tip);
    }

    private ChainState GetLatestChainState()
    {
        using var scope = serviceProvider.CreateScope();

        var observers = scope.ServiceProvider
            .GetServices<IChainObserver>()
            .ToArray();

        var chainSnapshotObserver = observers.OfType<ChainSnapshotObserver>().Single();
        var headObserver = observers.OfType<NewHeadChainObserver>().Single();

        return new()
        {
            Metadata = chainSnapshotObserver.Metadata,
            Version = chainSnapshotObserver.Version,
            GenesisHash = chainSnapshotObserver.GenesisHash,
            LatestFinalizedHeader = headObserver.LastFinalizedHeader!
        };
    }
}

