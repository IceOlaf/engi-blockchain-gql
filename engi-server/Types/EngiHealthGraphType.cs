using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngiHealthGraphType : ObjectGraphType<EngiHealth>
{
    public EngiHealthGraphType()
    {
        Description = "A type containing information about the chain and its current health status.";

        Field(x => x.Chain)
            .Description("The name of the chain.");
        Field(x => x.NodeName)
            .Description("The name of the node.");
        Field(x => x.Version)
            .Description("The chain version.");
        Field(x => x.Status)
            .Description("The current status of the chain.");
        Field(x => x.PeerCount, nullable: true)
            .Description("The number of peers.");
    }
}
