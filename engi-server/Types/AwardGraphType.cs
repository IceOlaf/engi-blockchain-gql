using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class AwardGraphType : ObjectGraphType<Award>
{
    public AwardGraphType()
    {
        Description = "An award to solve an ENGI job.";

        Field(x => x.Who, type: typeof(AddressGraphType))
            .Description("The address of the attempter.");

        Field(x => x.Amount, type: typeof(BigIntGraphType))
            .Description("Amount awarded.");
    }
}
