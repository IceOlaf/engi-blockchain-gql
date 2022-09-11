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
            .Argument<NonNullGraphType<SignedMutationArgumentsGraphType>>("signature")
            .ResolveAsync(BalanceTransferAsync);
    }

    private async Task<object?> BalanceTransferAsync(
        IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<BalanceTransferArguments>("transfer");
        var signature = context.GetValidatedArgument<SignedMutationArguments>("signature");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        var engiOptions = scope.ServiceProvider.GetRequiredService<IOptions<EngiOptions>>();

        if (!signature.IsValid(user.Address, engiOptions.Value.SignatureSkew))
        {
            throw new AuthenticationError();
        }
        
        var chainStateProvider = scope.ServiceProvider.GetRequiredService<LatestChainStateProvider>();

        var tipCalculator = scope.ServiceProvider.GetRequiredService<TransactionTipCalculator>();

        var client = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var sender = Keypair.FromPkcs8(user.KeypairPkcs8, engiOptions.Value.EncryptionCertificateAsX509);

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