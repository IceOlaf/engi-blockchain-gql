namespace Engi.Substrate.Server;

public class AwsOptions
{
    /// <summary>
    /// Service URL override for AWS config, so that localstack can be used locally.
    /// Only allowed in Development environment, for safety reasons.
    /// </summary>
    public string? ServiceUrl { get; set; }

    /// <summary>
    /// Bucket name for website storage.
    /// </summary>
    public string BucketName { get; set; } = null!;
}
