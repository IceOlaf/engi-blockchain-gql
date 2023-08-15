using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System;
using System.IO;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Engi.Substrate.Integration.EngineerTest;


class Response {
    public Data Data { get; set; }
}

class Data {
    public Engineer Engineer { get; set; }
}

class Engineer {
    public string DisplayName { get; set; }
    public string ProfileImageUrl { get; set; }
    public string? Email { get; set; }
    public ulong Balance { get; set; }
    public ulong BountiesSolved { get; set; }
    public ulong BountiesCreated { get; set; }
    public Earnings Earnings { get; set; }
    public string[] Technologies { get; set; }
    public string[] RepositoriesWorkedOn { get; set; }
    public string RootOrganization { get; set; }
}

class Earnings {
    public ulong PastDay { get; set; }
    public ulong PastWeek { get; set; }

    public ulong PastMonth { get; set; }
    public ulong Lifetime { get; set; }
}

public class EngineerTestItem {
    public string Query { get; set; }
    public string Expected { get; set; }
}

public class EngineerQuery {
    public string Query { get; set; }
    public string Variables { get; set; }
}

public class TestDataGenerator
{
    public static IEnumerable<object[]> GetEngineerQuery()
    {
        var filePath = "/source/engi-tests/API/test_set.json";
        var path = Path.GetFullPath(filePath, Directory.GetCurrentDirectory());

        if (!File.Exists(path))
        {
            throw new ArgumentException($"Could not find file at path: {path}");
        }

        var fileData = File.ReadAllText(filePath);

        var allData = JsonConvert.DeserializeObject<EngineerTestItem[]>(fileData);
        foreach (var data in allData)
        {
            yield return new object[] { data.Query, data.Expected };
        }
    }
}

public class EngineerTest
{
    [Theory]
    [MemberData(nameof(TestDataGenerator.GetEngineerQuery), MemberType = typeof(TestDataGenerator))]
    public async Task EngineerData(string queryjson, string expectedjson)
    {
        var http = new HttpClient();

        var query = JsonConvert.DeserializeObject<EngineerQuery>(queryjson);
        var content = JsonContent.Create(query);
        content.Headers.ContentType.MediaType = "application/json";

        var response = await http.PostAsync("http://api:8000/api/graphql", content);

        response.EnsureSuccessStatusCode();

        var actual = response.Content.ReadFromJsonAsync<Response?>().Result;
        var expected = JsonConvert.DeserializeObject<Response?>(expectedjson);
        Assert.Equivalent(expected, actual);
    }
}
