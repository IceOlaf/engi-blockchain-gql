using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Server.Types.Validation;

namespace GraphQL;

public static class ValidationExtensions
{
    public static T? GetOptionalValidatedArgument<T>(this IResolveFieldContext context, string name)
        where T : class
    {
        var arg = context.GetArgument<T?>(name);

        if (arg == null)
        {
            return null;
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

    public static T GetValidatedArgument<T>(this IResolveFieldContext context, string name) 
        where T : class
    {
        var arg = GetOptionalValidatedArgument<T>(context, name);

        if (arg == null)
        {
            throw new ArgumentNullException(name);
        }

        return arg;
    }
}