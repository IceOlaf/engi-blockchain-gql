using Xunit;

namespace Engi.Substrate.Indexing;

public class RepositoryUrlParsingTests
{
    [Theory]
    [InlineData("https://github.com/ravendb/ravendb", "ravendb", "ravendb")]
    [InlineData("http://github.com/ravendb/ravendb", "ravendb", "ravendb")]
    [InlineData("https://github.com/ravendb/ravendb.git", "ravendb", "ravendb")]
    [InlineData("https://www.github.com/ravendb/ravendb", "ravendb", "ravendb")]
    [InlineData("git://github.com/ravendb/ravendb.git", "ravendb", "ravendb")]
    [InlineData("git+ssh://georgiosd@github.com/ravendb/ravendb.git", "ravendb", "ravendb")]
    [InlineData("https://gitlab.com/ravendb/ravendb", "ravendb", "ravendb")]
    public void ParsesSupportedUrl(string url, string expectedOrganization, string expectedName)
    {
        var (org, name) = RepositoryUrl.Parse(url);

        Assert.Equal(expectedOrganization, org);
        Assert.Equal(expectedName, name);
    }

    [Theory]
    [InlineData("https://github.com/ravendb")]
    [InlineData("https://bitbucket.org/ravendb/ravendb")]
    [InlineData("https://gitlab.acme.com/ravendb/ravendb")]
    public void DoesNotParseUnsupportedOrInvalidUrls(string url)
    {
        var (org, name) = RepositoryUrl.Parse(url);

        Assert.Null(org);
        Assert.Null(name);
    }
}
