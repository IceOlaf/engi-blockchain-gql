using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class EngineerGraphType : ObjectGraphType<Engineer>
{
    public EngineerGraphType()
    {
        Description = "How much this person is making.";

        Field(x => x.DisplayName)
            .Description("Display name.");

        Field(x => x.ProfileImageUrl, nullable: true)
            .Description("Profile image location.");

        Field(x => x.Email) 
            .Description("Contact email");

        Field(x => x.Balance, type: typeof(BigIntGraphType))
            .Description("ENGI balance.");

        Field(x => x.BountiesSolved)
            .Description("Bounties solved.");

        Field(x => x.BountiesCreated)
            .Description("Bounties created.");

        Field(x => x.Earnings, type: typeof(EngineerEarningsGraphType))
            .Description("Earnings stats.");

        Field(x => x.Techologies, type: typeof(ListGraphType<TechnologyEnumGraphType>))
            .Description("Technologies used.");

        Field(x => x.RepositoriesWorkedOn, type: typeof(ListGraphType<StringGraphType>))
            .Description("Repositories worked on.");

        Field(x => x.RootOrganization)
            .Description("Root org this person belongs to.");
    }
}
