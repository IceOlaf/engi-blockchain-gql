using Engi.Substrate.Github;

namespace Engi.Substrate.Identity;

public class UserGithubEnrollmentDictionary : Dictionary<long, UserGithubEnrollment>
{
    public (UserGithubEnrollment? enrollment, GithubRepository? repo) Find(string owner, string name)
    {
        foreach (var enrollment in Values)
        {
            var repo = enrollment.Repositories
                .FirstOrDefault(repo => repo.Equals(owner, name));

            if (repo != null)
            {
                return (enrollment, repo);
            }
        }

        return (null, null);
    }
}