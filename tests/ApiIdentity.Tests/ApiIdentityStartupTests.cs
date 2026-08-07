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
        var personalResponse = await client.GetAsync("/identity/me/favorites");

        Assert.Equal(HttpStatusCode.OK, healthResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, discoveryResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, registrationResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, personalResponse.StatusCode);
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
                        ["ConnectionStrings:IdentityDb"] = string.Empty,
                        ["LocalProvisioning:AdministrationSecret"] = CreateAdministrationSecret()
                    });
                });
            });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("ConnectionStrings:IdentityDb", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailsClosedWhenTheLocalProvisioningSecretIsMissing()
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = TestDatabaseName
        }.ConnectionString;
        using var factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["IdentityDatabase:DatabaseName"] = TestDatabaseName,
                        ["ConnectionStrings:IdentityDb"] = connectionString,
                        ["LocalProvisioning:AdministrationSecret"] = string.Empty
                    });
                });
            });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("LocalProvisioning:AdministrationSecret", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailsClosedWhenTheMfaSnapshotLifetimeIsOutsideTheAllowedRange()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["IdentityMfa:SnapshotLifetimeHours"] = "169"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("IdentityMfa", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailsClosedWhenTheFido2TimeoutIsOutsideTheAllowedRange()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["IdentityMfa:Fido2TimeoutMilliseconds"] = "9999"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("IdentityMfa", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailsClosedWhenTheSessionLifetimeExceedsThirtyDays()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["IdentitySessions:LifetimeDays"] = "31"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("IdentitySessions", exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void FailsClosedWhenAnMfaOriginIsNotHttps()
    {
        using var factory = CreateFactory(new Dictionary<string, string?>
        {
            ["IdentityMfa:Origins:0"] = "http://localhost"
        });

        var exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        Assert.Contains("IdentityMfa:Origins", exception.Message, StringComparison.Ordinal);
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

    private static WebApplicationFactory<Program> CreateFactory(
        IReadOnlyDictionary<string, string?>? overrides = null)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = TestDatabaseName
        }.ConnectionString;

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    var settings = new Dictionary<string, string?>
                    {
                        ["IdentityDatabase:DatabaseName"] = TestDatabaseName,
                        ["ConnectionStrings:IdentityDb"] = connectionString,
                        ["LocalProvisioning:AdministrationSecret"] = CreateAdministrationSecret()
                    };
                    if (overrides is not null)
                    {
                        foreach (var item in overrides)
                        {
                            settings[item.Key] = item.Value;
                        }
                    }

                    configuration.AddInMemoryCollection(settings);
                });
            });
    }

    private static string CreateAdministrationSecret() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
}
