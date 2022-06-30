using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Server;

public interface IChainObserver
{
    JsonRpcRequest[] CreateRequests();

    Task ObserveAsync(JsonRpcRequest request, JsonRpcResponse response);
}