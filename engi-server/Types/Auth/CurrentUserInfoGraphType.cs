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

        Field(x => x.JobPreference)
            .Description("The user's preference for jobs in languages.");

        Field(x => x.CreatedOn)
            .Description("The date and time the user registered.");

        Field(x => x.GithubEnrollments, type: typeof(ListGraphType<UserGithubEnrollmentGraphType>))
            .Description("The Github enrollments for this user.");
    }
}
