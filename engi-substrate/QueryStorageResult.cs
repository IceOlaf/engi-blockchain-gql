using System.Collections.ObjectModel;

namespace Engi.Substrate;

public class QueryStorageResult : ReadOnlyDictionary<string, string?>
{
    public string BlockHash { get; init; }

    public QueryStorageResult(string blockHash, IDictionary<string, string?> dict)
        : base(dict)
    {
        BlockHash = blockHash;
    }

    public QueryStorageResult<T> Transform<T>(Func<ScaleStreamReader, T> func)
    {
        return new(BlockHash, this.ToDictionary(x => x.Key, x =>
        {
            if (x.Value == null)
            {
                return default;
            }

            using var reader = new ScaleStreamReader(x.Value);

            return func(reader);
        }));
    }
}

public class QueryStorageResult<T> : ReadOnlyDictionary<string, T?>
{
    public string BlockHash { get; init; }

    public QueryStorageResult(string blockHash, IDictionary<string, T?> dict)
        : base(dict)
    {
        BlockHash = blockHash;
    }
}