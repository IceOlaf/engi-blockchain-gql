using Engi.Substrate.Identity;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserBusinessSettingsGraphType : ObjectGraphType<UserBusinessSettings>
{
    public UserBusinessSettingsGraphType()
    {
        Field(x => x.CompanyName)
            .Description("The company name.");

        Field(x => x.PreferredLanguages)
            .Description("The preferred languages.");
    }
}
