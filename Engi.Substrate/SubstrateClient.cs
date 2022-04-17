using System.Net.Http.Json;
using System.Text.Json;
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

    public async Task<JsonElement> RpcAsync(string method, params string[] @params)
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

        var result = json.GetProperty("result");

        return result;
    }

    public async Task<TResult> RpcAsync<TResult>(string method, params string[] @params)
    {
        var result = await RpcAsync(method, @params);

        if (typeof(TResult) == typeof(string))
        {
            return (TResult) (object) result.GetString()!;
        }

        throw new NotImplementedException(typeof(TResult).Name);
    }

    public Task<string> GetSystemChainAsync() => RpcAsync<string>("system_chain");
    public Task<string> GetSystemNameAsync() => RpcAsync<string>("system_name");
    public Task<string> GetSystemVersionAsync() => RpcAsync<string>("system_version");

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

        var scale = new ScaleStream(result);

        return AccountInfo.Parse(scale);
    }

    private static readonly byte[] xxAccount = Hashing.Twox128("Account");
    private static readonly byte[] xxSystem = Hashing.Twox128("System");
}