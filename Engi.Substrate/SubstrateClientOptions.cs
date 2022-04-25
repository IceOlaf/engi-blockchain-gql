namespace Engi.Substrate;

public class SubstrateClientOptions
{
    public string? Url { get; set; }

    public Uri HttpsUri => new UriBuilder(Url!) { Scheme = "https" }.Uri;
    public Uri WssUri => new UriBuilder(Url!) { Scheme = "wss" }.Uri;
}