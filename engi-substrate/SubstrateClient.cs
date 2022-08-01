using System.Net.Http.Json;
using System.Text.Json;
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

    public SubstrateClient(string uri)
    {
        http = new HttpClient
        {
            BaseAddress = new Uri(uri)
        };
    }

    public async Task<T?> RpcAsync<T>(string method, params object[] @params)
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

        T? result = json.GetProperty("result").Deserialize<T>(SubstrateJsonSerializerOptions.Default);

        return result;
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

    public Task<string> GetSystemChainAsync() => RpcAsync<string>(ChainKeys.SystemChain)!;
    public Task<string> GetSystemNameAsync() => RpcAsync<string>(ChainKeys.SystemName)!;
    public Task<string> GetSystemVersionAsync() => RpcAsync<string>(ChainKeys.SystemVersion)!;
    public Task<SystemHealth> GetSystemHealthAsync() => RpcAsync<SystemHealth>(ChainKeys.SystemHealth)!;

    // state_

    public Task<RuntimeVersion> GetRuntimeVersionAsync(string hash) => RpcAsync<RuntimeVersion>("state_getRuntimeVersion", hash)!;

    public Task<RuntimeMetadata> GetStateMetadataAsync()
    {
        return RpcScaleAsync(RuntimeMetadata.Parse, ChainKeys.StateGetMetadata);
    }

    public Task<T?> GetStateStorageAsync<T>(string key, string? blockHash = null)
    {
        var @params = new[] { key };

        if (blockHash != null)
        {
            Array.Resize(ref @params, 2);

            @params[1] = blockHash;
        }

        return RpcAsync<T>(ChainKeys.StateGetStorage, @params);
    }

    // chain_

    public Task<SignedBlock?> GetChainBlockAsync(string hash) => RpcAsync<SignedBlock>(ChainKeys.ChainGetBlock, hash);
    public Task<SignedBlock> GetChainBlockAsync() => RpcAsync<SignedBlock>(ChainKeys.ChainGetBlock)!;
    public Task<string> GetChainBlockHashAsync(ulong number) => RpcAsync(ChainKeys.ChainGetBlockHash, number);
    public Task<string> GetChainFinalizedHeadAsync() => RpcAsync(ChainKeys.ChainGetFinalizedHead);
    public Task<Header> GetChainLatestHeaderAsync() => RpcAsync<Header>(ChainKeys.ChainGetHeader)!;
    public Task<Header?> GetChainHeaderAsync(string hash) => RpcAsync<Header>(ChainKeys.ChainGetHeader, hash);

    // author_

    public Task<string> AuthorSubmitExtrinsicAsync(byte[] payload) =>
        RpcAsync(ChainKeys.AuthorSubmitExtrinsic, Hex.GetString0X(payload));

    // composite

    public async Task<AccountInfo> GetSystemAccountAsync(byte[] accountIdBytes)
    {
        if (accountIdBytes == null)
        {
            throw new ArgumentNullException(nameof(accountIdBytes));
        }

        string addressHex = Hex.ConcatGetOXString(
            StorageKeys.System, 
            StorageKeys.Account,
            Hashing.Blake2Concat(accountIdBytes)
        );

        string? result = await GetStateStorageAsync<string>(addressHex);

        if (result == null)
        {
            throw new KeyNotFoundException(addressHex);
        }

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

    public async Task<EventRecord[]> GetSystemEventsAsync(string blockHash, RuntimeMetadata meta)
    {
        string key = Hex.ConcatGetOXString(StorageKeys.System, StorageKeys.Events);
        
        // if hash not found, it will throw, not return null

        string? result = await GetStateStorageAsync<string>(key, blockHash);

        if (result == null)
        {
            return Array.Empty<EventRecord>();
        }

        using var reader = new ScaleStreamReader(result);

        return reader.ReadList(
            s => EventRecord.Parse(s, meta));
    }

    public async Task<ChainState> GetChainStateAsync()
    {
        var metadataTask = GetStateMetadataAsync();
        var genesisTask = GetChainBlockHashAsync(0);
        var finalizedBlockHashTask = GetChainFinalizedHeadAsync();

        await Task.WhenAll(
            metadataTask,
            genesisTask,
            finalizedBlockHashTask);

        var runtimeVersionTask = GetRuntimeVersionAsync(finalizedBlockHashTask.Result);
        var finalizedHeaderTask = GetChainHeaderAsync(finalizedBlockHashTask.Result);

        await Task.WhenAll(runtimeVersionTask, finalizedHeaderTask);

        return new()
        {
            Metadata = metadataTask.Result,
            GenesisHash = genesisTask.Result,
            Version = runtimeVersionTask.Result,
            LatestFinalizedHeader = finalizedHeaderTask.Result!
        };
    }
}