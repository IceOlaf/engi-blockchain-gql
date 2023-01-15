using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UpdateUserArgumentsGraphType : InputObjectGraphType<UpdateUserArguments>
{
    public UpdateUserArgumentsGraphType()
    {
        Field(x => x.Email, nullable: true)
            .Description("The user's e-mail name.");

        Field(x => x.Display, nullable: true)
            .Description("The user's display name.");

        Field(x => x.FreelancerSettings, nullable: true, type: typeof(UserFreelancerSettingsInputGraphType))
            .Description("The user's freelancer settings (if any).");

        Field(x => x.EmailSettings, nullable: true, type: typeof(UserEmailSettingsInputGraphType))
            .Description("The user's e-mail settings.");
    }
}
