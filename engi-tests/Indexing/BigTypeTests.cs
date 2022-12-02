using System;
using System.Threading.Tasks;
using Engi.Substrate.Jobs;
using Raven.Client.Documents;
using Xunit;

namespace Engi.Substrate.Indexing;

public class BigTypeTests : EngiRavenTestDriver
{
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

        WaitForUserToContinueTheTest(store);

        using (var session = store.OpenAsyncSession())
        {
            string threshold = 15.ToString(StorageFormats.UInt128);

            var result = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .WhereLessThanOrEqual(x => x.Funding, threshold)
                .SingleAsync();

            Assert.Equal(1UL, result.JobId);
        }
    }
}