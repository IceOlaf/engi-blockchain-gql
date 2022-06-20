using System.Text.Json;
using Engi.Substrate.Metadata.V14;
using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public class RuntimeSubscriptionRegistration : SubscriptionRegistration
{
    public RuntimeSubscriptionRegistration()
        : base("state_subscribeRuntimeVersion")
    { }

    public RuntimeVersion Version { get; private set; } = null!;

    public override Task PublishAsync(JsonRpcResponse response)
    {
        Version = response.Parameters.Result.Deserialize<RuntimeVersion>(
            SubstrateJsonSerializerOptions.Default)!;

        return Task.CompletedTask;
    }
}