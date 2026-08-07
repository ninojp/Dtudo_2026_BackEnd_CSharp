using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Abstractions;

namespace ApiIdentity.Tests;

public sealed class ServiceTokenEndpointTests
{
    private const string DatabaseName = "DtudoIdentity.ServiceTokenEndpointTests";
    private const string Audience = "urn:dtudo:api-my-animelist";

    [Fact]
    public async Task IssuesAServiceJwtOnlyForTheBoundCertificateAndRequestedPermissions()
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [OpenIddictConstants.Parameters.GrantType] = OpenIddictConstants.GrantTypes.ClientCredentials,
                [OpenIddictConstants.Parameters.ClientId] = "api-my-animes",
                [OpenIddictConstants.Parameters.Scope] = "service.mal.read",
                [OpenIddictConstants.Parameters.Resource] = Audience
            })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tokenResponse = await response.Content.ReadFromJsonAsync<AccessTokenResponse>();
        Assert.NotNull(tokenResponse);
        Assert.Equal("Bearer", tokenResponse.TokenType, ignoreCase: true);
        Assert.False(string.IsNullOrWhiteSpace(tokenResponse.AccessToken));

        var tokenParts = tokenResponse.AccessToken.Split('.');
        Assert.Equal(3, tokenParts.Length);
        using var payload = JsonDocument.Parse(
            Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlDecode(tokenParts[1]));
        Assert.Equal(Audience, payload.RootElement.GetProperty("aud").GetString());
        Assert.Equal("service.mal.read", payload.RootElement.GetProperty("scope").GetString());
        Assert.Equal("api-my-animes", payload.RootElement.GetProperty("client_id").GetString());
    }

    [Theory]
    [InlineData(OpenIddictConstants.Parameters.ClientSecret, "must-not-be-accepted")]
    [InlineData(OpenIddictConstants.Parameters.ClientAssertion, "must-not-be-accepted")]
    public async Task RejectsSharedSecretAuthenticationEvenWhenTheCertificateIsValid(
        string parameter,
        string value)
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [OpenIddictConstants.Parameters.GrantType] = OpenIddictConstants.GrantTypes.ClientCredentials,
                [OpenIddictConstants.Parameters.ClientId] = "api-my-animes",
                [parameter] = value,
                [OpenIddictConstants.Parameters.Scope] = "service.mal.read",
                [OpenIddictConstants.Parameters.Resource] = Audience
            })
        };

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectsAnAudienceOutsideTheClientBinding()
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            certificate,
            audience: "urn:dtudo:api-other",
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectsAScopeOutsideTheClientBinding()
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            certificate,
            audience: Audience,
            scope: "catalog.write"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RejectsAnUnknownClientId()
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            certificate,
            clientId: "unknown-service",
            audience: Audience,
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectsTheWrongClientCertificate()
    {
        using var registeredCertificate = CreateClientCertificate("registered");
        using var wrongCertificate = CreateClientCertificate("wrong");
        await using var factory = CreateFactory(
            registeredCertificate,
            presentedCertificate: wrongCertificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            wrongCertificate,
            audience: Audience,
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RejectsARequestWithoutAClientCertificate()
    {
        using var certificate = CreateClientCertificate();
        await using var factory = CreateFactory(certificate, attachPresentedCertificate: false);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            certificate,
            audience: Audience,
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AcceptsThePreviousCertificateDuringTheConfiguredOverlap()
    {
        using var activeCertificate = CreateClientCertificate("active");
        using var previousCertificate = CreateClientCertificate("previous");
        await using var factory = CreateFactory(
            activeCertificate,
            previousCertificate,
            DateTimeOffset.UtcNow.AddMinutes(5),
            previousCertificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            previousCertificate,
            audience: Audience,
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RejectsThePreviousCertificateAfterTheOverlapEnds()
    {
        using var activeCertificate = CreateClientCertificate("active");
        using var previousCertificate = CreateClientCertificate("previous");
        await using var factory = CreateFactory(
            activeCertificate,
            previousCertificate,
            DateTimeOffset.UtcNow.AddMinutes(-1),
            previousCertificate);
        using var client = CreateClient(factory);

        var response = await client.SendAsync(CreateTokenRequest(
            previousCertificate,
            audience: Audience,
            scope: "service.mal.read"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost")
        });

    private static HttpRequestMessage CreateTokenRequest(
        X509Certificate2 certificate,
        string clientId = "api-my-animes",
        string audience = Audience,
        string scope = "service.mal.read") => new(HttpMethod.Post, "/connect/token")
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                [OpenIddictConstants.Parameters.GrantType] = OpenIddictConstants.GrantTypes.ClientCredentials,
                [OpenIddictConstants.Parameters.ClientId] = clientId,
                [OpenIddictConstants.Parameters.Scope] = scope,
                [OpenIddictConstants.Parameters.Resource] = audience
            })
        };

    private static WebApplicationFactory<Program> CreateFactory(
        X509Certificate2 activeCertificate,
        X509Certificate2? previousCertificate = null,
        DateTimeOffset? previousAcceptedUntilUtc = null,
        X509Certificate2? presentedCertificate = null,
        bool attachPresentedCertificate = true)
    {
        var connectionString = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = DatabaseName,
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
                        ["IdentityDatabase:DatabaseName"] = DatabaseName,
                        ["ConnectionStrings:IdentityDb"] = connectionString,
                        ["LocalProvisioning:AdministrationSecret"] = CreateAdministrationSecret(),
                        ["ServiceAuthentication:Enabled"] = "true",
                        ["ServiceAuthentication:AccessTokenLifetimeSeconds"] = "300",
                        ["ServiceAuthentication:Clients:0:ClientId"] = "api-my-animes",
                        ["ServiceAuthentication:Clients:0:CertificateThumbprints:0"] = activeCertificate.Thumbprint,
                        ["ServiceAuthentication:Clients:0:AllowedScopes:0"] = "service.mal.read",
                        ["ServiceAuthentication:Clients:0:AllowedAudiences:0"] = Audience
                    });
                    if (previousCertificate is not null)
                    {
                        configuration.AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["ServiceAuthentication:Clients:0:CertificateThumbprints:1"] = previousCertificate.Thumbprint,
                            ["ServiceAuthentication:Clients:0:PreviousCertificateAcceptedUntilUtc"] =
                                previousAcceptedUntilUtc?.ToString("O")
                        });
                    }
                });
                var certificateToPresent = presentedCertificate
                    ?? (attachPresentedCertificate ? activeCertificate : null);
                if (certificateToPresent is not null)
                {
                    builder.ConfigureServices(services =>
                        services.AddSingleton<IStartupFilter>(new ClientCertificateStartupFilter(certificateToPresent)));
                }
            });
    }

    private static X509Certificate2 CreateClientCertificate(string name = "api-my-animes-test")
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={name}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        request.CertificateExtensions.Add(new X509EnhancedKeyUsageExtension(
            new OidCollection { new(LibDtudo.Shared.Security.ServiceCertificateValidator.ClientAuthenticationEku) },
            critical: true));
        return request.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddMinutes(-5),
            DateTimeOffset.UtcNow.AddMinutes(30));
    }

    private static string CreateAdministrationSecret() =>
        Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
            RandomNumberGenerator.GetBytes(32));

    private sealed class AccessTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("token_type")]
        public string TokenType { get; set; } = string.Empty;
    }

    private sealed class ClientCertificateStartupFilter(X509Certificate2 certificate) : IStartupFilter
    {
        public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
            application =>
            {
                application.Use(async (context, nextRequest) =>
                {
                    context.Connection.ClientCertificate = certificate;
                    await nextRequest();
                });
                next(application);
            };
    }
}
