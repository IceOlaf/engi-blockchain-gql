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

        if (Errors.Length == 0)
        {
            throw new ArgumentException("Errors cannot be an empty array.");
        }
    }
}