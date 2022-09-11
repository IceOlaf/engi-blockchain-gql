namespace Engi.Substrate.Server;

public class ApiOptions
{
    public string Url { get; set; } = null!;

    public string Domain
    {
        get
        {
            if (Url == null)
            {
                throw new ArgumentNullException(nameof(Url));
            }

            return new Uri(Url).Host;
        }
    }
}