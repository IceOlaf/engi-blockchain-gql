using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;

namespace Engi.Substrate.Playground;

public static class Program
{
    public static async Task Main()
    {
        
    }

    private static async Task GetSystemEventsForBlockAsync()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9933")
        };

        var client = new SubstrateClient(http);

        var snapshot = await client.GetChainStateAsync();

        var events =
            await client.GetSystemEventsAsync("0x2f4ad438bc18b7f1ca11f80aee7af7fc446288b71eedd860528f70ea98992374",
                snapshot.Metadata);

        return;
    }

    private static async Task InvokeTemplateModuleDoSomethingAsync()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9933")
        };

        var client = new SubstrateClient(http);

        var snapshot = await client.GetChainStateAsync();

        var (template, do_something) = snapshot.Metadata
            .FindPalletCallVariant("TemplateModule", "do_something");

        snapshot.Metadata.VerifySignature(do_something,
            (field, type) => type.FullName == "u32");

        using var ms = new MemoryStream();
        using var writer = new ScaleStreamWriter(ms);

        writer.Write(template.Index);
        writer.Write(do_something.Index);
        writer.Write((uint)5);

        var sender = KeypairFactory.CreateFromMnemonic(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long", "", Wordlists.English);

        var account = await client.GetSystemAccountAsync(sender.Address);

        string result = await client.SignAndAuthorSubmitExtrinsicAsync(
            snapshot, sender, account, ms.ToArray(), Era.Immortal);

        return;
    }

    private static async Task ContractCallErc20BalanceOfExampleAsync()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9933")
        };

        var client = new SubstrateClient(http);

        var response = await client.ContractCallAsync(new ContractCall
        {
            ContractAddress = "5HjZeHL5MPeKRurbg3HuK6PH9L5uAw822EBBNRKr5Xd6GFy2", // comes from upload
            Origin = "5FHneW46xGXgs5mUiveU4sbTyGBzmstUspZC92UhjJM694ty",
            GasLimit = 4999999999999,
            InputData0X = "0x0f755a56" // selector from metadata
                + Hex.GetString(Address.From("5CiPPseXPECbkjWCa6MnjNokrgYjMqmKndv2rSnekmSK2DjL").Raw)
        });

        return;
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

        var snapshot = await client.GetChainStateAsync();
        var account = await client.GetSystemAccountAsync(sender.Address);

        var era = Era.CreateMortal(snapshot.LatestHeader, 55);

        await client.BalanceTransferAsync(snapshot, sender, account, dest, amount, era, tip);
    }
}
