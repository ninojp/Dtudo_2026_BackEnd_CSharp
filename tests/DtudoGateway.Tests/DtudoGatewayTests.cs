using System.Net;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using DtudoGateway.Configuration;
using DtudoGateway.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Yarp.ReverseProxy.Configuration;

namespace DtudoGateway.Tests;

public sealed class DtudoGatewayTests
{
    private const string PublicOrigin = "https://localhost:7120";
    private const string SessionCookieName = "__Host-dtudo-bff";

    [Fact]
    public async Task LoginRejectsRedirectOutsideTheAllowlist()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            "/bff/login?returnUrl=https%3A%2F%2Fevil.example%2Fsteal");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task LoginAllowsAnAbsoluteRedirectOnlyForTheConfiguredOrigin()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            "/bff/login?returnUrl=https%3A%2F%2Flocalhost%3A7120%2Fcatalog");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    [Fact]
    public async Task LoginRejectsRedirectsContainingUserInfo()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync(
            "/bff/login?returnUrl=https%3A%2F%2Fatacante%40localhost%3A7120%2Fcatalog");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task LoginUsesAuthorizationCodeAndS256PkceWithTheFixedCallback()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/bff/login?returnUrl=%2Fcatalog");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var location = response.Headers.Location!;
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(location.Query);
        Assert.Equal("https", location.Scheme);
        Assert.Equal("localhost", location.Host);
        Assert.Equal("/identity/connect/authorize", location.AbsolutePath);
        Assert.Equal("code", query[OpenIdConnectParameterNames.ResponseType].ToString());
        Assert.Equal("S256", query["code_challenge_method"].ToString());
        Assert.False(string.IsNullOrWhiteSpace(query["code_challenge"].ToString()));
        Assert.Equal(
            PublicOrigin + "/signin-oidc",
            query[OpenIdConnectParameterNames.RedirectUri].ToString());
    }

    [Fact]
    public async Task LogoutRejectsMissingAntiforgeryToken()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await AddAuthenticatedSessionCookieAsync(factory, client);

        using var antiforgeryResponse = await client.GetAsync("/bff/antiforgery");
        var antiforgeryToken = await ReadTokenAsync(antiforgeryResponse);
        var antiforgeryCookie = GetCookieValue(antiforgeryResponse, "__Host-dtudo-xsrf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout");
        request.Headers.Add("Cookie", $"{SessionCookieName}=invalid-session; __Host-dtudo-xsrf={antiforgeryCookie}");
        request.Headers.Remove("Cookie");
        request.Headers.Add("Cookie", GetSessionCookie(client, antiforgeryCookie));
        request.Content = new StringContent(string.Empty);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_csrf", await response.Content.ReadAsStringAsync(), StringComparison.Ordinal);
        Assert.False(string.IsNullOrWhiteSpace(antiforgeryToken));
    }

    [Fact]
    public async Task LogoutValidatesCsrfAndDeletesTheServerSideSessionCookie()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        var sessionCookie = await AddAuthenticatedSessionCookieAsync(factory, client);

        using var antiforgeryResponse = await client.GetAsync("/bff/antiforgery");
        var antiforgeryToken = await ReadTokenAsync(antiforgeryResponse);
        var antiforgeryCookie = GetCookieValue(antiforgeryResponse, "__Host-dtudo-xsrf");

        using var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout?returnUrl=%2F");
        request.Headers.Add("Cookie", $"{sessionCookie}; __Host-dtudo-xsrf={antiforgeryCookie}");
        request.Headers.Add("X-CSRF-TOKEN", antiforgeryToken);
        request.Content = new StringContent(string.Empty);

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains(
            PublicOrigin + "/identity/connect/logout",
            response.Headers.Location?.ToString(),
            StringComparison.Ordinal);
        Assert.Contains(
            response.Headers.GetValues("Set-Cookie"),
            value => value.Contains(SessionCookieName, StringComparison.Ordinal)
                && value.Contains("expires=Thu, 01 Jan 1970", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task CookieAndAntiforgerySettingsAreSecureAndTheTicketStoreIsServerSide()
    {
        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var cookieOptions = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get("BffCookie");
        var antiforgeryOptions = scope.ServiceProvider
            .GetRequiredService<IOptions<AntiforgeryOptions>>()
            .Value;

        Assert.Equal(SessionCookieName, cookieOptions.Cookie.Name);
        Assert.True(cookieOptions.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, cookieOptions.Cookie.SecurePolicy);
        Assert.Equal(SameSiteMode.Lax, cookieOptions.Cookie.SameSite);
        Assert.Equal(TimeSpan.FromMinutes(120), cookieOptions.ExpireTimeSpan);
        Assert.True(cookieOptions.SlidingExpiration);
        Assert.False(cookieOptions.LogoutPath.HasValue);
        Assert.NotNull(cookieOptions.SessionStore);
        Assert.Equal("__Host-dtudo-xsrf", antiforgeryOptions.Cookie.Name);
        Assert.False(antiforgeryOptions.Cookie.HttpOnly);
        Assert.Equal(CookieSecurePolicy.Always, antiforgeryOptions.Cookie.SecurePolicy);
        Assert.Equal("X-CSRF-TOKEN", antiforgeryOptions.HeaderName);
    }

    [Fact]
    public async Task NewSessionForTheSameSubjectReplacesThePreviousServerSideTicket()
    {
        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var cookieOptions = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get("BffCookie");
        var ticketStore = cookieOptions.SessionStore!;

        static AuthenticationTicket CreateTicket() => new(
            new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("sub", "account-1")],
                "oidc")),
            new AuthenticationProperties
            {
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            },
            "BffCookie");

        var previousKey = await ticketStore.StoreAsync(CreateTicket());
        var currentKey = await ticketStore.StoreAsync(CreateTicket());

        Assert.Null(await ticketStore.RetrieveAsync(previousKey));
        Assert.NotNull(await ticketStore.RetrieveAsync(currentKey));
    }

    [Fact]
    public async Task MeDoesNotReturnAccessOrRefreshTokensToTheBrowser()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);
        await AddAuthenticatedSessionCookieAsync(factory, client);

        using var response = await client.GetAsync("/bff/me");
        var body = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain("access_token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("refresh_token", body, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("server-only-access-token", body, StringComparison.Ordinal);
        Assert.DoesNotContain("server-only-refresh-token", body, StringComparison.Ordinal);
    }

    [Fact]
    public async Task GatewayDoesNotExposeInternalOrMutatingRoutes()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var writeResponse = await client.PostAsync(
            "/api/catalog/animes",
            new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));
        using var internalResponse = await client.GetAsync("/apiLocal/Anime");
        using var tokenResponse = await client.PostAsync("/identity/connect/token", content: null);
        using var swaggerResponse = await client.GetAsync("/swagger");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, writeResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, internalResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, tokenResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, swaggerResponse.StatusCode);
    }

    [Fact]
    public async Task ReverseProxyConfigurationRequiresAuthenticationForCatalogRoutes()
    {
        await using var factory = CreateFactory();
        using var scope = factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<IProxyConfigProvider>();
        var config = provider.GetConfig();

        Assert.Equal(13, config.Routes.Count);
        Assert.All(config.Routes, route =>
        {
            Assert.Equal([HttpMethod.Get.Method], route.Match.Methods);
        });

        var catalogRoutes = config.Routes.Where(route => route.RouteId.StartsWith("catalog-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(5, catalogRoutes.Length);
        Assert.All(catalogRoutes, route =>
            Assert.Equal(GatewayRouteConfiguration.AuthenticatedCatalogPolicy, route.AuthorizationPolicy));

        var identityRoutes = config.Routes.Where(route => route.RouteId.StartsWith("identity-", StringComparison.Ordinal)).ToArray();
        Assert.Equal(2, identityRoutes.Length);
        Assert.All(identityRoutes, route =>
            Assert.Equal(GatewayRouteConfiguration.AnonymousPolicy, route.AuthorizationPolicy));
    }

    [Fact]
    public void CatalogRoutesAreAuthenticatedAndUseExpectedReadOnlyBackendPaths()
    {
        var routes = GatewayRouteConfiguration.CreateRoutes();
        var catalogRoutes = routes.Where(route => route.RouteId.StartsWith("catalog-", StringComparison.Ordinal)).ToArray();
        var expectedBackendPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["catalog-animes-list"] = "/apiLocal/Anime",
            ["catalog-animes-search"] = "/apiLocal/Anime/buscar",
            ["catalog-anime-by-id"] = "/apiLocal/Anime/{id}",
            ["catalog-collections-list"] = "/apiLocal/MyAnime/public",
            ["catalog-collection-by-id"] = "/apiLocal/MyAnime/public/{id}",
        };

        Assert.Equal(5, catalogRoutes.Length);
        Assert.All(catalogRoutes, route =>
        {
            Assert.Equal(GatewayRouteConfiguration.AuthenticatedCatalogPolicy, route.AuthorizationPolicy);
            Assert.Equal([HttpMethod.Get.Method], route.Match.Methods);
            var backendPaths = (route.Transforms ?? [])
                .SelectMany(transform => transform.Values)
                .Where(value => value.StartsWith("/apiLocal/", StringComparison.Ordinal));
            Assert.Contains(expectedBackendPaths[route.RouteId], backendPaths);
        });
    }

    [Fact]
    public void MusicRoutesAreAuthenticatedReadOnlyRoutesForTheApiMusicXContract()
    {
        var routes = GatewayRouteConfiguration.CreateRoutes();
        var musicRoutes = routes.Where(route => route.RouteId.StartsWith("musicx-", StringComparison.Ordinal)).ToArray();
        var expectedBackendPaths = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["musicx-collections-list"] = "/apiLocal/collections",
            ["musicx-collection-by-id"] = "/apiLocal/collections/{id}",
            ["musicx-collection-releases"] = "/apiLocal/collections/{id}/releases",
            ["musicx-artists-list"] = "/apiLocal/artists",
            ["musicx-artist-by-id"] = "/apiLocal/artists/{id}",
            ["musicx-release-by-id"] = "/apiLocal/releases/{id}",
        };

        Assert.Equal(expectedBackendPaths.Count, musicRoutes.Length);
        Assert.All(musicRoutes, route =>
        {
            Assert.Equal(GatewayRouteConfiguration.MusicClusterId, route.ClusterId);
            Assert.Equal(GatewayRouteConfiguration.AuthenticatedCatalogPolicy, route.AuthorizationPolicy);
            Assert.Equal([HttpMethod.Get.Method], route.Match.Methods);
            var backendPaths = (route.Transforms ?? [])
                .SelectMany(transform => transform.Values)
                .Where(value => value.StartsWith("/apiLocal/", StringComparison.Ordinal));
            Assert.Contains(expectedBackendPaths[route.RouteId], backendPaths);
            Assert.DoesNotContain(
                route.Transforms ?? [],
                transform => transform.TryGetValue("RequestHeaderRemove", out var header)
                    && header.Equals("Authorization", StringComparison.OrdinalIgnoreCase));
        });
    }

    [Fact]
    public async Task UnauthenticatedCatalogRequestsRedirectToLoginAndExposeNoIndexHeader()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var liveResponse = await client.GetAsync("/health/live");
        Assert.Equal(HttpStatusCode.OK, liveResponse.StatusCode);
        Assert.Equal("noindex, nofollow, noarchive", liveResponse.Headers.GetValues("X-Robots-Tag").Single());

        using var catalogResponse = await client.GetAsync("/api/catalog/animes");
        Assert.Equal(HttpStatusCode.Redirect, catalogResponse.StatusCode);
        Assert.Contains("/identity/connect/authorize", catalogResponse.Headers.Location?.ToString(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task HealthChecksAreNotBlockedByTheGlobalRateLimiter()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        for (var attempt = 0; attempt < 65; attempt++)
        {
            using var response = await client.GetAsync("/health/live");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }

    [Fact]
    public async Task DevelopmentDoesNotRateLimitUnauthenticatedBffChecks()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        for (var attempt = 0; attempt < 65; attempt++)
        {
            using var response = await client.GetAsync("/bff/me");
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }

    [Fact]
    public async Task UnauthenticatedBffSessionCheckReturnsUnauthorizedWithoutOidcRedirect()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/bff/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task GetLogoutRedirectsToFrontendLogoutFlowInsteadOfReturningMethodNotAllowed()
    {
        await using var factory = CreateFactory();
        using var client = CreateClient(factory);

        using var response = await client.GetAsync("/bff/logout?returnUrl=%2Fauth%2Flogin");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("http://localhost:5173/auth/logout", response.Headers.Location?.ToString());
    }

    private static HttpClient CreateClient(WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri(PublicOrigin),
            AllowAutoRedirect = false,
            HandleCookies = false
        });

    private static WebApplicationFactory<Program> CreateFactory(string environment = "Development") =>
        new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(environment);
                builder.UseSetting("Gateway:PublicOrigin", PublicOrigin);
                builder.UseSetting("Gateway:AllowedRedirectOrigins:0", PublicOrigin);
                builder.UseSetting("Gateway:ApiMyAnimesBaseUrl", "https://127.0.0.1:1/");
                builder.UseSetting("Gateway:ApiMusicXBaseUrl", "https://127.0.0.1:3/");
                builder.UseSetting("OpenIdConnect:Authority", "https://identity.test/");
                builder.UseSetting("OpenIdConnect:ClientId", "dtudo-gateway-test");
                builder.UseSetting("OpenIdConnect:ClientSecret", "test-client-secret");
                builder.UseSetting("OpenIdConnect:Scopes:0", "openid");
                builder.UseSetting("OpenIdConnect:Scopes:1", "profile");
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.Sources.Clear();
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["Gateway:PublicOrigin"] = PublicOrigin,
                        ["Gateway:AllowedRedirectOrigins:0"] = PublicOrigin,
                        ["Gateway:AllowedCorsOrigins:0"] = PublicOrigin,
                        ["Gateway:TrustedProxyAddresses:0"] = "127.0.0.1",
                        ["Gateway:TrustedProxyAddresses:1"] = "::1",
                        ["Gateway:ApiMyAnimesBaseUrl"] = "https://127.0.0.1:1/",
                        ["Gateway:ApiMusicXBaseUrl"] = "https://127.0.0.1:3/",
                        ["Gateway:ApiIdentityBaseUrl"] = "https://127.0.0.1:2/",
                        ["Gateway:MaxRequestBodyBytes"] = "1048576",
                        ["Gateway:RateLimitPermitLimit"] = "60",
                        ["Gateway:RateLimitWindowSeconds"] = "60",
                        ["OpenIdConnect:Authority"] = "https://identity.test/",
                        ["OpenIdConnect:ClientId"] = "dtudo-gateway-test",
                        ["OpenIdConnect:ClientSecret"] = "test-client-secret",
                        ["OpenIdConnect:Scopes:0"] = "openid",
                        ["OpenIdConnect:Scopes:1"] = "profile"
                    });
                });
                builder.ConfigureTestServices(services =>
                {
                    services.PostConfigure<GatewayOptions>(options =>
                    {
                        options.PublicOrigin = PublicOrigin;
                        options.AllowedRedirectOrigins = [PublicOrigin];
                        options.AllowedCorsOrigins = [PublicOrigin];
                        options.TrustedProxyAddresses = ["127.0.0.1", "::1"];
                        options.ApiMyAnimesBaseUrl = "https://127.0.0.1:1/";
                        options.ApiMusicXBaseUrl = "https://127.0.0.1:3/";
                        options.ApiIdentityBaseUrl = "https://127.0.0.1:2/";
                        options.MaxRequestBodyBytes = 1_048_576;
                        options.RateLimitPermitLimit = 60;
                        options.RateLimitWindowSeconds = 60;
                    });
                    services.PostConfigure<GatewayOpenIdConnectOptions>(options =>
                    {
                        options.Authority = "https://identity.test/";
                        options.ClientId = "dtudo-gateway-test";
                        options.ClientSecret = "test-client-secret";
                        options.Scopes = ["openid", "profile"];
                    });
                    services.PostConfigure<OpenIdConnectOptions>("oidc", options =>
                    {
                        options.Authority = "https://identity.test/";
                        options.ClientId = "dtudo-gateway-test";
                        options.ClientSecret = "test-client-secret";
                        var configuration = new OpenIdConnectConfiguration
                        {
                            Issuer = "https://identity.test/",
                            AuthorizationEndpoint = "https://identity.test/connect/authorize",
                            TokenEndpoint = "https://identity.test/connect/token",
                            EndSessionEndpoint = "https://identity.test/connect/logout"
                        };
                        options.Configuration = configuration;
                        options.ConfigurationManager = new StaticConfigurationManager<OpenIdConnectConfiguration>(configuration);
                    });
                });
            });

    private static async Task<string> AddAuthenticatedSessionCookieAsync(
        WebApplicationFactory<Program> factory,
        HttpClient client)
    {
        using var scope = factory.Services.CreateScope();
        var cookieOptions = scope.ServiceProvider
            .GetRequiredService<IOptionsMonitor<CookieAuthenticationOptions>>()
            .Get("BffCookie");
        var ticketStore = cookieOptions.SessionStore!;
        var identity = new ClaimsIdentity(
            [
                new Claim("sub", "account-1"),
                new Claim("name", "Test User"),
                new Claim("email", "test@example.test")
            ],
            "oidc");
        var properties = new AuthenticationProperties
        {
            IsPersistent = true,
            ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
        };
        properties.StoreTokens(
        [
            new AuthenticationToken { Name = "access_token", Value = "server-only-access-token" },
            new AuthenticationToken { Name = "refresh_token", Value = "server-only-refresh-token" }
        ]);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), properties, "BffCookie");
        var key = await ticketStore.StoreAsync(ticket);
        var protectedKey = cookieOptions.TicketDataFormat.Protect(
            new AuthenticationTicket(
                new ClaimsPrincipal(new ClaimsIdentity(
                    [new Claim("Microsoft.AspNetCore.Authentication.Cookies-SessionId", key)],
                    "BffCookie")),
                new AuthenticationProperties
                {
                    ExpiresUtc = properties.ExpiresUtc
                },
                "BffCookie"));
        var cookie = $"{cookieOptions.Cookie.Name}={protectedKey}";
        client.DefaultRequestHeaders.Remove("Cookie");
        client.DefaultRequestHeaders.Add("Cookie", cookie);
        return cookie;
    }

    private static async Task<string> ReadTokenAsync(HttpResponseMessage response)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("token").GetString()!;
    }

    private static string GetCookieValue(HttpResponseMessage response, string name)
    {
        var cookie = response.Headers.GetValues("Set-Cookie")
            .Single(value => value.StartsWith(name + "=", StringComparison.Ordinal));
        return cookie[(name.Length + 1)..].Split(';', 2)[0];
    }

    private static string GetSessionCookie(HttpClient client, string antiforgeryCookie) =>
        client.DefaultRequestHeaders.GetValues("Cookie").Single()
        + "; __Host-dtudo-xsrf="
        + antiforgeryCookie;
}
