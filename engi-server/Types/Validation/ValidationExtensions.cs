using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Server.Types.Validation;

namespace GraphQL;

public static class ValidationExtensions
{
    public static T GetValidatedArgument<T>(this IResolveFieldContext context, string name)
    {
        var arg = context.GetArgument<T>(name);

        if (arg == null)
        {
            throw new ArgumentNullException(name);
        }

        var results = new List<ValidationResult>();
        var validationContext = new ValidationContext(arg);

        bool isValid = Validator.TryValidateObject(arg, validationContext, results, true);

        if (!isValid)
        {
            throw new ArgumentValidationException(name, results.ToArray());
        }

        return arg;
    }
}