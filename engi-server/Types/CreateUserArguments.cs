using System.ComponentModel.DataAnnotations;
using Engi.Substrate.Keys;

namespace Engi.Substrate.Server.Types;

public class CreateUserArguments : IValidatableObject
{
    [Required, StringLength(50, MinimumLength = 4)]
    public string Name { get; set; } = null!;

    [Required]
    public string Mnemonic { get; set; } = null!;

    public string? MnemonicSalt { get; set; }

    public string? Password { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        bool isRaw;

        var results = new List<ValidationResult>();

        try
        {
            KeypairFactory.ValidateMnemonic(Mnemonic, Wordlists.English, out var mnemonicIndices);

            isRaw = mnemonicIndices == null;
        }
        catch (ArgumentException ex)
        {
            results.Add(new ValidationResult(ex.Message, new[] { nameof(Mnemonic) }));
            
            return results;
        }

        if (isRaw)
        {
            if (!string.IsNullOrEmpty(MnemonicSalt))
            {
                results.Add(new ValidationResult(
                    "A raw seed cannot be used in conjuction with a mnemonic salt.", new [] { nameof(MnemonicSalt) }));

                return results;
            }
        }

        return results;
    }
}