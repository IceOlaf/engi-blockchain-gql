﻿using Engi.Substrate.Pallets;
using Engi.Substrate.Server.Types;
using GraphQL;
using GraphQL.Types;
using Sentry;

namespace Engi.Substrate.Server;

public class EngiQuery : ObjectGraphType
{
    private readonly IServiceProvider serviceProvider;

    public EngiQuery(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;

        FieldAsync<EngiHealthGraphType>("health", resolve: async _ => await GetHealthAsync());

        FieldAsync<AccountInfoGraphType>("account",
            arguments: new QueryArguments(
                new QueryArgument<IdGraphType> { Name = "id" }
            ),
            resolve: async context => await GetAccountAsync(context));

        FieldAsync<ListGraphType<EventRecordGraphType>>("events",
            arguments: new QueryArguments(
                new QueryArgument<IdGraphType> { Name = "blockHash" }),
            resolve: async context => await GetEventsAsync(context));
    }

    private async Task<EngiHealth> GetHealthAsync()
    {
        using var scope = serviceProvider.CreateScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();
        var sentry = scope.ServiceProvider.GetRequiredService<IHub>();

        try
        {
            var chainTask = substrate.GetSystemChainAsync();
            var nameTask = substrate.GetSystemNameAsync();
            var versionTask = substrate.GetSystemVersionAsync();
            var healthTask = substrate.GetSystemHealthAsync();

            await Task.WhenAll(
                chainTask,
                nameTask,
                versionTask,
                healthTask
            );

            return new EngiHealth
            {
                Chain = chainTask.Result,
                NodeName = nameTask.Result,
                Version = versionTask.Result,
                Status = healthTask.Result.Peers > 0 ? EngiHealthStatus.Online : EngiHealthStatus.Offline,
                PeerCount = healthTask.Result.Peers
            };
        }
        catch (Exception ex)
        {
            sentry.CaptureException(ex);

            return new EngiHealth
            {
                Status = EngiHealthStatus.Offline
            };
        }
    }

    private Task<AccountInfo> GetAccountAsync(IResolveFieldContext<object> context)
    {
        using var scope = serviceProvider.CreateScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        string accountId = context.GetArgument<string>("id")!;

        return substrate.GetSystemAccountAsync(Address.From(accountId));
    }

    private async Task<EventRecord[]> GetEventsAsync(IResolveFieldContext<object> context)
    {
        using var scope = serviceProvider.CreateScope();

        var substrate = scope.ServiceProvider.GetRequiredService<SubstrateClient>();

        // TODO: get from cache
        var snapshot = await substrate.GetChainSnapshotAsync();

        string blockHash = context.GetArgument<string>("blockHash")!;

        return await substrate.GetSystemEventsAsync(blockHash, snapshot.Metadata);
    }
}