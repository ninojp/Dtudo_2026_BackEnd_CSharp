using System.Net;
using System.Net.Http.Json;
using ApiFileStorage.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace ApiFileStorage.Tests;

public sealed class ApiAuthorizationTests : IDisposable
{
    private readonly string _temporaryDirectory;

    public ApiAuthorizationTests()
    {
        _temporaryDirectory = Path.Combine(Path.GetTempPath(), "DtudoFileStorageApiTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_temporaryDirectory, "nested"));
        File.WriteAllText(Path.Combine(_temporaryDirectory, "nested", "safe.txt"), "safe");
    }

    [Fact]
    public async Task AnonymousResolve_IsRejected()
    {
        await using var app = CreateApp();

        using var response = await app.CreateClient().PostAsJsonAsync(
            "/api/file-storage/resolve",
            new { objectId = StorageObjectId.Create("media", "nested/safe.txt") });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AnonymousHealth_IsRejected()
    {
        await using var app = CreateApp();

        using var response = await app.CreateClient().GetAsync("/api/file-storage/health");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task HealthRequiresDedicatedPermissionAndScope()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=filesystem.command;permission=filesystem.command");

        using var response = await client.GetAsync("/api/file-storage/health");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AuthorizedHealthReturnsOperationalDataWithoutPhysicalPaths()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=health.read;permission=health.read");

        using var response = await client.GetAsync("/api/file-storage/health");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("roots", body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("quarantine", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain(_temporaryDirectory, body, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("scope=filesystem.command")]
    [InlineData("permission=filesystem.command")]
    public async Task MissingPermissionOrScope_IsRejected(string claims)
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", claims);

        using var response = await client.PostAsJsonAsync(
            "/api/file-storage/resolve",
            new { objectId = StorageObjectId.Create("media", "nested/safe.txt") });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MatchingPermissionAndScope_ResolvesWithoutReturningPhysicalRoot()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=filesystem.command;permission=filesystem.command");

        using var response = await client.PostAsJsonAsync(
            "/api/file-storage/resolve",
            new { objectId = StorageObjectId.Create("media", "nested/safe.txt") });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(_temporaryDirectory, body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("objectId", body, StringComparison.Ordinal);
        Assert.DoesNotContain("nested/safe.txt", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task MaliciousPath_IsRejectedWithoutPathDetails()
    {
        await using var app = CreateApp();
        var client = app.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Claims", "scope=filesystem.command;permission=filesystem.command");

        using var response = await client.PostAsJsonAsync(
            "/api/file-storage/resolve",
            new { objectId = UncheckedStorageObjectId.Create("media", "nested/%252e%252e/safe.txt") });
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(_temporaryDirectory, body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("%252e", body, StringComparison.OrdinalIgnoreCase);
    }

    private WebApplicationFactory<Program> CreateApp()
        => new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-file-storage",
                        ["Seq:Url"] = string.Empty,
                        ["FileStorage:Roots:0:Id"] = "media",
                        ["FileStorage:Roots:0:Path"] = _temporaryDirectory
                    });
                });
                builder.ConfigureTestServices(services => TestAuthentication.Add(services));
            });

    public void Dispose()
    {
        if (Directory.Exists(_temporaryDirectory))
        {
            Directory.Delete(_temporaryDirectory, recursive: true);
        }
    }
}
