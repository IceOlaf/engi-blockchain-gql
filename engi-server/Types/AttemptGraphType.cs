using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AttemptGraphType : ObjectGraphType<Attempt>
{
    public AttemptGraphType()
    {
        Description = "An attempt to solve an ENGI job.";

        // TODO: https://github.com/graphql-dotnet/graphql-dotnet/issues/3303
        Field(x => x.AttemptId, type: typeof(IdGraphType))
            .Description("The id of the attempt.");
        Field(x => x.Attempter, type: typeof(AddressGraphType))
            .Description("The address of the attempter.");
        Field(x => x.Tests, type: typeof(ListGraphType<TestAttemptGraphType>))
            .Description("The recorded tests in this attempt.");
    }
}