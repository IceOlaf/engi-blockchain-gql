using System.Reactive.Linq;
using System.Security.Cryptography;
using System.Text;
using Engi.Substrate.Indexing;
using Engi.Substrate.Jobs;
using Engi.Substrate.Keys;
using Engi.Substrate.Pallets;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Encodings;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;
using Raven.Client.Documents;

namespace Engi.Substrate.Playground;

#pragma warning disable 1998

public static class Program
{
    private static readonly Keypair AliceKeyPair = KeypairFactory.CreateFromAny("0xe5be9a5092b81bca64be81d212e7f2f9eba183bb7a90954f7b76361f6edb5c0a");
    private static readonly Keypair GeorgiosdKeypair = KeypairFactory.CreateFromAny("ridge accuse cotton debate step theory fade bench flock liar seek day");

    private static readonly Keypair[] TestKeypairs =
    {
        KeypairFactory.CreateFromAny("time treat merit corn crystal fiscal banner zoo jacket pulse frog long"),
        KeypairFactory.CreateFromAny("shove matter cousin govern glare space survey congress torch easy girl profit"),
        KeypairFactory.CreateFromAny("vocal awake scan share position often baby example segment skill perfect unique"),
        KeypairFactory.CreateFromAny("build genre people salute buddy trash polar hero purse hire caught tail")
    };

    public static async Task Main()
    {
    }

    private static async Task EncryptKeypairWithEngiPublicKey(Keypair keypair)
    {
        var http = new HttpClient();

        var engiPublicKeyData = await http.GetByteArrayAsync("http://localhost:5000/api/engi/public-key");

        var publicKeyParameters = PublicKeyFactory.CreateKey(engiPublicKeyData);

        var e = new Pkcs1Encoding(new RsaEngine());

        e.Init(true, publicKeyParameters);

        var keypairPkcs8 = keypair.ExportToPkcs8();

        var encrypted = e.ProcessBlock(keypairPkcs8, 0, keypairPkcs8.Length);

        Console.WriteLine(Convert.ToBase64String(encrypted));

        //var rsa = new RSACryptoServiceProvider();
        //rsa.ImportRSAPublicKey();
        
        //var key = new RSACryptoServiceProvider(publicKeyParameters);
    }

    private static void CreateLoginPayload(Keypair keypair)
    {
        var now = DateTimeOffset.UtcNow;

        string payload = $"{keypair.Address.Id}|{now.ToUnixTimeMilliseconds()}";

        var signature = keypair.Sign(Encoding.UTF8.GetBytes(payload));

        Console.WriteLine($@"
args: {{
    address: ""{keypair.Address.Id}"",
    signedOn: ""{now.UtcDateTime.ToString("o")}"",
    signedRequestHex: ""{Hex.GetString0X(signature)}""
}}
");

        return;
    }

    private static async Task JobWorkflowTestCaseAsync()
    {
        using var store = new DocumentStore
        {
            Urls = new[] { "http://localhost:8080 " },
            Database = "engi-local"
        };

        store.Conventions.Serialization = new EngiSerializationConventions();

        store.Initialize();

        using var session = store.OpenAsyncSession();

        session.Advanced.MaxNumberOfRequestsPerSession = 100000;

        await CreateJobAsync(GeorgiosdKeypair);

        var jobDocumentChange = await store.Changes()
            .ForDocumentsInCollection<JobSnapshot>().FirstAsync();

        var job = await session.LoadAsync<JobSnapshot>(jobDocumentChange.Id);

        var attemptIds = new ulong[TestKeypairs.Length];

        for (var index = 0; index < TestKeypairs.Length; index++)
        {
            var keypair = TestKeypairs[index];

            await AttemptJobAsync(job.JobId, keypair);

            var attemptDocumentChange = await store.Changes()
                .ForDocumentsInCollection<JobAttemptedSnapshot>()
                .FirstAsync();

            var attempt = await session.LoadAsync<JobAttemptedSnapshot>(attemptDocumentChange.Id);

            attemptIds[index] = attempt.AttemptId;
        }

        // solve one test each

        ulong solutionIdCounter = 0;

        for (var index = 0; index < TestKeypairs.Length; index++)
        {
            var keypair = TestKeypairs[index];

            await SolveJobAsync(job.JobId, Interlocked.Add(ref solutionIdCounter, 100), AliceKeyPair, keypair.Address, tests =>
            {
                tests[index].Result = TestResult.Passed;
                tests[index].FailedResultMessage = null;
            });

            var solutionChange = await store.Changes()
                .ForDocumentsInCollection<SolutionSnapshot>()
                .FirstAsync();

            var solution = await session.LoadAsync<SolutionSnapshot>(solutionChange.Id);
        }

        // finally pick a dude to solve all

        var solver = TestKeypairs[RandomNumberGenerator.GetInt32(0, TestKeypairs.Length)];

        await SolveJobAsync(job.JobId, Interlocked.Add(ref solutionIdCounter, 100), AliceKeyPair, solver.Address,
            tests =>
            {
                foreach (var test in tests)
                {
                    test.Result = TestResult.Passed;
                    test.FailedResultMessage = null;
                }
            });

        return;
    }

    private static async Task SolveJobAsync(
        ulong jobId,
        ulong solutionId,
        Keypair sudoer, 
        Address solver,
        Action<TestAttempt[]>? mutateTests = null)
    {
        var client = new SubstrateClient("http://localhost:9933");

        var chainState = await client.GetChainStateAsync();

        var account = await client.GetSystemAccountAsync(sudoer.Address);

        var callExtrinsic = new SolveJobArguments
        {
            SolutionId = solutionId,
            JobId = jobId,
            Attempt = new()
            {
                AttemptId = 100,
                Attempter = solver,
                Tests = new TestAttempt[]
                {
                    new()
                    {
                        Id = "test-1",
                        Result = TestResult.Failed,
                        FailedResultMessage = "Failed 1"
                    },
                    new()
                    {
                        Id = "test-2",
                        Result = TestResult.Failed,
                        FailedResultMessage = "Failed 2"
                    },
                    new()
                    {
                        Id = "test-3",
                        Result = TestResult.Failed,
                        FailedResultMessage = "Failed 3"
                    },
                    new()
                    {
                        Id = "test-4",
                        Result = TestResult.Passed,
                        FailedResultMessage = "Failed 4"
                    }
                }
            },
            Author = solver,
            PatchFileUrl = "https://test.com"
        };

        if (mutateTests != null)
        {
            mutateTests(callExtrinsic.Attempt.Tests);
        }

        var args = new SudoCallArguments(callExtrinsic);

        var result = await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<SudoCallArguments>(
                AliceKeyPair, args, account, ExtrinsicEra.Immortal, chainState, 0), chainState.Metadata);

        return;
    }

