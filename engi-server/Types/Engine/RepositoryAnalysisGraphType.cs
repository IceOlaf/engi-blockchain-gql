using Engi.Substrate.Jobs;
using GraphQL;
using GraphQL.Types;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Server.Types.Engine;

public class RepositoryAnalysisGraphType : ObjectGraphType<RepositoryAnalysis>
{
    public RepositoryAnalysisGraphType()
    {
        Field(x => x.Id);

        Field(x => x.RepositoryUrl)
            .Description("The repository URL analyzed.");

        Field(x => x.Branch)
            .Description("The branch analyzed.");

        Field(x => x.Commit)
            .Description("The commit SHA for the tree analyzed.");

        Field(x => x.CreatedOn)
            .Description("The datetime the analysis was created.");

        Field(x => x.CreatedBy)
            .Description("The address of the user that created the analysis.");

        Field(x => x.Status)
            .Description("The status denoting the current state of the analysis.");

        Field(x => x.Technologies, type: typeof(ListGraphType<TechnologyEnumGraphType>), nullable: true)
            .Description("If the analysis was completed, the dominant language of the repository.");

        Field(x => x.DirectoryEntries, nullable: true)
            .Description("If the analysis was completed, the list of files processed.");

        Field(x => x.Complexity, type: typeof(RepositoryComplexityGraphType), nullable: true)
            .Description("If the analysis was completed, information about the complexity of the repository.");

        Field(x => x.Tests, type: typeof(ListGraphType<TestAttemptGraphType>), nullable: true)
            .Description("If the analysis was completed, the list of tests and their status.");

        Field(x => x.ExecutionResult, type: typeof(CommandLineExecutionResultGraphType))
            .Description("If the analysis was completed, the command line execution result.");

        Field(x => x.ProcessedOn, nullable: true)
            .Description("The datetime the analysis was processed (completed or failed).");
    }
}
