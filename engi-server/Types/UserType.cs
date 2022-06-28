using GraphQL.Types;

namespace Engi.Substrate.Server.Types;

public class UserType : ObjectGraphType<User>
{
    public UserType()
    {
        Field(x => x.Name);
        Field(x => x.Address);
        Field(x => x.CreatedOn);
        Field(x => x.Encoded);
        Field(x => x.Metadata);
    }
}