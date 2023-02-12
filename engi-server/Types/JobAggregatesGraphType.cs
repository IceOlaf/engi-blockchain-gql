using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class JobAggregatesGraphType : ObjectGraphType<JobAggregateIndex.Result>
{
    public JobAggregatesGraphType()
    {
        Field(x => x.ActiveJobCount)
            .Description("Total number of active jobs.");

        Field(x => x.TotalAmountFunded)
            .Description("Total amount funded.");

        Field(x => x.LanguageCount)
            .Description("Total number of languages used in jobs.");
    }
}
