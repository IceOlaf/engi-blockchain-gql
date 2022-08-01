using System.Security.Cryptography.X509Certificates;
using Engi.Substrate.Server.Indexing;
using Newtonsoft.Json;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;

namespace Engi.Substrate.Server;

public static class RavenConfigurationExtensions
{
    public static IServiceCollection AddRaven(this IServiceCollection services, IConfigurationSection section)
    {
        var options = section.Get<RavenConnectionOptions>();

        var store = new DocumentStore
        {
            Urls = options.Urls,
            Database = options.Database,
            Certificate = string.IsNullOrEmpty(options.Certificate) 
                ? null : new X509Certificate2(Convert.FromBase64String(options.Certificate))
        };

        // customize conventions

        var conventions = store.Conventions;

        conventions.ThrowIfQueryPageSizeIsNotSet = true;

        conventions.Serialization = new NewtonsoftJsonSerializationConventions
        {
            CustomizeJsonSerializer = CustomizeSerializer,
            CustomizeJsonDeserializer = CustomizeSerializer
        };

        store.Initialize();

        services.AddSingleton<IDocumentStore>(store);

        services.AddScoped(_ =>
        {
            var asyncSession = store.OpenAsyncSession();
            asyncSession.Advanced.UseOptimisticConcurrency = true;
            return asyncSession;
        });

        return services;
    }

    private static void CustomizeSerializer(JsonSerializer serializer)
    {
        serializer.Converters.Add(new BigIntegerJsonConverter());
        serializer.Converters.Add(new InlineByteArrayJsonConvert());
    }
}