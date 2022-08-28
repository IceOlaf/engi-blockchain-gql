using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Server.Types.Validation;
using ValidationContext = System.ComponentModel.DataAnnotations.ValidationContext;
using ValidationResult = System.ComponentModel.DataAnnotations.ValidationResult;

namespace GraphQL;

public static class ValidationExtensions
{
    public static T? GetOptionalValidatedArgument<T>(this IResolveFieldContext context, string name)
        where T : class
    {
        T? arg;

        try
        {
            arg = context.GetArgument<T?>(name);
        }
        catch (Exception)
        {
            throw new ArgumentValidationException(name, Array.Empty<ValidationResult>());
        }

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

    // simple type

    public static T? GetOptionalValidatedArgument<T>(this IResolveFieldContext context, string name, ValidationAttribute validationAttribute)
    {
        T? arg;

        try
        {
            arg = context.GetArgument<T?>(name);
        }
        catch (Exception)
        {
            throw new ArgumentValidationException(name, Array.Empty<ValidationResult>());
        }

        if (arg == null)
        {
            return default;
        }

        var validationContext = new ValidationContext(arg);

        var result = validationAttribute.GetValidationResult(arg, validationContext);

        if (result != ValidationResult.Success)
        {
            throw new ArgumentValidationException(name, new []
            {
                new ValidationResult(result!.ErrorMessage, result.MemberNames.Any() ? result.MemberNames : new [] { name })
            });
        }

        return arg;
    }

    public static T GetValidatedArgument<T>(this IResolveFieldContext context, string name, ValidationAttribute validationAttribute)
    {
        var arg = GetOptionalValidatedArgument<T>(context, name, validationAttribute);

        if (arg == null)
        {
            throw new ArgumentNullException(name);
        }

        return arg;
    }
}