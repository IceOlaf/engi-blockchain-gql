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

    public async Task<T> RpcAsync<T>(string method, params string[] @params)
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

        T? result = json.GetProperty("result").Deserialize<T>(new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DictionaryKeyPolicy = JsonNamingPolicy.CamelCase,
            Converters = { new JsonStringEnumConverter() }
        });

        return result!;
    }

    public async Task<string> RpcAsync(string method, params string[] @params)
    {
        var json = await RpcAsync<JsonElement>(method, @params);

        return json.GetString()!;
    }

    public Task<string> GetSystemChainAsync() => RpcAsync<string>("system_chain");
    public Task<string> GetSystemNameAsync() => RpcAsync<string>("system_name");
    public Task<string> GetSystemVersionAsync() => RpcAsync<string>("system_version");
    public Task<SystemHealth> GetSystemHealthAsync() => RpcAsync<SystemHealth>("system_health");

    public async Task<RuntimeMetadata> GetRuntimeMetadataAsync()
    {
        string result = await RpcAsync("state_getMetadata");

        using var scale = new ScaleStreamReader(result);

        return RuntimeMetadata.Parse(scale);
    }

    public Task<T> GetStateStorageAsync<T>(params string[] @params) => RpcAsync<T>("state_getStorage", @params);

    public async Task<AccountInfo> GetSystemAccountAsync(string? accountId)
    {
        if (string.IsNullOrEmpty(accountId))
        {
            throw new ArgumentNullException(nameof(accountId));
        }

        byte[] accountIdBytes = Address.Decode(accountId);

        string addressHex = Hex.ConcatGetOxString(
            xxSystem,
            xxAccount,
            Hashing.Blake2Concat(accountIdBytes)
        );

        string result = await GetStateStorageAsync<string>(addressHex);

        var scale = new ScaleStreamReader(result);

        return AccountInfo.Parse(scale);
    }

    public Task<Header> GetHeaderAsync(string hash) => RpcAsync<Header>("chain_getHeader", hash);

    private static readonly byte[] xxAccount = Hashing.Twox128("Account");
    private static readonly byte[] xxSystem = Hashing.Twox128("System");
}