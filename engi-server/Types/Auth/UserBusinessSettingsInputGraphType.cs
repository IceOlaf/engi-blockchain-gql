using Engi.Substrate.Identity;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserBusinessSettingsInputGraphType : InputObjectGraphType<UserBusinessSettings>
{
    public UserBusinessSettingsInputGraphType()
    {
        Field(x => x.CompanyName)
            .Description("The company name.");

        Field(x => x.PreferredTechnologies)
            .Description("The preferred technologies.");
    }
}
