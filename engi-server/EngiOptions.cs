using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace Engi.Substrate.Server;

public class EngiOptions
{
    /// <summary>
    /// Assume a role when using AWS services.
    /// </summary>
    public string? AssumeRoleArn { get; set; }

    /// <summary>
    /// The encryption certificate to use to store keys.
    /// </summary>
    [Required]
    public string EncryptionCertificate { get; set; } = null!;

    /// <summary>
    /// The allowed skew for signature validation.
    /// </summary>
    public TimeSpan SignatureSkew { get; set; }

    /// <summary>
    ///  Used to disable the chain observer during testing.
    /// </summary>
    public bool DisableChainObserver { get; set; }

    /// <summary>
    ///  Used to disable integration with Engine during testing.
    /// </summary>
    public bool DisableEngineIntegration { get; set; }

    /// <summary>
    /// The API key for sudo calls.
    /// </summary>
    [Required, StringLength(128, MinimumLength = 8)]
    public string SudoApiKey { get; set; } = null!;

    /// <summary>
    /// The ENGI Github app id.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int GithubAppId { get; set; }

    /// <summary>
    /// The private key generated for signing JWT tokens to authenticate
    /// with Github for the ENGI app. PEM, base64 encoded.
    /// </summary>
    [Required]
    public string GithubAppPrivateKey { get; set; } = null!;

    /// <summary>
    /// The webhook secret for the ENGI app webhook deliveries.
    /// </summary>
    [Required]
    public string GithubAppWebhookSecret { get; set; } = null!;

    /// <summary>
    /// ARN of the topic that Engine requests will be posted to.
    /// </summary>
    [Required]
    public string EngineInputTopicArn { get; set; } = null!;

    /// <summary>
    /// URL of the queue that Engine responses will be received from.
    /// </summary>
    [Required]
    public string EngineOutputQueueUrl { get; set; } = null!;

    /// <summary>
    /// The SNS topic name that the engine will post responses to
    /// before they are queued to <see cref="EngineOutputQueueUrl"/>.
    /// </summary>
    [Required]
    public string EngineOutputTopicName { get; set; } = null!;

    /// <summary>
    /// A mnemonic that can be used to invoke sudo commands on the chain
    /// like solve_job.
    /// </summary>
    [Required]
    public string SudoChainMnemonic { get; set; } = null!;

    public X509Certificate2 EncryptionCertificateAsX509 =>
        X509CertificatesHelper.CertificateFromBase64String(EncryptionCertificate);
}
