using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CreateUserInputType : InputObjectGraphType<CreateUserInput>
{
    public CreateUserInputType()
    {
        Field(x => x.Name);
        Field(x => x.Mnemonic);
        Field(x => x.MnemonicSalt);
        Field(x => x.Password);
    }
}