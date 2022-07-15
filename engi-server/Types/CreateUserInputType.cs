using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CreateUserInputType : InputObjectGraphType<CreateUserInput>
{
    public CreateUserInputType()
    {
        Field(x => x.Name);
        Field(x => x.Mnemonic);
        Field(x => x.MnemonicSalt, nullable: true);
        Field(x => x.Password, nullable: true);
    }
}