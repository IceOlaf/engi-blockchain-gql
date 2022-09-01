using Engi.Substrate.Jobs;
using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;

namespace Engi.Substrate.Playground;

#pragma warning disable 1998

public static class Program
{
    public static async Task Main()
    {
        
    }

    private static async Task AttemptJobAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        var chainState = await client.GetChainStateAsync();

        var sender = KeypairFactory.CreateFromAny(
            "time treat merit corn crystal fiscal banner zoo jacket pulse frog long");

        var account = await client.GetSystemAccountAsync(sender.Address);

        var args = new AttemptJobArguments
        {
            JobId = 2301417399463059613,
            SubmissionPatchFileUrl = "https://wetransfer.com/patch"
        };

        string result = await client.AttemptJobAsync(chainState, sender, account, args);
        
        return;
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
            Funding = 1000,
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
                    Result = TestResult.Passed,
                    Required = true
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

    private static async Task ContractCallErc20BalanceOfExampleAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        ContractCall call = new ContractCall
        {
            ContractAddress = "5HjZeHL5MPeKRurbg3HuK6PH9L5uAw822EBBNRKr5Xd6GFy2", // comes from upload
            Origin = "5FHneW46xGXgs5mUiveU4sbTyGBzmstUspZC92UhjJM694ty",
            GasLimit = 4999999999999,
            InputData0X = "0x0f755a56" // selector from metadata
                          + Hex.GetString(Address.Parse("5CiPPseXPECbkjWCa6MnjNokrgYjMqmKndv2rSnekmSK2DjL").Raw)
        };

        var response = await client.RpcAsync<ContractCallResponse>("contracts_call", call);

        return;
    }
}
