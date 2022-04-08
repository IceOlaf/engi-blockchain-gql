using System.Net.Http.Json;
using System.Text.Json;

namespace Engi.Substrate;

class SubstrateClient
{
    private static long IdCounter = 0;

    private readonly HttpClient http;

    public SubstrateClient(HttpClient http)
    {
        this.http = http;
    }

    private async Task RpcAsync(string method)
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

        if (result.ValueKind == JsonValueKind.String)
        {
            string s = result.GetString()!;


        }
    }
}