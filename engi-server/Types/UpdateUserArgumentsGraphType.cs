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

        Field(x => x.JobPreference, nullable: true)
            .Description("The user's preference for jobs in languages.");
    }
}
