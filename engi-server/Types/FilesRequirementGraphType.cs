using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class FilesRequirementGraphType : ObjectGraphType<FilesRequirement>
{
    public FilesRequirementGraphType()
    {
        Description = "Requirements on files for an ENGI job.";

        Field(x => x.IsEditable, nullable: true)
            .Description("Regex or glob pattern that defines the files that can be edited.");
        Field(x => x.IsAddable, nullable: true)
            .Description("Regex or glob pattern that defines the files that can be added.");
        Field(x => x.IsDeletable, nullable: true)
            .Description("Regex or glob pattern that defines the files that can be deleted.");
    }
}
