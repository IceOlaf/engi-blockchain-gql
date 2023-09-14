using Engi.Substrate.Jobs;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types.Engine;

public class JobDraftGraphType : ObjectGraphType<JobDraft>
{
    public JobDraftGraphType()
    {
        Field(x => x.Id);

        Field(x => x.Tests, nullable: true, type: typeof(ListGraphType<StringGraphType>))
            .Description("The test list that must be passing.");

        Field(x => x.IsEditable, nullable: true)
            .Description("Glob pattern for editable files");

        Field(x => x.IsAddable, nullable: true)
            .Description("Glob pattern for addable files");

        Field(x => x.IsDeletable, nullable: true)
            .Description("Glob pattern for deletable files");

        Field(x => x.Funding, nullable: true)
            .Description("How much to award.");

        Field(x => x.Name, nullable: true)
            .Description("Job name.");

        Field<RepositoryAnalysisGraphType>("Analysis")
            .Description("Analysis.")
            .ResolveAsync(GetAnalysis);
    }

    private async Task<object?> GetAnalysis(IResolveFieldContext context)
    {
        await using var scope = context.RequestServices!.CreateAsyncScope();

        using var session = scope.ServiceProvider.GetRequiredService<IAsyncDocumentSession>();

        if (context.Source is JobDraft draft)
        {
            var analysis = await session.LoadAsync<RepositoryAnalysis>(draft.AnalysisId);
            return analysis;
        }

        return null;
    }
}
