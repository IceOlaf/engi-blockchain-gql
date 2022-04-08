namespace Engi.Substrate;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var options = new SubstrateClientOptions
        {
            Url = "http://localhost:9933"
        };

        var http = new HttpClient
        {
            BaseAddress = new Uri(options.Url)
        };

        var client = new SubstrateClient(http);
    }
}

