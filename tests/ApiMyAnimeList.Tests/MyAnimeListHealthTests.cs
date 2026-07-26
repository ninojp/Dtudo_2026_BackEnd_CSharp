using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimeList.Tests;

public class MyAnimeListHealthTests
{
    [Fact]
    public async Task Health_ReturnsOkWithoutCallingExternalApi()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MyAnimeList:ClientId"] = "test-client-id"
                    });
                });
            });

        var response = await app.CreateClient().GetAsync("/ApiMyAnimeList/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
