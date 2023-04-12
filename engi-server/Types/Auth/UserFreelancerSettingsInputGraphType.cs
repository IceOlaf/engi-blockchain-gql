using Engi.Substrate.Identity;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserFreelancerSettingsInputGraphType : InputObjectGraphType<UserFreelancerSettings>
{
    public UserFreelancerSettingsInputGraphType()
    {
        Field(x => x.JobPreference)
            .Description("The user's preference for jobs in technologies.");
    }
}
