using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class ActivityGraphType : ObjectGraphType<Activity>
{
    public ActivityGraphType()
    {
        Field(x => x.Items, type: typeof(ListGraphType<ActivityDailyGraphType>))
            .Description("A list of the daily activity, in ascending date order.");
    }
}
