using System.Security.Cryptography;
using Engi.Substrate.Jobs;
using Engi.Substrate.Keys;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using Raven.Client.Documents.Subscriptions;
using Sentry;

namespace Engi.Substrate.Server.Async;

public class SolveJobService : SubscriptionProcessingBase<SolveJobCommand>
{
    private readonly EngiOptions engiOptions;

    public SolveJobService(
        IDocumentStore store,
        IServiceProvider serviceProvider,
        IWebHostEnvironment env,
        IHub sentry,
        ILoggerFactory loggerFactory,
        IOptions<EngiOptions> engiOptions)
        : base(store, serviceProvider, env, sentry, loggerFactory)
    {
        this.engiOptions = engiOptions.Value;

        MaxDocumentsPerBatch = 1;
    }

    protected override string CreateQuery()
    {
        return
@"
declare function filter(b) {
    return b.ProcessedOn === null && b.SentryId === null
}

from SolveJobCommands as c where filter(c) include c.JobAttemptedSnapshotId
";
    }

    protected override async Task ProcessBatchAsync(
        SubscriptionBatch<SolveJobCommand> batch,
        IServiceProvider serviceProvider)
    {
        await using var scope = serviceProvider.CreateAsyncScope();

        var sudoer = KeypairFactory.CreateFromAny(engiOptions.SudoChainMnemonic);

        var chainStateProvider = scope.ServiceProvider.GetRequiredService<LatestChainStateProvider>();

        using var session = batch.OpenAsyncSession();

        foreach (var item in batch.Items)
        {
            var command = item.Result;

            var client = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

            var chainState = await chainStateProvider.GetLatestChainStateAsync();

            try
            {
                await ProcessAsync(command, session, client, sudoer, chainState);
            }
            catch(Exception ex)
            {
                command.SentryId = Sentry.CaptureException(ex, new()
                {
                    ["command"] = command.Id
                }).ToString();

                Logger.LogWarning(ex,
                    "Processing command {command} failed; sentry id={sentryId}.",
                    command.Id, command.SentryId);
            }
        }

        await session.SaveChangesAsync();
    }

    private async Task ProcessAsync(
        SolveJobCommand command,
        IAsyncDocumentSession session,
        SubstrateClient client,
        Keypair sudoer,
        ChainState chainState)
    {
        var account = await client.GetSystemAccountAsync(sudoer.Address);

        var attempt = await session.LoadAsync<JobAttemptedSnapshot>(command.JobAttemptedSnapshotId);

        ulong solutionId = BitConverter.ToUInt64(RandomNumberGenerator.GetBytes(8));

        var solveJobArgs = new SolveJobArguments
        {
            SolutionId = solutionId,
            JobId = attempt.JobId,
            Attempt = new()
            {
                AttemptId = attempt.AttemptId,
                Attempter = attempt.Attempter,
                Tests = command.EngineResult.Tests!
            },
            Author = attempt.Attempter,
            PatchFileUrl = attempt.PatchFileUrl
        };

        var sudoArgs = new SudoCallArguments(solveJobArgs);

        var result = await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<SudoCallArguments>(
                sudoer, sudoArgs, account, ExtrinsicEra.CreateMortal(chainState.LatestFinalizedHeader, 55), chainState, 0), chainState.Metadata);

        command.SolutionId = solutionId;
        command.ResultHash = result;
        command.ProcessedOn = DateTime.UtcNow;
    }
}
