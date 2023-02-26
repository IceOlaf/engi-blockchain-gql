using System.Security.Cryptography.X509Certificates;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.Server.Indexing;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Indexes;
using Raven.Client.Documents.Operations.Expiration;
using Raven.Client.Documents.Operations.Refresh;
using Raven.Client.ServerWide;
using Raven.Client.ServerWide.Operations;
using Raven.Migrations;

namespace Engi.Substrate.Server;

public static class RavenConfigurationExtensions
{
    public static IServiceCollection AddRaven(
        this IServiceCollection services,
        IConfigurationSection section,
        Action<DocumentConventions> customizeConventions)
    {
        var options = section.Get<RavenConnectionOptions>();

        var store = new DocumentStore
        {
            Urls = options.GetParsedUrls(),
            Database = options.Database,
            Certificate = string.IsNullOrEmpty(options.Certificate) 
                ? null : new X509Certificate2(Convert.FromBase64String(options.Certificate))
        };

        // customize conventions

        customizeConventions(store.Conventions);

        store.Initialize();

        try 
        {
            store.Maintenance.Server.Send(
                new CreateDatabaseOperation(
                    new DatabaseRecord(options.Database)));
        }
        catch(Exception)
        {
            // this may fail in real environments, useful for local/CI
        }

        try
        {
            store.Maintenance.Send(new ConfigureRefreshOperation(new()
            {
                Disabled = false,
                RefreshFrequencyInSec = 60
            }));

            store.Maintenance.Send(new ConfigureExpirationOperation(new()
            {
                Disabled = false,
                DeleteFrequencyInSec = 60
            }));
        }
        catch (Exception)
        {
            // this may fail in real environments, useful for local/CI
        }

        IndexCreation.CreateIndexes(typeof(RuntimeMetadata).Assembly, store);
        IndexCreation.CreateIndexes(typeof(IndexingBackgroundService).Assembly, store);

        services.AddSingleton<IDocumentStore>(store);

        services.AddScoped(_ =>
        {
            var asyncSession = store.OpenAsyncSession();
            asyncSession.Advanced.UseOptimisticConcurrency = true;
            return asyncSession;
        });

        services.AddRavenDbMigrations();
        services.AddHostedService<RavenMigrationService>();

        return services;
    }
}
