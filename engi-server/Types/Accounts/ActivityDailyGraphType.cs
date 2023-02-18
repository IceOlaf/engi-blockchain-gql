using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ActivityDailyGraphType : ObjectGraphType<ActivityDaily>
{
    public ActivityDailyGraphType()
    {
        Field(x => x.Date)
            .Description("The date that the activity took place.");

        Field(x => x.Completed, type: typeof(ListGraphType<JobSummaryGraphType>))
            .Description("The jobs that were completed that day.");

        Field(x => x.NotCompleted, type: typeof(ListGraphType<JobSummaryGraphType>))
            .Description("The jobs updated that day but whose status is not Completed.");
    }
}
