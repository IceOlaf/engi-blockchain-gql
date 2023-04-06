using Engi.Substrate.Identity;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserFreelancerSettingsGraphType : ObjectGraphType<UserFreelancerSettings>
{
    public UserFreelancerSettingsGraphType()
    {
        Field(x => x.JobPreference)
            .Description("The user's preference for jobs in technologies.");
    }
}
