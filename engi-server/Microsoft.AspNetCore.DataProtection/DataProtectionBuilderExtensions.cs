using System.Collections.ObjectModel;
using System.Globalization;
using System.Xml.Linq;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.DataProtection.Repositories;
using Microsoft.Extensions.Options;
using Raven.Client;
using Raven.Client.Documents;

namespace Microsoft.AspNetCore.DataProtection;

public static class DataProtectionBuilderExtensions
{
    public static IDataProtectionBuilder PersistKeysToRaven(this IDataProtectionBuilder builder)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        builder.Services.AddSingleton<IConfigureOptions<KeyManagementOptions>>(services =>
        {
            var store = services.GetRequiredService<IDocumentStore>();

            return new ConfigureOptions<KeyManagementOptions>(options =>
            {
                options.XmlRepository = new RavenXmlRepository(store);
            });
        });

        return builder;
    }

    class RavenXmlRepository : IXmlRepository
    {
        private readonly IDocumentStore store;

        public RavenXmlRepository(IDocumentStore store)
        {
            this.store = store;
        }

        public IReadOnlyCollection<XElement> GetAllElements()
        {
            using var session = store.OpenSession();

            var docs = session.Advanced
                .LoadStartingWith<XmlDocument>(CollectionName + "/",
                    pageSize: int.MaxValue);

            return new ReadOnlyCollection<XElement>(
                docs.Select(x => XElement.Parse(x.SerializedXml)).ToArray());
        }

        public void StoreElement(XElement element, string friendlyName)
        {
            string? id = element.Attribute("id")?.Value;

            if (id == null
                || !DateTime.TryParse(element.Element("expirationDate")?.Value, null, DateTimeStyles.AssumeUniversal, out var expirationDate))
            {
                throw new NotSupportedException($"Not supported Xml: {element}");
            }

            using var session = store.OpenSession();

            var document = new XmlDocument
            {
                Id = $"{CollectionName}/{id}",
                SerializedXml = element.ToString()
            };

            session.Store(document);

            var metadata = session.Advanced.GetMetadataFor(document);

            metadata[Constants.Documents.Metadata.Expires] = expirationDate;

            session.SaveChanges();
        }

        private const string CollectionName = "XmlDocuments";

        class XmlDocument
        {
            public string Id { get; init; } = null!;

            public string SerializedXml { get; init; } = null!;
        }
    }
}