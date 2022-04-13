using GraphQL.Types;

namespace Engi.Substrate.Server.Schemas;

public class EngiHealthSchema : Schema
{
    public EngiHealthSchema(IServiceProvider serviceProvider)
    {
        Query = new EngiHealthQuery(serviceProvider);
    }
}