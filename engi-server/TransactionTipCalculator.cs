using Engi.Substrate.Identity;

namespace Engi.Substrate.Server;

public class TransactionTipCalculator
{
    public Task<byte> CalculateTipAsync(User user)
    {
        return Task.FromResult((byte)1);
    }
}