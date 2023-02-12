using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserInfoGraphType : ObjectGraphType<UserInfo>
{
    public UserInfoGraphType()
    {
        Field(x => x.Address)
            .Description("The user's address");

        Field(x => x.CreatedJobsCount)
            .Description("The number of jobs this user has created.");

        Field(x => x.SolvedJobsCount)
            .Description("The number of solutions this user has submitted.");
    }
}
