using Engi.Substrate.Jobs;
using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiMutations : ObjectGraphType
{
    private readonly IServiceProvider serviceProvider;

    public EngiMutations(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        Field<StringGraphType>("attemptJob")
            .Argument<NonNullGraphType<AttemptJobArgumentsGraphType>>("args")
            .ResolveAsync(AttemptJobAsync);

        Field<StringGraphType>("balanceTransfer")
            .Argument<NonNullGraphType<BalanceTransferArgumentsGraphType>>("transfer")
            .ResolveAsync(BalanceTransferAsync);

        Field<StringGraphType>("createJob")
            .Argument<NonNullGraphType<CreateJobArgumentsGraphType>>("job")
            .ResolveAsync(CreateJobAsync);

        Field<UserType>("createUser")
            .Argument<NonNullGraphType<CreateUserArgumentsGraphType>>("user")
            .Resolve(CreateUser);
    }

    private async Task<object?> AttemptJobAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<AttemptJobArguments>("args");
        var chainState = await GetLatestChainState();

        var sender = KeypairFactory.CreateFromAny(args.SenderSecret);

        var client = serviceProvider.GetRequiredService<SubstrateClient>();

        AccountInfo account;

        try
        {
            account = await client.GetSystemAccountAsync(sender.Address);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentValidationException(
                nameof(args), nameof(args.SenderSecret), "Account not found.");
        }

        return await client.AttemptJobAsync(chainState, sender, account, args);
    }

    private async Task<object?> BalanceTransferAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<BalanceTransferArguments>("transfer");
        var chainState = await GetLatestChainState();

        var sender = KeypairFactory.CreateFromAny(args.SenderSecret);
        var dest = Address.Parse(args.RecipientAddress);

        var client = serviceProvider.GetRequiredService<SubstrateClient>();

        AccountInfo account;

        try
        {
            account = await client.GetSystemAccountAsync(sender.Address);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentValidationException(
                nameof(args), nameof(args.SenderSecret), "Account not found.");
        }

        return await client.BalanceTransferAsync(
            chainState, sender, account, dest, args.Amount, ExtrinsicEra.Immortal, args.Tip);
    }

    private async Task<object?> CreateJobAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<CreateJobArguments>("job");
        var chainState = await GetLatestChainState();

        var sender = KeypairFactory.CreateFromAny(args.SenderSecret);

        var client = serviceProvider.GetRequiredService<SubstrateClient>();

        AccountInfo account;

        try
        {
            account = await client.GetSystemAccountAsync(sender.Address);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentValidationException(
                nameof(args), nameof(args.SenderSecret), "Account not found.");
        }

        return await client.CreateJobAsync(chainState, sender, account, args);
    }

    private async Task<ChainState> GetLatestChainState()
    {
        using var scope = serviceProvider.CreateScope();

        var observers = scope.ServiceProvider
            .GetServices<IChainObserver>()
            .ToArray();

        var chainSnapshotObserver = observers.OfType<ChainSnapshotObserver>().Single();
        var headObserver = observers.OfType<NewHeadChainObserver>().Single();

        return new()
        {
            Metadata = await chainSnapshotObserver.Metadata,
            Version = await chainSnapshotObserver.Version,
            GenesisHash = await chainSnapshotObserver.GenesisHash,
            LatestFinalizedHeader = headObserver.LastFinalizedHeader!
        };
    }

    private User CreateUser(IResolveFieldContext context)
    {
        var input = context.GetValidatedArgument<CreateUserArguments>("user");

        var sr25519Pair = KeypairFactory.CreateFromAny(input.Mnemonic, input.MnemonicSalt);

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

