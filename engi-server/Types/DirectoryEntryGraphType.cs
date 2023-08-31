using GraphQL.Types;
using Engi.Substrate.Jobs;

namespace Engi.Substrate.Server.Types;

public class DirectoryEntryGraphType: ObjectGraphType<DirectoryEntry>
{
    public DirectoryEntryGraphType()
    {
        Description = "A directory entry.";

        Field(x => x.Path)
            .Description("Path for this directory component.");

        Field(x => x.Name)
            .Description("Name for this directory component.");

        Field(x => x.Type)
            .Description("Type for this directory component.");

        Field(x => x.Extension, nullable: true)
            .Description("File extension for this directory component.");

        Field(x => x.Children, type: typeof(ListGraphType<DirectoryEntryGraphType>))
            .Description("Children of this component.");
    }
}
