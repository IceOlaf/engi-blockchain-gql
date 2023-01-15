using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Pallets;
using Sentry;

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

            int? code = null;
            string? message = null;

            try
            {
                var prop = error.GetProperty("code");

                code = prop.ValueKind switch
                {
                    JsonValueKind.Number => prop.GetInt32(),
                    JsonValueKind.String => int.Parse(prop.GetString()!),
                    _ => throw new NotImplementedException()
                };
            }
            catch
            {
                // ignored
            }
           
            try
            {
                message = error.GetProperty("message").GetString()!;
            }
            catch
            {
                // ignored
            }

            if (code == -32000 && message != null)
            {
                string? hash = null;

                try
                {
                    hash = Regex.Match(message, @"0x([a-z\d]{64})")
                        .Captures[0].Value;
                }
                catch (Exception)
                {
                    // ignore

                    SentrySdk.CaptureMessage("Failed to extract hash from block not found error",
                        scope =>
                        {
                            scope.SetExtra(nameof(code), code);
                            scope.SetExtra(nameof(message), message);
                        });
                }

                throw new BlockHeaderNotFoundException(hash ?? "unknown", code.Value, message, data);
            }

            throw new InvalidOperationException(
                $"Substrate error code={code?.ToString() ?? "unknown"}; message={message ?? "unknown"}: {data}")
            {
                Data =
                {
                    ["json"] = json
                }
            };
        }

        try
        {
            T? result = json.GetProperty("result").Deserialize<T>(SubstrateJsonSerializerOptions.Default);

            return result;
        }
        catch (Exception ex)
        {
            throw new SubstrateRpcJsonException(method, typeof(T), json, ex);
        }
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

    public Task<string?> GetStateStorageAsync(string key, string? blockHash = null)
    {
        var @params = new[] { key };

        if (blockHash != null)
        {
            Array.Resize(ref @params, 2);

            @params[1] = blockHash;
        }

        return RpcAsync<string>(ChainKeys.StateGetStorage, @params);
    }

    public async Task<T?> GetStateStorageAsync<T>(string key, Func<ScaleStreamReader, T> parse, string? blockHash = null)
    {
        string? result = await GetStateStorageAsync(key, blockHash);

        if (result == null)
        {
            return default;
        }

        using var reader = new ScaleStreamReader(result);

        return parse(reader);
    }

    public async Task<QueryStorageResult> QueryStorageAtAsync(string[] keys, string? blockHash = null)
    {
        var @params = new object[] { keys };

        if (blockHash != null)
        {
            Array.Resize(ref @params, 2);

            @params[1] = blockHash;
        }

        var json = await RpcAsync<JsonArray>(ChainKeys.StateQueryStorageAt, @params);

        if (json?.Count > 1)
        {
            throw new NotImplementedException($"Expected a single result from {ChainKeys.StateQueryStorageAt}");
        }

        var firstItem = json![0]!;

        string resultBlockHash = firstItem["block"]!.GetValue<string>();

        var changes = firstItem["changes"]!.AsArray()
            .ToDictionary(change => (string)change![0]!, change => (string?)change![1]);

        return new(resultBlockHash, changes);
    }

    public async Task<QueryStorageResult<T>> QueryStorageAtAsync<T>(string[] keys, Func<ScaleStreamReader, T> transform, string? blockHash = null)
    {
        var result = await QueryStorageAtAsync(keys, blockHash);

        return result.Transform(transform);
    }

    // chain_

    public async Task<SignedBlock> GetChainBlockAsync(string hash)
    {
        var block = await RpcAsync<SignedBlock>(ChainKeys.ChainGetBlock, hash);

        if (block == null)
        {
            throw new BlockHeaderNotFoundException(hash, null, $"Block with hash {hash} was not found.", null);
        }

        return block;
    }

    public Task<SignedBlock> GetChainBlockAsync() => RpcAsync<SignedBlock>(ChainKeys.ChainGetBlock)!;
    public Task<string> GetChainBlockHashAsync(ulong number) => RpcAsync(ChainKeys.ChainGetBlockHash, number);
    public Task<string> GetChainFinalizedHeadAsync() => RpcAsync(ChainKeys.ChainGetFinalizedHead);
    public Task<Header> GetChainLatestHeaderAsync() => RpcAsync<Header>(ChainKeys.ChainGetHeader)!;
    public Task<Header?> GetChainHeaderAsync(string hash) => RpcAsync<Header>(ChainKeys.ChainGetHeader, hash);

    // author_

    public Task<string> AuthorSubmitExtrinsicAsync<TExtrinsic>(SignedExtrinsicArguments<TExtrinsic> args, RuntimeMetadata meta)
        where TExtrinsic : IExtrinsic
    {
        var payload = Hex.GetString0X(args.Serialize(meta));

        return RpcAsync(ChainKeys.AuthorSubmitExtrinsic, payload);
    }

    // composite

    public async Task<AccountInfo> GetSystemAccountAsync(Address address)
    {
        if (address == null)
        {
            throw new ArgumentNullException(nameof(address));
        }

        string addressHex = StorageKeys.System.Account(address);

        string? result = await GetStateStorageAsync(addressHex);

        if (result == null)
        {
            throw new KeyNotFoundException(addressHex);
        }

        var scale = new ScaleStreamReader(result);

        return AccountInfo.Parse(scale);
    }

    public async Task<EventRecord[]> GetSystemEventsAsync(string blockHash, RuntimeMetadata meta)
    {
        // if hash not found, it will throw, not return null

        string? result = await GetStateStorageAsync(StorageKeys.System.Events, blockHash);

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
