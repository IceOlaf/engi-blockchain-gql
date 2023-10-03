using GraphQL.Types;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class DirectoryEntryGraphType: ObjectGraphType<DirectoryEntry>
{
    public DirectoryEntryGraphType()
    {
        Description = "A directory entry.";

        Field(x => x.path)
            .Description("Path for this directory component.");

        Field(x => x.name)
            .Description("Name for this directory component.");

        Field(x => x.type)
            .Description("Type for this directory component.");

        Field(x => x.extension, nullable: true)
            .Description("File extension for this directory component.");

        Field(x => x.children, type: typeof(ListGraphType<DirectoryEntryGraphType>))
            .Description("Children of this component.");
    }
}