    private static async Task ReadJobFromChainStorageAsync()
    {
        var client = new SubstrateClient("http://localhost:9933");

        ulong jobId = 13575614961675953037;

        string snapshotStorageKey = StorageKeys.Jobs.ForJobId(jobId);

        var job = await client.GetStateStorageAsync(snapshotStorageKey,
            reader => JobSnapshot.Parse(reader, new BlockReference()));

        return;
    }

    private static async Task AttemptJobAsync(ulong jobId, Keypair sender)
    {
        var client = new SubstrateClient("http://localhost:9933");

        var chainState = await client.GetChainStateAsync();

        var account = await client.GetSystemAccountAsync(sender.Address);

        var args = new AttemptJobArguments
        {
            JobId = jobId,
            SubmissionPatchFileUrl = $"https://wetransfer.com/{Guid.NewGuid()}"
        };

        string result = await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<AttemptJobArguments>(sender, args, account, ExtrinsicEra.Immortal, chainState, 0), 
            chainState.Metadata);
        
        return;
    }

    private static async Task CreateJobAsync(Keypair sender, int numberOfTests = 4)
    {
        var client = new SubstrateClient("http://localhost:9933");

        var chainState = await client.GetChainStateAsync();

        var account = await client.GetSystemAccountAsync(sender.Address);

        var args = new CreateJobArguments
        {
            Funding = 5000,
            RepositoryUrl = "https://github.com/ravendb/ravendb",
            BranchName = "master",
            CommitHash = "119158985933033e05de60533d353b7599b0bbab",
            Language = Language.CSharp,
            Name = "Test job",
            Tests = Enumerable.Range(0, numberOfTests)
                .Select(offset => new Test
                {
                    Id = $"test-{offset + 1}",
                    AnalysisResult = TestResult.Passed,
                    Required = true
                })
                .ToArray(),
            FilesRequirement = new FilesRequirement
            {
                IsEditable = "*.cs",
                IsAddable = "*.cs",
                IsDeletable = "*.cs"
            }
        };

        await client.AuthorSubmitExtrinsicAsync(
            new SignedExtrinsicArguments<CreateJobArguments>(sender, args, account, ExtrinsicEra.Immortal, chainState, 0),
            chainState.Metadata);

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
