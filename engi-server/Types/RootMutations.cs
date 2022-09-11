using Engi.Substrate.Server.Types.Authentication;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class RootMutations : ObjectGraphType
{
    public RootMutations()
    {
        Field<AuthMutations>("auth")
            .Resolve(_ => new { });

        Field<CurrencyMutations>("currency")
            .Resolve(_ => new { });

        Field<JobMutations>("jobs")
            .Resolve(_ => new { });
    }
}