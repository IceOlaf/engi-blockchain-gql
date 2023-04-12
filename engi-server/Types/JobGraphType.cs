using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobGraphType : ObjectGraphType<Job>
{
    public JobGraphType()
    {
        Description = "An ENGI job.";

        Field("id", x => x.JobId, type: typeof(IdGraphType))
            .Description("The job id on the chain.");
        Field(x => x.Creator)
            .Description("The address of the creator.");
        Field(x => x.Funding)
            .Description("The funding amount.");
        Field(x => x.Repository, type: typeof(RepositoryGraphType))
            .Description("The job's repository information (url, branch, commit hash).");
        Field(x => x.Technologies)
            .Description("The principal technology of the repository.");
        Field(x => x.Name)
            .Description("The job name.");
        Field(x => x.Tests, type: typeof(ListGraphType<TestGraphType>))
            .Description("The collection of tests included in this job.");
        Field(x => x.Requirements, type: typeof(FilesRequirementGraphType))
            .Description("The files requirement of this job.");
        Field(x => x.Solution, type: typeof(SolutionGraphType))
            .Description("The solution to this job, if any.");
        Field(x => x.AttemptCount)
            .Description("The number of attempts for this job.");
        Field(x => x.SolutionUserCount)
            .Description("The number of users who have submitted a solution for this job.");
        Field(x => x.LeadingSolution, nullable: true, type: typeof(SolutionGraphType))
            .Description("The leading solution for this job.");
        Field(x => x.CurrentUserSolution, nullable: true, type: typeof(SolutionGraphType))
            .Description("The solution by the current user, if any.");
        Field(x => x.AverageProgress, nullable: true, type: typeof(FractionalGraphType))
            .Description("The average (median) progress of the best solution by each author. If this job has no solutions, this field will be null.");
        Field(x => x.CreatedOn, type: typeof(BlockReferenceGraphType))
            .Description("Information about the block that created this job.");
        Field(x => x.UpdatedOn, type: typeof(BlockReferenceGraphType))
            .Description("Information about the last block that updated this job.");
        Field(x => x.Status)
            .Description("The current job status; Open, Active, or Complete.");
    }
}
