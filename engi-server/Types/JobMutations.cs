using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types.Authentication;
using Engi.Substrate.Server.Types.Validation;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types;

public class JobMutations : ObjectGraphType
{
    public JobMutations()
    {
        this.AuthorizeWithPolicy(PolicyNames.Authenticated);

        Field<StringGraphType>("attempt")
            .Description("Register a job attempt by calling attempt_job on the chain.")
            .Argument<NonNullGraphType<AttemptJobArgumentsGraphType>>("args")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .ResolveAsync(AttemptJobAsync);

        Field<StringGraphType>("create")
            .Description("Create a job by calling create_job on the chain.")
            .Argument<NonNullGraphType<CreateJobArgumentsGraphType>>("job")
            .Argument<NonNullGraphType<SignatureArgumentsGraphType>>("signature")
            .ResolveAsync(CreateJobAsync);
    }

    private async Task<object?> AttemptJobAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<AttemptJobArguments>("args");
        var signature = context.GetValidatedArgument<SignatureArguments>("signature");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var crypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        crypto.ValidateOrThrow(user, signature);

        var client = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var chainStateProvider = scope.ServiceProvider.GetRequiredService<LatestChainStateProvider>();

        var tipCalculator = scope.ServiceProvider.GetRequiredService<TransactionTipCalculator>();

        var sender = crypto.DecryptKeypair(user);

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

        var chainState = await chainStateProvider.GetLatestChainStateAsync();

        var tip = await tipCalculator.CalculateTipAsync(user);

        return await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<AttemptJobArguments>(
                sender, args, account, ExtrinsicEra.Immortal, chainState, tip), chainState.Metadata);
    }

    private async Task<object?> CreateJobAsync(IResolveFieldContext context)
    {
        var args = context.GetValidatedArgument<CreateJobArguments>("args");
        var signature = context.GetValidatedArgument<SignatureArguments>("signature");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        var userCrypto = scope.ServiceProvider.GetRequiredService<UserCryptographyService>();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        var user = await session.LoadAsync<User>(context.User!.Identity!.Name);

        userCrypto.ValidateOrThrow(user, signature);

        // validate the user owns this repo

        string repositoryFullName;

        try
        {
            repositoryFullName = RepositoryUrl.ParseFullName(args.RepositoryUrl);
        }
        catch (ArgumentException ex)
        {
            throw new ArgumentValidationException(ex);
        }

        if (!user.GithubEnrollments.ContainsRepositoryWithFullName(repositoryFullName))
        {
            throw new ExecutionError("User does not have access to repository.") { Code = "FORBIDDEN" };
        }

        // submit

        var client = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        var chainStateProvider = scope.ServiceProvider.GetRequiredService<LatestChainStateProvider>();

        var tipCalculator = scope.ServiceProvider.GetRequiredService<TransactionTipCalculator>();

        var sender = userCrypto.DecryptKeypair(user);

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

        var chainState = await chainStateProvider.GetLatestChainStateAsync();

        var tip = await tipCalculator.CalculateTipAsync(user);

        return await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<CreateJobArguments>(
                sender, args, account, ExtrinsicEra.Immortal, chainState, tip), chainState.Metadata);
    }

}
