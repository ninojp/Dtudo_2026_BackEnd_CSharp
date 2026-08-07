using System.Net;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimeList.Tests;

public class MyAnimeListHealthTests
{
    private const string ServiceAudience = "urn:dtudo:api-my-animelist";

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
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animelist",
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
                builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=health.read;permission=health.read");
        var response = await client.GetAsync("/ApiMyAnimeList/health");

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
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animelist",
                        ["MyAnimeList:ClientId"] = string.Empty,
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("MyAnimeList:ClientId", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingAuthenticationAudience_FailsClosedDuringStartup()
    {
        using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = string.Empty,
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("Authentication:Audience", exception.ToString(), StringComparison.Ordinal);
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
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animelist",
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
                builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });

        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=health.read;permission=health.read");
        using var request = new HttpRequestMessage(HttpMethod.Get, "/ApiMyAnimeList/health");
        request.Headers.TryAddWithoutValidation("X-Correlation-ID", "stage04-health");
        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.TryGetValues("X-Correlation-ID", out var values));
        Assert.Equal("stage04-health", values.Single());
    }

    [Fact]
    public async Task Health_RejectsAnonymousRequests()
    {
        await using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animelist",
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty
                    });
                });
            });

        var response = await app.CreateClient().GetAsync("/ApiMyAnimeList/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_RejectsAnonymousRequests()
    {
        await using var app = CreateApp();

        var response = await app.CreateClient().GetAsync("/ApiMyAnimeList/search?q=naruto");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Search_RequiresServiceMalPermissionAndScope()
    {
        await using var app = CreateApp(useTestAuthentication: true);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=service.mal.read");

        var response = await client.GetAsync("/ApiMyAnimeList/search?q=naruto");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Search_WithServiceMalPermissionAndScope_ReachesController()
    {
        await using var app = CreateApp(useTestAuthentication: true);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=service.mal.read;permission=service.mal.read");

        var response = await client.GetAsync("/ApiMyAnimeList/search");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task ServiceTokenWithoutClientCertificate_IsRejected()
    {
        await using var app = CreateApp(useTestAuthentication: true, enableServiceAuthentication: true);
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add(
            "X-Test-Claims",
            "client_id=api-my-animes;scope=service.mal.read;permission=service.mal.read");

        var response = await client.GetAsync("/ApiMyAnimeList/search?q=naruto");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task OpenApi_RejectsAnonymousRequests()
    {
        await using var app = CreateApp();

        var response = await app.CreateClient().GetAsync("/openapi/v1.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateApp(
        bool useTestAuthentication = false,
        bool enableServiceAuthentication = false)
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animelist",
                        ["MyAnimeList:ClientId"] = "test-client-id",
                        ["Seq:Url"] = string.Empty,
                        ["ServiceAuthentication:Enabled"] = enableServiceAuthentication.ToString(),
                        ["ServiceAuthentication:Clients:0:ClientId"] = "api-my-animes",
                        ["ServiceAuthentication:Clients:0:CertificateThumbprints:0"] = new string('A', 40),
                        ["ServiceAuthentication:Clients:0:AllowedScopes:0"] = "service.mal.read",
                        ["ServiceAuthentication:Clients:0:AllowedAudiences:0"] = ServiceAudience
                    });
                });

                if (useTestAuthentication)
                    builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });
}
