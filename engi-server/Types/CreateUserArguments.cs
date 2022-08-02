namespace Engi.Substrate.Server.Types;

public class CreateUserArguments
{
    public string Name { get; set; } = null!;

    public string Mnemonic { get; set; } = null!;

    public string? MnemonicSalt { get; set; }

    public string? Password { get; set; }
}