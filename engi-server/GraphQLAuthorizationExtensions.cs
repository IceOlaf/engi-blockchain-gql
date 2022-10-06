using GraphQL;
using GraphQL.Builders;

namespace Engi.Substrate.Server;

public static class GraphQLAuthorizationExtensions
{
    public static FieldBuilder<TSourceType, TReturnType> AllowAnonymous<TSourceType, TReturnType>(
        this FieldBuilder<TSourceType, TReturnType> builder)
    {
        builder.FieldType.AllowAnonymous();
        return builder;
    }
}