
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace Engi.Substrate.Integration;

public class HealthTest
{
    [Fact]
    public async Task Healthy()
    {
        var http = new HttpClient();

        var response = await http.GetAsync("http://api:8000/api/health");

        response.EnsureSuccessStatusCode();
    }
}
