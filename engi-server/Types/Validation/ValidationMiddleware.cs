using System.Collections;
using System.ComponentModel.DataAnnotations;
using GraphQL;
using GraphQL.Instrumentation;

namespace Engi.Substrate.Server.Types.Validation;

public class ValidationMiddleware : IFieldMiddleware
{
    public async ValueTask<object?> ResolveAsync(IResolveFieldContext context, FieldMiddlewareDelegate next)
    {
        try
        {
            return await next(context);
        }
        catch (ArgumentValidationException validationException)
        {
            context.Errors.Add(new ModelValidationError(validationException.Errors));

            return null;
        }
    }

    class ModelValidationError : ExecutionError
    {
        public ModelValidationError(IEnumerable<ValidationResult> results)
            : base("Model validation failed.", GetData(results))
        {
            Code = "MODEL_VALIDATION";
        }

        private static IDictionary GetData(IEnumerable<ValidationResult> results)
        {
            return results.ToDictionary(
                x => ToCamelCase(x.MemberNames.First()),
                x => x.ErrorMessage);
        }

        private static string ToCamelCase(string s)
        {
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }
    }
}