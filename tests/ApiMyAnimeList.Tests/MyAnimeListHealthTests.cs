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
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        var response = await app.CreateClient().GetAsync("/ApiMyAnimeList/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void MissingClientId_FailsClosedDuringStartup()
    {
        using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MyAnimeList:ClientId"] = string.Empty,
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("MyAnimeList:ClientId", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task Health_EchoesProvidedCorrelationId()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/ApiMyAnimeList/health");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "stage04-health");
        using var response = await app.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal("stage04-health", values.Single());
    }
}
