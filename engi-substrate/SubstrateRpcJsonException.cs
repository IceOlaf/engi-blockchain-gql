using System.Text.Json;

namespace Engi.Substrate;

public class SubstrateRpcJsonException : Exception
{
    public string Method { get; }

    public Type ExpectedType { get; }

    public string Content { get; }

    public SubstrateRpcJsonException(string method, Type expectedType, JsonElement json, Exception ex)
        : base("Failed to deserialize RPC response.", ex)
    {
        Method = method;
        ExpectedType = expectedType;
        Content = json.ToString();
    }
}
