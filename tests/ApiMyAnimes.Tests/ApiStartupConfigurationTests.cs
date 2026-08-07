using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApiMyAnimes.Tests;

public sealed class ApiStartupConfigurationTests
{
    [Fact]
    public void MissingDatabaseConnection_FailsClosedDuringStartup()
    {
        using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = "https://identity.test",
                        ["Authentication:Audience"] = "api-my-animes",
                        ["ConnectionStrings:LocalDbConnection"] = string.Empty
                    });
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("ConnectionStrings:LocalDbConnection", exception.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public void MissingAuthenticationIssuer_FailsClosedDuringStartup()
    {
        using var app = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Authentication:Issuer"] = string.Empty,
                        ["Authentication:Audience"] = "api-my-animes",
                        ["ConnectionStrings:LocalDbConnection"] = "Server=(localdb)\\MSSQLLocalDB;Database=Dtudo2026Tests;Trusted_Connection=True;TrustServerCertificate=True"
                    });
                });
            });

        var exception = Assert.ThrowsAny<Exception>(() => app.CreateClient());

        Assert.Contains("Authentication:Issuer", exception.ToString(), StringComparison.Ordinal);
    }
}