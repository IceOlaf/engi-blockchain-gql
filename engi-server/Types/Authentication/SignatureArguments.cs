using System.ComponentModel.DataAnnotations;

namespace Engi.Substrate.Server.Types.Authentication;

public sealed class SignatureArguments : IValidatableObject
{
    [Required]
    public DateTime SignedOn { get; set; }

    [Required]
    public string Value { get; set; } = null!;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!Value.StartsWith("0x"))
        {
            yield return new ValidationResult("Must start with 0x",
                new[] { nameof(Value) });
        }
    }
}