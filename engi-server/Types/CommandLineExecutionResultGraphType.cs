using Engi.Substrate.Jobs;
using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CommandLineExecutionResultGraphType : ObjectGraphType<CommandLineExecutionResult>
{
    public CommandLineExecutionResultGraphType()
    {
        Field(x => x.Identifier)
            .Description("Id of the command being executed");

        Field(x => x.Stdout, nullable: true)
            .Description("Stdout of the engine command");

        Field(x => x.Stderr, nullable: true)
            .Description("Stderr of the engine command");

        Field(x => x.ReturnCode)
            .Description("Return code of the engine command");
    }
}
