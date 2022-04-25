using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class CreateUserInputType : InputObjectGraphType
{
    public CreateUserInputType()
    {
        Name = "UserInput";

        Field<NonNullGraphType<StringGraphType>>("name");
        Field<NonNullGraphType<StringGraphType>>("mnemonic");
        Field<StringGraphType>("mnemonicSalt");
        Field<StringGraphType>("password");
    }
}