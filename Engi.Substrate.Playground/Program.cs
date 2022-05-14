using Engi.Substrate.Keys;

namespace Engi.Substrate.Playground;

public static class Program
{
    public static async Task Main()
    {
        
    }

    private static async Task BalanceTransferExampleAsync()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("https://westend-rpc.polkadot.io")
        };

        var client = new SubstrateClient(http);

        var sender = KeypairFactory.CreateFromMnemonic(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long", "", Wordlists.English);
        var dest = Address.From("5FZDBCeK9FuUvnny3WhXr62Ah6pneeSaxBWL6osFoUXSszxD");
        ulong amount = 1;
        byte tip = 0;

        var snapshot = await client.GetChainSnapshotAsync();
        var account = await client.GetSystemAccountAsync(sender.Address);

        var era = Era.CreateMortal(snapshot.LatestHeader, 55);

        await client.BalanceTransferAsync(snapshot, sender, account, dest, amount, era, tip);
    }
}
