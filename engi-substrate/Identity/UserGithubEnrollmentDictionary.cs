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

    public bool ContainsRepositoryWithFullName(string fullName)
    {
        return Values
            .SelectMany(x => x.Repositories)
            .Any(x => string.Equals(x.FullName, fullName, StringComparison.OrdinalIgnoreCase));
    }
}