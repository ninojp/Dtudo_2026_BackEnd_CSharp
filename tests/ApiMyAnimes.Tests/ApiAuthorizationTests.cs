using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimes.Tests;

public sealed class ApiAuthorizationTests
{
    [Fact]
    public async Task AnonymousCatalogWrite_IsRejected()
    {
        await using var app = CreateApp();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/apiLocal/Anime/conflito-titulo");

        using var response = await app.CreateClient().SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task MissingCatalogPermission_IsRejected()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=catalog.write");

        using var response = await client.PostAsync("/apiLocal/Anime/conflito-titulo", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MissingCatalogScope_IsRejected()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "permission=catalog.write");

        using var response = await client.PostAsync("/apiLocal/Anime/conflito-titulo", content: null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MatchingCatalogPermissionAndScope_ReachesController()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=catalog.write;permission=catalog.write");

        using var response = await client.PostAsync("/apiLocal/Anime/conflito-titulo", content: null);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CatalogRead_IsExplicitlyPublic()
    {
        await using var app = CreateApp();

        using var response = await app.CreateClient().GetAsync("/apiLocal/Anime?skip=-1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_RejectsAnonymousRequests()
    {
        await using var app = CreateApp();

        using var response = await app.CreateClient().GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Swagger_AllowsHealthReadClaims()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=health.read;permission=health.read");

        using var response = await client.GetAsync("/swagger/v1/swagger.json");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/apiLocal/Auth/register")]
    [InlineData("/apiLocal/Auth/login")]
    [InlineData("/apiLocal/Auth/me/legacy")]
    public async Task LegacyAuthenticationEndpoints_AreNotMapped(string path)
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "sub=legacy-negative");

        using var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private static WebApplicationFactory<Program> CreateApp()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animes",
                        ["ConnectionStrings:LocalDbConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Dtudo2026Tests;Trusted_Connection=True;TrustServerCertificate=True",
                        ["Seq:Url"] = string.Empty
                    });
                });
                builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });
}
