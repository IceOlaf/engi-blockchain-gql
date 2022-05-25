using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;

namespace Engi.Substrate;

public class SubstrateClient
{
    private static long IdCounter = 0;

    private readonly HttpClient http;

    public SubstrateClient(HttpClient http)
    {
        this.http = http;
    }

    public SubstrateClient(IHttpClientFactory httpClientFactory)
    {
        http = httpClientFactory.CreateClient(nameof(SubstrateClient));
    }

    public async Task<T> RpcAsync<T>(string method, params object[] @params)
    {
        long id = Interlocked.Increment(ref IdCounter);

        var payload = new
        {
            id,
            jsonrpc = "2.0",
            method,
            @params 
        };

        var response = await http.PostAsJsonAsync(string.Empty, payload);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        if (json.TryGetProperty("error", out var error))
        {
            error.TryGetProperty("data", out var data);

            throw new InvalidOperationException(
                $"Substrate error {error.GetProperty("code")}; {error.GetProperty("message")}: {data}");
        }

        T? result = json.GetProperty("result").Deserialize<T>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Converters = {new JsonStringEnumConverter()}
        });

        return result!;
    }

    public async Task<string> RpcAsync(string method, params object[] @params)
    {
        var json = await RpcAsync<JsonElement>(method, @params);

        return json.GetString()!;
    }

    private async Task<T> RpcScaleAsync<T>(Func<ScaleStreamReader, T> parse, string method, params object[] @params)
    {
        string result = await RpcAsync(method, @params);

        using var scale = new ScaleStreamReader(result);

        return parse(scale);
    }

    // system_

    public Task<string> GetSystemChainAsync() => RpcAsync<string>("system_chain");
    public Task<string> GetSystemNameAsync() => RpcAsync<string>("system_name");
    public Task<string> GetSystemVersionAsync() => RpcAsync<string>("system_version");
    public Task<SystemHealth> GetSystemHealthAsync() => RpcAsync<SystemHealth>("system_health");

    // state_

    public Task<RuntimeVersion> GetRuntimeVersionAsync(string hash) => RpcAsync<RuntimeVersion>("state_getRuntimeVersion", hash);

    public Task<RuntimeMetadata> GetStateMetadataAsync()
    {
        return RpcScaleAsync(RuntimeMetadata.Parse, "state_getMetadata");
    }

    public Task<T> GetStateStorageAsync<T>(params string[] @params) => RpcAsync<T>("state_getStorage", @params);

    // chain_

    public Task<string> GetChainBlockHashAsync(ulong number) => RpcAsync("chain_getBlockHash", number);
    public Task<string> GetChainFinalizedHeadAsync() => RpcAsync("chain_getFinalizedHead");
    public Task<Header> GetChainLatestHeaderAsync() => RpcAsync<Header>("chain_getHeader");
    public Task<Header> GetChainHeaderAsync(string hash) => RpcAsync<Header>("chain_getHeader", hash);

    // author_

    public Task<string> AuthorSubmitExtrinsic(byte[] payload) =>
        RpcAsync("author_submitExtrinsic", Hex.GetString0x(payload));

    // composite

    public async Task<AccountInfo> GetSystemAccountAsync(byte[] accountIdBytes)
    {
        if (accountIdBytes == null)
        {
            throw new ArgumentNullException(nameof(accountIdBytes));
        }

        string addressHex = Hex.ConcatGetOxString(
            xxSystem,
            xxAccount,
            Hashing.Blake2Concat(accountIdBytes)
        );

        string result = await GetStateStorageAsync<string>(addressHex);

        var scale = new ScaleStreamReader(result);

        return AccountInfo.Parse(scale);
    }

    public Task<AccountInfo> GetSystemAccountAsync(Address address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        return GetSystemAccountAsync(address.Raw);
    }

    public async Task<ChainSnapshot> GetChainSnapshotAsync()
    {
        var metadataTask = GetStateMetadataAsync();
        var genesisTask = GetChainBlockHashAsync(0);
        var finalizedBlockTask = GetChainFinalizedHeadAsync();
        var latestHeaderTask = GetChainLatestHeaderAsync();

        await Task.WhenAll(
            metadataTask,
            genesisTask,
            finalizedBlockTask,
            latestHeaderTask);

        var runtimeVersionTask = GetRuntimeVersionAsync(finalizedBlockTask.Result);

        return new()
        {
            Metadata = metadataTask.Result,
            GenesisHash = genesisTask.Result,
            FinalizedBlockHash = finalizedBlockTask.Result,
            LatestHeader = latestHeaderTask.Result,
            RuntimeVersion = runtimeVersionTask.Result
        };
    }

    private static readonly byte[] xxAccount = Hashing.Twox128("Account");
    private static readonly byte[] xxSystem = Hashing.Twox128("System");
}