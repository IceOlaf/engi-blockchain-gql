namespace Engi.Substrate.Server.Indexing;

public class IndexingException : Exception
{
    public string Hash { get; }

    public IndexingException(string hash, string message)
        : base(message)
    {
        Hash = hash;
    }
}
