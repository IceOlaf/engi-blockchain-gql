using System.Net.Http.Json;
using System.Text.Json;

namespace Engi.Substrate;

public class SubstrateClient
{
    private static long IdCounter = 0;

    private readonly HttpClient http;

    public SubstrateClient(HttpClient http)
    {
        this.http = http;
    }

    public async Task<JsonElement> RpcAsync(string method)
    {
        long id = Interlocked.Increment(ref IdCounter);

        var payload = new
        {
            id,
            jsonrpc = "2.0",
            method
        };

        var response = await http.PostAsJsonAsync(string.Empty, payload);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();

        var result = json.GetProperty("result");

        return result;
    }

    public async Task<TResult> RpcAsync<TResult>(string method)
    {
        var result = await RpcAsync(method);

        if (typeof(TResult) == typeof(string))
        {
            return (TResult) (object) result.GetString()!;
        }

        throw new NotImplementedException(typeof(TResult).Name);
    }
}