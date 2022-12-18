using System.Net;

namespace Engi.Substrate;

public static class ExceptionUtils
{
    public static bool IsTransient(Exception ex)
    {
        if (ex is TimeoutException or TaskCanceledException { InnerException: TimeoutException })
        {
            return true;
        }

        if (ex is HttpRequestException { StatusCode: HttpStatusCode.BadGateway or HttpStatusCode.GatewayTimeout })
        {
            return true;
        }

        return false;
    }
}
