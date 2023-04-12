using Raven.Client.Documents.Queries;
using Raven.Client.Documents.Session;

namespace Engi.Substrate.Jobs;

public static class JobIndexQueryExtensions
{
    public static IAsyncDocumentQuery<JobIndex.Result> Search(
        this IAsyncDocumentQuery<JobIndex.Result> query,
        JobsQueryArguments? args,
        out QueryStatistics stats)
    {
        args ??= new();

        if (args.Creator != null)
        {
            query = query
                .WhereIn(x => x.Creator, args.Creator);
        }

        if (args.CreatedAfter.HasValue)
        {
            query = query
                .WhereGreaterThanOrEqual(x => x.CreatedOn_DateTime, args.CreatedAfter.Value);
        }

        if (args.Status.HasValue)
        {
            query = query
                .WhereEquals(x => x.Status, args.Status.Value);
        }

        if (!string.IsNullOrWhiteSpace(args.Search))
        {
            query = query
                .Search(x => x.Query, $"{args.Search}*", @operator: SearchOperator.And);
        }

        if (args.Technologies != null)
        {
            query = query
                .ContainsAny(x => x.Technologies, args.Technologies);
        }

        if (args.MinFunding != null && args.MaxFunding != null)
        {
            query = query
                .WhereBetween(x => x.Funding,
                    args.MinFunding.Value.ToString(StorageFormats.UInt128), args.MaxFunding.Value.ToString(StorageFormats.UInt128));
        }
        else if (args.MinFunding != null)
        {
            query = query
                .WhereGreaterThanOrEqual(x => x.Funding, args.MinFunding.Value.ToString(StorageFormats.UInt128));
        }
        else if (args.MaxFunding != null)
        {
            query = query
                .WhereLessThanOrEqual(x => x.Funding, args.MaxFunding.Value.ToString(StorageFormats.UInt128));
        }

        if (args.SolvedBy != null)
        {
            query = query
                .ContainsAny(x => x.SolvedBy, args.SolvedBy);
        }

        if (args.CreatedOrSolvedBy != null)
        {
            query = query
                .OpenSubclause()
                .WhereEquals(x => x.Creator, args.CreatedOrSolvedBy)
                .OrElse()
                .ContainsAny(x => x.SolvedBy, new[] { args.CreatedOrSolvedBy })
                .CloseSubclause();
        }

        if (args.RepositoryFullName != null)
        {
            query = query
                .WhereIn(x => x.Repository_FullName, args.RepositoryFullName);
        }

        if (args.RepositoryOrganization != null)
        {
            query = query
                .WhereIn(x => x.Repository_Organization, args.RepositoryOrganization);
        }

        switch (args.OrderByProperty)
        {
            case JobsOrderByProperty.CreatedOn:
                query = args.OrderByDirection == OrderByDirection.Asc
                    ? query.OrderBy(x => x.CreatedOn.DateTime)
                    : query.OrderByDescending(x => x.CreatedOn.DateTime);
                break;

            case JobsOrderByProperty.UpdatedOn:
                query = args.OrderByDirection == OrderByDirection.Asc
                    ? query.OrderBy(x => x.UpdatedOn.DateTime)
                    : query.OrderByDescending(x => x.UpdatedOn.DateTime);
                break;

            case JobsOrderByProperty.Funding:
                query = args.OrderByDirection == OrderByDirection.Asc
                    ? query.OrderBy(x => x.Funding)
                    : query.OrderByDescending(x => x.Funding);
                break;
        }

        return query
            .Statistics(out stats)
            .Skip(args.Skip)
            .Take(args.Limit);
    }

    public static IAsyncDocumentQuery<JobIndex.Result> Search(
        this IAsyncDocumentQuery<JobIndex.Result> query,
        JobsQueryArguments? args)
    {
        return Search(query, args, out _);
    }
}
