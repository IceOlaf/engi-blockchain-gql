using Engi.Substrate.Identity;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserEmailSettingsGraphType : ObjectGraphType<UserEmailSettings>
{
    public UserEmailSettingsGraphType()
    {
        Field(x => x.WeeklyNewsletter)
            .Description("The user's weekly newsletter e-mail setting.");

        Field(x => x.JobAlerts)
            .Description("The user's job alerts e-mail setting.");

        Field(x => x.TechnicalUpdates)
            .Description("The user's technical updates e-mail setting.");
    }
}
