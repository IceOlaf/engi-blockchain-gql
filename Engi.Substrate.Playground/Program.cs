namespace Engi.Substrate.Playground;

public static class Program
{
    public static async Task Main()
    {
        var http = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9933")
        };

        var substrateClient = new SubstrateClient(http);

        string accountId = "5GrwvaEF5zXb26Fz9rcQpDWS57CtERHpNehXCPcNoHGKutQY";

        var account = await substrateClient.GetSystemAccountAsync(accountId);

        //string hex = await substrateClient.RpcAsync<string>("state_getMetadata");

        //var metadata = RuntimeMetadata.Parse(new ScaleStream(Convert.FromHexString(hex.Substring(2))));

        return;
    }
}