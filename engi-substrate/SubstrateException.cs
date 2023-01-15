using System.Text.Json;

namespace Engi.Substrate;

public class SubstrateException : Exception
{
    public int? Code { get; }

    public SubstrateException(int? code, string message, JsonElement? data)
        : base($"Substrate error code={code?.ToString() ?? "unknown"}; {message}; data={data}")
    {
        Code = code;
    }
}
