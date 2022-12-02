using System.Text;
using GitHubJwt;

namespace Engi.Substrate.Server.Github;

public class Base64PrivateKeySource : IPrivateKeySource
{
    private readonly string key;

    public Base64PrivateKeySource(string key)
    {
        this.key = key ?? throw new ArgumentNullException(nameof(key));
    }

    public TextReader GetPrivateKeyReader()
    {
        byte[] data = Convert.FromBase64String(key);

        return new StringReader(Encoding.UTF8.GetString(data));
    }
}