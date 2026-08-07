using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Provisioning;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Http.Json;

namespace ApiIdentity.Tests;

public sealed class LocalProvisioningEndpointTests
{
    private const string AdministrationSecret = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string StrongPassword = "Dtudo2026!InitialPassword";

    [Fact]
    public void LocalProvisioningGuardRequiresLoopbackAndTheConfiguredSecret()
    {
        var guard = new LocalProvisioningRequestGuard(Options.Create(new LocalProvisioningOptions
        {
            AdministrationSecret = AdministrationSecret
        }));
        var localContext = new DefaultHttpContext();
        localContext.Connection.RemoteIpAddress = IPAddress.Loopback;
        localContext.Request.Headers[LocalProvisioningRequestGuard.AdministrationSecretHeader] = AdministrationSecret;

        var remoteContext = new DefaultHttpContext();
        remoteContext.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.10");
        remoteContext.Request.Headers[LocalProvisioningRequestGuard.AdministrationSecretHeader] = AdministrationSecret;

        Assert.True(guard.IsAuthorized(localContext));
        Assert.False(guard.IsAuthorized(remoteContext));
    }

    [Fact]
    public async Task ActivationEndpointUsesGenericFailureAndRateLimitsAttempts()
    {
        var databaseName = $"DtudoIdentity.Stage11EndpointTests.{Guid.NewGuid():N}";
        using var factory = CreateFactory(databaseName);
        try
        {
            InitialSecretDelivery delivery;
            await using (var setupScope = factory.Services.CreateAsyncScope())
            {
                var context = setupScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await context.Database.MigrateAsync();

                var service = setupScope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
                var bootstrap = await service.BootstrapAsync(new BootstrapAccountRequest(
                    "endpoint-admin",
                    "endpoint-admin@example.test"));
                delivery = Assert.IsType<InitialSecretDelivery>(bootstrap.Delivery);
            }

            using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
            {
                BaseAddress = new Uri("https://localhost")
            });
            var unknownResponse = await ActivateAsync(client, Guid.NewGuid());
            var invalidResponse = await ActivateAsync(client, delivery.ActivationId);

            Assert.Equal(HttpStatusCode.OK, unknownResponse.StatusCode);
            Assert.Equal(HttpStatusCode.OK, invalidResponse.StatusCode);
            Assert.Equal(
                await unknownResponse.Content.ReadFromJsonAsync<AccountActivationResult>(),
                await invalidResponse.Content.ReadFromJsonAsync<AccountActivationResult>());

            for (var attempt = 0; attempt < 3; attempt++)
            {
                var response = await ActivateAsync(client, Guid.NewGuid());
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            var limitedResponse = await ActivateAsync(client, Guid.NewGuid());
            Assert.Equal(HttpStatusCode.TooManyRequests, limitedResponse.StatusCode);
        }
        finally
        {
            await using var cleanupScope = factory.Services.CreateAsyncScope();
            var context = cleanupScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await context.Database.EnsureDeletedAsync();
        }
    }

    private static Task<HttpResponseMessage> ActivateAsync(HttpClient client, Guid activationId) =>
        client.PostAsJsonAsync(
            "/identity/activate-initial-account",
            new InitialAccountActivationRequest(activationId, "invalid-secret", StrongPassword));

    private static WebApplicationFactory<Program> CreateFactory(string databaseName)
    {
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        }.ConnectionString;

        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["IdentityDatabase:DatabaseName"] = databaseName,
                        ["ConnectionStrings:IdentityDb"] = connectionString,
                        ["LocalProvisioning:AdministrationSecret"] = AdministrationSecret
                    });
                });
            });
    }
}
