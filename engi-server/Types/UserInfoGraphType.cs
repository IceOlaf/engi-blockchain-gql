using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserInfoGraphType : ObjectGraphType<UserInfo>
{
    public UserInfoGraphType()
    {
        Field(x => x.Address)
            .Description("The user's address");

        Field(x => x.Display, nullable: true)
            .Description("The user's display name, if available.");

        Field(x => x.ProfileImageUrl, nullable: true)
            .Description("The user's profile image URL, if available.");

        Field(x => x.CreatedOn, nullable: true)
            .Description("The date when this user was created, if available.");

        Field(x => x.CreatedJobsCount)
            .Description("The number of jobs this user has created.");

        Field(x => x.SolvedJobsCount)
            .Description("The number of solutions this user has submitted.");
    }
}
