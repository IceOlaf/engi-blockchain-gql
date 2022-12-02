using System;
using System.Threading.Tasks;
using Engi.Substrate.Jobs;
using Raven.Client.Documents;
using Xunit;

namespace Engi.Substrate.Indexing;

public class JobIndexQueryTests : EngiRavenTestDriver
{
    private const string Address1 = "5EUJ3p7ds1436scqdA2n6ph9xVs6chshRP1ADjgK1Qj3Hqs2";
    private const string Address2 = "5EyiGwuYPgGiEfwpPwXyH5TwXXEUFz6ZgPhzYik2fMCcbqMC";
    protected override void SetupDatabase(IDocumentStore documentStore)
    {
        documentStore.ExecuteIndex(new JobIndex());
    }

    [Fact]
    public async Task FilterBy_Solver()
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

            await session.StoreAsync(new SolutionSnapshot
            {
                SolutionId = 100,
                JobId = 1,
                Author = Address1,
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
            var resultAddress1 = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .FilterBy(new JobsQueryArguments
                {
                    SolvedBy = new[] { Address1 }
                })
                .CountAsync();

            var resultAddress2 = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .FilterBy(new JobsQueryArguments
                {
                    SolvedBy = new[] { Address2 }
                })
                .CountAsync();

            Assert.Equal(1, resultAddress1);
            Assert.Equal(0, resultAddress2);
        }
    }

    [Fact]
    public async Task FilterBy_CanIncludeSolutions()
    {
        string solutionKey = SolutionSnapshot.KeyFrom(100, 50);

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

            await session.StoreAsync(new SolutionSnapshot
            {
                Id = solutionKey,
                SolutionId = 100,
                JobId = 1,
                Author = Address1,
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
            var result = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .Include(x => x.SolutionIds)
                .FirstOrDefaultAsync();

            Assert.True(session.Advanced.IsLoaded(solutionKey));
        }
    }

    [Fact]
    public async Task FilterBy_ReturnsSolutionUserCount_Distinct()
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

            await session.StoreAsync(new SolutionSnapshot
            {
                SolutionId = 100,
                JobId = 1,
                Author = Address1,
                SnapshotOn = new BlockReference
                {
                    DateTime = DateTime.UtcNow
                }
            });

            await session.StoreAsync(new SolutionSnapshot
            {
                SolutionId = 200,
                JobId = 1,
                Author = Address2,
                SnapshotOn = new BlockReference
                {
                    DateTime = DateTime.UtcNow
                }
            });

            await session.StoreAsync(new SolutionSnapshot
            {
                SolutionId = 300,
                JobId = 1,
                Author = Address2,
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
            var result = await session.Advanced
                .AsyncDocumentQuery<JobIndex.Result, JobIndex>()
                .FirstAsync();

            Assert.Equal(2, result.SolutionUserCount);
        }
    }
}