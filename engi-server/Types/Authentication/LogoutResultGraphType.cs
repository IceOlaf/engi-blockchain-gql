using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Authentication;

public class LogoutResultGraphType : ObjectGraphType<LogoutResult>
{
    public LogoutResultGraphType()
    {
        Field(x => x.Result)
            .Description("Goodbye.");
    }
}
