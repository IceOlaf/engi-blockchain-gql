using Sentry;

namespace Engi.Substrate.Server;

public static class SentryExtensions
{
    public static SentryId CaptureException(
        this IHub sentry,
        Exception ex,
        Dictionary<string, object?> extras)
    {
        return sentry.CaptureException(ex, scope => scope.SetExtras(extras));
    }
}