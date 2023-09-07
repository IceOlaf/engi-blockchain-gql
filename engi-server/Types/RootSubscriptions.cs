using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Engi.Substrate.Identity;
using Engi.Substrate.Jobs;
using Engi.Substrate.Observers;
using Engi.Substrate.Server.Types.Engine;
using GraphQL;
using GraphQL.Server.Transports.AspNetCore.Errors;
using GraphQL.Types;
using Raven.Client.Documents.Changes;
using Raven.Client.Documents.Session;
using Raven.Client.Documents;

namespace Engi.Substrate.Server.Types;

public class RootSubscriptions : ObjectGraphType
{
    public RootSubscriptions()
    {
        Field<HeaderGraphType>("newFinalizedHead")
            .Resolve(context => context.Source)
            .ResolveStream(context =>
            {
                var newHeadObserver = context.RequestServices!
                    .GetServices<IChainObserver>()
                    .OfType<NewHeadChainObserver>()
                    .Single();

                return newHeadObserver.FinalizedHeaders;
            });

        Field<JobDraftGraphType>("draftUpdates")
            .Argument<NonNullGraphType<StringGraphType>>("id")
            .Resolve(context => context.Source)
            .ResolveStreamAsync(SubscribeToJobDraftUpdatesAsync);
            .AuthorizeWithPolicy(PolicyNames.Authenticated);
    }

    private async Task<IObservable<object?>> SubscribeToJobDraftUpdatesAsync(IResolveFieldContext<object?> context)
    {
        string id = context.GetArgument<string>("id");

        await using var scope = context.RequestServices!.CreateAsyncScope();

        // make sure it exists and enforce perms

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        string currentUserId = context.User!.Identity!.Name!;

        var objects = await session
            .LoadAsync<object>(new[] { id, currentUserId });

        var analysis = (JobDraft?)objects[id];
        var currentUser = (User)objects[currentUserId];

        if (analysis == null)
        {
            throw new ExecutionError("Not found") { Code = "NOT_FOUND" };
        }

        if (analysis.CreatedBy != currentUser.Address)
        {
            throw new AccessDeniedError(analysis.Id);
        }

        var store = scope.ServiceProvider.GetRequiredService<IDocumentStore>();

        return store.Changes()
            .ForDocument(id)
            .Where(change => change.Type == DocumentChangeTypes.Put)
            .Select(async change =>
            {
                using var session = store.OpenAsyncSession();

                return await session.LoadAsync<JobDraft>(change.Id);
            })
            .Select(task => task.ToObservable())
            .Concat();
    }
}
