using System.Security.Cryptography.X509Certificates;
using Engi.Substrate.Server.Indexing;
using Raven.Client.Documents;
using Raven.Client.Documents.Indexes;

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

        conventions.Serialization = new EngiSerializationConventions();

        store.Initialize();

        IndexCreation.CreateIndexes(typeof(IndexingBackgroundService).Assembly, store);

        services.AddSingleton<IDocumentStore>(store);

        services.AddScoped(_ =>
        {
            var asyncSession = store.OpenAsyncSession();
            asyncSession.Advanced.UseOptimisticConcurrency = true;
            return asyncSession;
        });

        return services;
    }
}