using ApiIdentity.Authorization;
using ApiIdentity.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;

namespace ApiIdentity.Tests;

public sealed class ApiIdentityStartupTests
{
    private const string TestDatabaseName = "DtudoIdentity.ApiIdentityTests";

    [Fact]
    public async Task StartsAndPublishesOpenIdDiscoveryWithoutPublicRegistration()
    {
        await using var factory = CreateFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        var healthResponse = await client.GetAsync("/health/live");
        var discoveryResponse = await client.GetAsync("/.well-known/openid-configuration");
        var registrationResponse = await client.PostAsync("/register", content: null);

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, registrationResponse.StatusCode);
    }

    [Fact]
    public void ConfiguresAnIdentityDatabaseSeparateFromOtherServiceContexts()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

        Assert.Equal(TestDatabaseName, context.Database.GetDbConnection().Database);
    }

    [Fact]
    public void FailsClosedWhenTheIdentityDatabaseConnectionIsMissing()
    {
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:IdentityDb"] = string.Empty
                    });
                });
            });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("ConnectionStrings:IdentityDb", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RegistersAClaimPolicyForEveryCatalogPermission()
    {
        using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var authorizationOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthorizationOptions>>().Value;

        foreach (var permission in AuthorizationCatalog.AllPermissions)
        {
            var policy = authorizationOptions.GetPolicy(AuthorizationCatalog.PolicyName(permission.Key));

            Assert.NotNull(policy);
            Assert.Contains(policy.Requirements, requirement => requirement is ClaimsAuthorizationRequirement claimRequirement
                && claimRequirement.ClaimType == AuthorizationCatalog.PermissionClaimType
                && claimRequirement.AllowedValues?.Contains(permission.Key) == true);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            InitialCatalog = TestDatabaseName
        }.ConnectionString;

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["IdentityDatabase:DatabaseName"] = TestDatabaseName,
                        ["ConnectionStrings:IdentityDb"] = connectionString
                    });
                });
            });
    }
}
