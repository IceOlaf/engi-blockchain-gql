using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngineerEarningsGraphType : ObjectGraphType<EngineerEarnings>
{
    public EngineerEarningsGraphType()
    {
        Description = "How much this person is making.";

        Field(x => x.PastDay, type: typeof(BigIntGraphType))
            .Description("Past day earnings.");

        Field(x => x.PastWeek, type: typeof(BigIntGraphType))
            .Description("Past week earnings.");

        Field(x => x.PastMonth, type: typeof(BigIntGraphType))
            .Description("Past month earnings.");

        Field(x => x.Lifetime, type: typeof(BigIntGraphType))
            .Description("Lifetime earnings.");
    }
}
