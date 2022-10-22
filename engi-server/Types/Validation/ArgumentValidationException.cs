using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Validation;

internal class ArgumentValidationException : Exception
{
    public string ArgumentName { get; }
    
    public ValidationResult[] Errors { get; }

    public ArgumentValidationException(string argumentName, ValidationResult[] errors)
        : base($"Argument {argumentName} failed validation.")
    {
        ArgumentName = argumentName ?? throw new ArgumentNullException(nameof(argumentName));
        Errors = errors ?? throw new ArgumentNullException(nameof(errors));

        if (!Errors.Any())
        {
            Errors = new[]
            {
                new ValidationResult(
                    "Model validation failed, please check the data types of your arguments.", 
                    new [] { argumentName })
            };
        }
    }

    public ArgumentValidationException(string argumentName, string propertyName, string message)
        : this(argumentName, new[] { new ValidationResult(message, new[] { propertyName }) })
    { }

    public ArgumentValidationException(string argumentName, string message)
        : this(argumentName, argumentName, message)
    { }

    public ArgumentValidationException(ArgumentException ex)
        : this(ex.ParamName ?? string.Empty, ex.Message)
    { }
}