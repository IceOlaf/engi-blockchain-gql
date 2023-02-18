using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ActivityArgumentsGraphType : InputObjectGraphType<ActivityArguments>
{
    public ActivityArgumentsGraphType()
    {
        Field(x => x.DayCount)
            .Description("The number of days to include.");

        Field(x => x.MaxCompletedCount)
            .Description("The maximum number of completed results per day.");

        Field(x => x.MaxNotCompletedCount)
            .Description("The maximum number of not completed results per day.");
    }
}
