using System.Text.Json;

namespace Engi.Substrate;

public class SubstrateException : Exception
{
    public string Code { get; }

    public SubstrateException(string code, string message, JsonElement? data)
        : base($"Substrate error code={code}; {message}; data={data}")
    {
        Code = code;
    }
}
