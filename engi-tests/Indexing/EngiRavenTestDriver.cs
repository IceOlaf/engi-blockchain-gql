using Raven.Client.Documents;
using Raven.TestDriver;

namespace Engi.Substrate.Indexing;

public class EngiRavenTestDriver : RavenTestDriver
{
    protected override void PreInitialize(IDocumentStore store)
    {
        store.Conventions.Serialization = new EngiSerializationConventions();
    }
}