using Engi.Substrate.Jobs;
using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;
using Engi.Substrate.Server;
using Engi.Substrate.Server.Indexing;
using Raven.Client.Documents;

namespace Engi.Substrate.Playground;

#pragma warning disable 1998

public static class Program
{
    public static async Task Main()
    {

    }

    private static async Task CreateIndexingJobAsync()
    {
        var store = new DocumentStore
        {
            Urls = new[] { "http://localhost:8080" },
            Database = "engi-local"
        };

        store.Initialize();

        using var session = store.OpenAsyncSession();

        await session.StoreAsync(new ExpandedBlock(1));

        await session.SaveChangesAsync();
    }

    private static async Task ExpandBlockAsync()
    {
        const int number = 1086;

        var client = new SubstrateClient("http://localhost:9933");

        var meta = await client.GetStateMetadataAsync();
        var blockHash = await client.GetChainBlockHashAsync(number);
        var block = await client.GetChainBlockAsync(blockHash);
        var events = await client.GetSystemEventsAsync(blockHash, meta);

        var expanded = new ExpandedBlock(number);
            
        expanded.Fill(block!.Block, events, meta);

        var store = new DocumentStore
        {
            Urls = new[] { "http://localhost:8080" },
            Database = "engi-local"
        };

        store.Conventions.Serialization = new EngiSerializationConventions();

        store.Initialize();

        using var session = store.OpenAsyncSession();

        await session.StoreAsync(expanded);

        await session.SaveChangesAsync();

        return; // set breakpoint here
    }

    private static async Task CreateJobAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var chainState = await client.GetChainStateAsync();

        var sender = KeypairFactory.CreateFromAny(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long");

        var account = await client.GetSystemAccountAsync(sender.Address);

        var args = new CreateJobArguments
        {
            Funding = 10,
            RepositoryUrl = "https://github.com/ravendb/ravendb",
            BranchName = "master",
            CommitHash = "119158985933033e05de60533d353b7599b0bbab",
            Language = Language.CSharp,
            Name = "Test job",
            Tests = new []
            {
                new Test
                {
                    Id = "test-1",
                    Result = TestResult.Failed,
                    ResultMessage = "Tests failed.",
                    Required = TestResult.Passed,
                    RequiredMessage = "Tests passed."
                }
            },
            FilesRequirement = new FilesRequirement
            {
                IsEditable = "*.cs",
                IsAddable = "*.cs",
                IsDeletable = "*.cs"
            }
        };

        await client.CreateJobAsync(chainState, sender, account, args);

        return; // set breakpoint here
    }

    private static async Task GetSystemEventsForBlockAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var snapshot = await client.GetChainStateAsync();

        var events =
            await client.GetSystemEventsAsync("0x2f4ad438bc18b7f1ca11f80aee7af7fc446288b71eedd860528f70ea98992374",
                snapshot.Metadata);

        return;
    }

    private static async Task InvokeTemplateModuleDoSomethingAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var snapshot = await client.GetChainStateAsync();

        var (template, do_something) = snapshot.Metadata
            .FindPalletCallVariant("TemplateModule", "do_something");

        snapshot.Metadata.VerifySignature(do_something,
            (field, type, _) => type.FullName == "u32");

        using var writer = new ScaleStreamWriter();

        writer.Write(template.Index);
        writer.Write(do_something.Index);
        writer.Write((uint)5);

        var sender = KeypairFactory.CreateFromAny(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long");

        var account = await client.GetSystemAccountAsync(sender.Address);

        string result = await client.SignAndAuthorSubmitExtrinsicAsync(
            snapshot, sender, account, writer.GetBytes(), ExtrinsicEra.Immortal);

        return;
    }

    private static async Task ContractCallErc20BalanceOfExampleAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        ContractCall call = new ContractCall
        {
            ContractAddress = "5HjZeHL5MPeKRurbg3HuK6PH9L5uAw822EBBNRKr5Xd6GFy2", // comes from upload
            Origin = "5FHneW46xGXgs5mUiveU4sbTyGBzmstUspZC92UhjJM694ty",
            GasLimit = 4999999999999,
            InputData0X = "0x0f755a56" // selector from metadata
                + Hex.GetString(Address.From("5CiPPseXPECbkjWCa6MnjNokrgYjMqmKndv2rSnekmSK2DjL").Raw)
        };

        var response = await client.RpcAsync<ContractCallResponse>("contracts_call", call);

        return;
    }

    private static async Task BalanceTransferAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var sender = KeypairFactory.CreateFromAny(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long");
        var dest = Address.From("5FZDBCeK9FuUvnny3WhXr62Ah6pneeSaxBWL6osFoUXSszxD");
        ulong amount = 1;
        byte tip = 0;

        var snapshot = await client.GetChainStateAsync();
        var account = await client.GetSystemAccountAsync(sender.Address);

        var era = ExtrinsicEra.CreateMortal(snapshot.LatestFinalizedHeader, 55);

        await client.BalanceTransferAsync(snapshot, sender, account, dest, amount, era, tip);
    }

    private static async Task InspectMetadataAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var metadata = await client.GetStateMetadataAsync();

        return; // set breakpoint here
    }

    private static async Task InspectWestendMetadataAsync()
    {
        var client = new SubstrateClient("https://westend-rpc.polkadot.io");

        var metadata = await client.GetStateMetadataAsync();

        return; // set breakpoint here
    }
}
