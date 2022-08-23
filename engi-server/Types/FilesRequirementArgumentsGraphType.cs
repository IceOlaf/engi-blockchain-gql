using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class FilesRequirementArgumentsGraphType : InputObjectGraphType<FilesRequirement>
{
    public FilesRequirementArgumentsGraphType()
    {
        Description = "Job file requirements";

        Field(x => x.IsEditable)
            .Description("Regex or glob pattern that defines the files that can be edited.");
        Field(x => x.IsAddable)
            .Description("Regex or glob pattern that defines the files that can be added.");
        Field(x => x.IsDeletable)
            .Description("Regex or glob pattern that defines the files that can be deleted.");
    }
}