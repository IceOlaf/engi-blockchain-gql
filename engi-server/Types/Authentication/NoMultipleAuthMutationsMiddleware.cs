using GraphQL;
using GraphQL.Instrumentation;

namespace Engi.Substrate.Server.Types.Authentication;

public class NoMultipleAuthMutationsMiddleware : IFieldMiddleware
{
    public ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        if (context.FieldDefinition.ResolvedType is AuthMutations)
        {
            if (context.SubFields?.Count > 1)
            {
                throw new ExecutionError("A single auth mutation is allowed.");
            }
        }

        return next(context);
    }
}