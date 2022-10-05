using Engi.Substrate.Identity;
using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types.Authentication;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types;

public class CurrencyMutations : ObjectGraphType
{
    public CurrencyMutations()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<StringGraphType>("balanceTransfer")
            .Argument<NonNullGraphType<BalanceTransferArgumentsGraphType>>("transfer")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .ResolveAsync(BalanceTransferAsync);
    }

    private async Task<object?> BalanceTransferAsync(
        IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<BalanceTransferArguments>("transfer");
        var signature = context.GetValidatedArgument<SignatureArguments>("signature");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var crypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        crypto.ValidateOrThrow(user, signature);
        
        var chainStateProvider = scope.ServiceProvider.GetRequiredService<LatestChainStateProvider>();

        var tipCalculator = scope.ServiceProvider.GetRequiredService<TransactionTipCalculator>();

        var client = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var sender = crypto.DecodeKeypair(user);

        AccountInfo account;

        try
        {
            account = await client.GetSystemAccountAsync(sender.Address);
        }
        catch (KeyNotFoundException)
        {
            throw new ArgumentValidationException(
                nameof(args), nameof(sender), "Account not found.");
        }

        var chainState = await chainStateProvider.GetLatestChainState();

        var tip = await tipCalculator.CalculateTipAsync(user);

        return await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<BalanceTransferArguments>(
                sender, args, account, ExtrinsicEra.Immortal, chainState, tip), chainState.Metadata);
    }
}