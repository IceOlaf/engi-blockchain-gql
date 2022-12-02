namespace Engi.Substrate.Server;

public class ApplicationOptions
{
    /// <summary>
    /// The public hostname of the API.
    /// </summary>
    public string ApiUrl { get; set; } = null!;

    public string ApiDomain
    {
        get
        {
            if (ApiUrl == null)
            {
                throw new ArgumentNullException(nameof(ApiUrl));
            }

            return new Uri(ApiUrl).Host;
        }
    }

    /// <summary>
    /// The main website URL, used to compose links.
    /// </summary>
    public string Url { get; set; } = null!;
}