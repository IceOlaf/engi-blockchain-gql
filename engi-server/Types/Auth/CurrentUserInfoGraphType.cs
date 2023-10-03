using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CurrentUserInfoGraphType : ObjectGraphType<CurrentUserInfo>
{
    public CurrentUserInfoGraphType()
    {
        Field(x => x.Email)
            .Description("The user's email.");

        Field(x => x.Display)
            .Description("The user's display name.");

        Field(x => x.ProfileImageUrl, nullable: true)
            .Description("The user's profile image URL.");

        Field(x => x.FreelancerSettings, nullable: true, type: typeof(UserFreelancerSettingsGraphType))
            .Description("The user's freelancer settings (if any).");

        Field(x => x.BusinessSettings, nullable: true, type: typeof(UserBusinessSettingsGraphType))
            .Description("The user's business settings (if any).");

        Field(x => x.EmailSettings, type: typeof(UserEmailSettingsGraphType))
            .Description("The user's e-mail settings.");

        Field(x => x.CreatedOn)
            .Description("The date and time the user registered.");

        Field(x => x.GithubEnrollments, type: typeof(ListGraphType<UserGithubEnrollmentGraphType>))
            .Description("The Github enrollments for this user.");

        Field(x => x.Balance, nullable: true, type: typeof(BigIntGraphType))
            .Description("Balance for this wallet address.");

        Field(x => x.Wallet, nullable: true, type: typeof(AddressGraphType))
            .Description("Wallet address for this user.");
    }
}
