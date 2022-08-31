using System;
using System.Threading.Tasks;
using Engi.Substrate.Jobs;
using Raven.Client.Documents;
using Raven.TestDriver;
using Xunit;

namespace Engi.Substrate.Indexing;

public class BigTypeTests : RavenTestDriver
{
    protected override void PreInitialize(IDocumentStore store)
    {
        store.Conventions.Serialization = new EngiSerializationConventions();
    }

    protected override void SetupDatabase(IDocumentStore store)
    {
        store.ExecuteIndex(new JobIndex());
    }

    [Fact]
    public async Task Query_Funding_LessThanOrEqualTo()
    {
        using var store = GetDocumentStore();

        using (var session = store.OpenAsyncSession())
        {
            await session.StoreAsync(new JobSnapshot
            {
                JobId = 1,
                Funding = 10,
                IsCreation = true,
                SnapshotOn = new BlockReference
                {
                    DateTime = DateTime.UtcNow
                }
            });

            await session.StoreAsync(new JobSnapshot
            {
                JobId = 2,
                Funding = 20,
                IsCreation = true,
                SnapshotOn = new BlockReference
                {
                    DateTime = DateTime.UtcNow
                }
            });

            await session.SaveChangesAsync();
        }

        WaitForIndexing(store);

        using (var session = store.OpenAsyncSession())
        {
            string threshold = 15.ToString("D40");

            var result = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .WhereLessThanOrEqual(x => x.Funding, threshold)
                .SingleAsync();

            Assert.Equal(1UL, result.JobId);
        }
    }
}