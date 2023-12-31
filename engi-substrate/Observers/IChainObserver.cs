using Engi.Substrate.WebSockets;

namespace Engi.Substrate.Observers;

public interface IChainObserver
{
    JsonRpcRequest[] CreateRequests();

    Task ObserveAsync(JsonRpcRequest request, JsonRpcResponse response);
}
