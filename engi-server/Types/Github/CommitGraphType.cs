using GraphQL.Types;

namespace Engi.Substrate.Server.Types.Github;

public class CommitGraphType : ObjectGraphType<Commit>
{
    public CommitGraphType()
    {
        Field(x => x.Sha)
            .Description("The commit SHA.");
        Field(x => x.Message)
            .Description("The commit message.");
        Field(x => x.Author)
            .Description("The author's login.");
        Field(x => x.Committer)
            .Description("The committer's login.");
    }
}