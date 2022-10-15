using Engi.Substrate.Identity;
using Raven.Client.Documents.Operations;

namespace Engi.Substrate.Server.Github;

internal class UpdateGithubEnrollmentPatchRequest : PatchRequest
{
    public UpdateGithubEnrollmentPatchRequest(UserGithubEnrollment enrollment)
    {
        Script = @"
    this.GithubEnrollments = this.GithubEnrollments || {}
    this.GithubEnrollments[args.installationId] = args.enrollment
";

        Values = new()
        {
            ["installationId"] = enrollment.InstallationId,
            ["enrollment"] = enrollment
        };
    }
}