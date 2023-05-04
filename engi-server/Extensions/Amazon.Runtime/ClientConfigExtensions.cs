using Engi.Substrate.Server;

namespace Amazon.Runtime;

public static class ClientConfigExtensions
{
    public static TConfig Apply<TConfig>(this TConfig config, AwsOptions options)
        where TConfig : ClientConfig
    {
        if (!string.IsNullOrEmpty(options.ServiceUrl))
        {
            config.ServiceURL = options.ServiceUrl;
        }

        return config;
    }
}
