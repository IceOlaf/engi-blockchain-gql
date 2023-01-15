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

        Field(x => x.ProfileImageUrl, nullable: true)
            .Description("The user's profile image URL. Must reside in the pre-approved S3 bucket.");

        Field(x => x.FreelancerSettings, nullable: true, type: typeof(UserFreelancerSettingsInputGraphType))
            .Description("The user's freelancer settings (if any).");

        Field(x => x.BusinessSettings, nullable: true, type: typeof(UserBusinessSettingsInputGraphType))
            .Description("The user's business settings (if any).");

        Field(x => x.EmailSettings, nullable: true, type: typeof(UserEmailSettingsInputGraphType))
            .Description("The user's e-mail settings.");
    }
}
