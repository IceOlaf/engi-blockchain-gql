namespace Engi.Substrate.WebSockets;

public class RpcException : Exception
{
    public RpcException(JsonRpcError error)
    : base($"RPC error {error.Code}: {error.Message}")
    {
        
    }
}