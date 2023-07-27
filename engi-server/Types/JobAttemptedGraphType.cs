using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobAttemptedGraphType : ObjectGraphType<JobAttemptedSnapshot>
{
    public JobAttemptedGraphType()
    {
        Description = "An attempt to solve an ENGI job.";

        // TODO: https://github.com/graphql-dotnet/graphql-dotnet/issues/3303
        Field(x => x.AttemptId, type: typeof(IdGraphType))
            .Description("The id of the attempt.");
        Field(x => x.JobId, type: typeof(IdGraphType))
            .Description("The id of the associated job.");
        Field(x => x.Attempter, type: typeof(AddressGraphType))
            .Description("The address of the attempter.");
        Field(x => x.PatchFileUrl)
            .Description("The patchfile url.");
        Field(x => x.SnapshotOn, type: typeof(BlockReferenceGraphType))
            .Description("The block this attemp was included in.");
        Field(x => x.DispatchedOn, nullable: true)
            .Description("Date of the attempt.");
    }
}
