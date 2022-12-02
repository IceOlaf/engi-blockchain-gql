using Engi.Substrate.Server.Types.Analysis;
using Engi.Substrate.Server.Types.Authentication;
using Engi.Substrate.Server.Types.Github;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class RootMutations : ObjectGraphType
{
    public RootMutations()
    {
        Field<AnalysisMutations>("analysis")
            .Resolve(_ => new { });

        Field<AuthMutations>("auth")
            .Resolve(_ => new { });

        Field<CurrencyMutations>("currency")
            .Resolve(_ => new { });

        Field<GithubMutations>("github")
            .Resolve(_ => new { });

        Field<JobMutations>("jobs")
            .Resolve(_ => new { });

        Field<UserMutations>("user")
            .Resolve(_ => new { });
    }
}