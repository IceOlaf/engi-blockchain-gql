using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public abstract class SubscriptionRegistration
{
    public string Method { get; init; } = null!;

    public string[]? Params { get; init; }

    public string CurrentId { get; set; } = null!;

    protected SubscriptionRegistration() { }

    protected SubscriptionRegistration(string method, string[]? @params = null)
    {
        Method = method;
        Params = @params;
    }

    public object GetPayload()
    {
        return new
        {
            method = Method,
            @params = Params
        };
    }

    public abstract Task PublishAsync(JsonRpcResponse response);
}