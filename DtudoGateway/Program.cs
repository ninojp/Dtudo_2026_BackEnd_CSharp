using System.Net;
using System.Security.Claims;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using DtudoGateway.Configuration;
using DtudoGateway.Infrastructure;
using Yarp.ReverseProxy;

const string CookieScheme = "BffCookie";
const string OpenIdConnectScheme = "oidc";
const string BffPolicy = "gateway-bff";

var builder = WebApplication.CreateBuilder(args);

builder.Configuration.AddUserSecrets<Program>(optional: true);

var configuredGatewayOptions = builder.Configuration
    .GetSection(GatewayOptions.SectionName)
    .Get<GatewayOptions>() ?? new GatewayOptions();
var configuredOpenIdConnectOptions = builder.Configuration
    .GetSection(GatewayOpenIdConnectOptions.SectionName)
    .Get<GatewayOpenIdConnectOptions>() ?? new GatewayOpenIdConnectOptions();

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = configuredGatewayOptions.MaxRequestBodyBytes;
});

builder.Services.AddOptions<GatewayOptions>()
    .Bind(builder.Configuration.GetSection(GatewayOptions.SectionName))
    .Validate(GatewayOptionsValidator.IsValid, "Gateway deve conter origem HTTPS, allowlist de redirects e destino HTTPS.")
    .ValidateOnStart();
builder.Services.AddOptions<GatewayOpenIdConnectOptions>()
    .Bind(builder.Configuration.GetSection(GatewayOpenIdConnectOptions.SectionName))
    .Validate(GatewayOptionsValidator.IsValid, "OpenIdConnect deve conter authority HTTPS, client id, segredo externo e escopo openid.")
    .ValidateOnStart();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    foreach (var address in configuredGatewayOptions.TrustedProxyAddresses)
    {
        if (!IPAddress.TryParse(address, out var parsedAddress))
        {
            throw new InvalidOperationException("Gateway:TrustedProxyAddresses contem um endereco invalido.");
        }

        options.KnownProxies.Add(parsedAddress);
    }
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicCatalog", policy =>
    {
        policy.WithOrigins(configuredGatewayOptions.AllowedCorsOrigins)
            .WithMethods(HttpMethods.Get, HttpMethods.Head, HttpMethods.Options)
            .WithHeaders("Accept", "Content-Type")
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
    });
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
    {
        if (context.Request.Path.StartsWithSegments("/health"))
        {
            return RateLimitPartition.GetNoLimiter<string>("health");
        }

        var remoteAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var partitionName = IsAuthenticationPath(context.Request.Path)
            ? $"authentication:{remoteAddress}"
            : $"general:{remoteAddress}";
        return RateLimitPartition.GetFixedWindowLimiter(
            partitionName,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = configuredGatewayOptions.RateLimitPermitLimit,
                Window = TimeSpan.FromSeconds(configuredGatewayOptions.RateLimitWindowSeconds),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    });
    options.OnRejected = (context, _) =>
    {
        context.HttpContext.Response.Headers.RetryAfter = configuredGatewayOptions.RateLimitWindowSeconds.ToString();
        return ValueTask.CompletedTask;
    };
});

builder.Services.AddHealthChecks()
    .AddCheck("gateway", () => HealthCheckResult.Healthy(), tags: ["live"]);

var dataProtectionKeyDirectory = new DirectoryInfo(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Dtudo2026",
    "DtudoGateway",
    "DataProtectionKeys"));

var dataProtectionBuilder = builder.Services.AddDataProtection()
    .SetApplicationName("Dtudo2026.DtudoGateway")
    .PersistKeysToFileSystem(dataProtectionKeyDirectory);
if (OperatingSystem.IsWindows())
{
    dataProtectionBuilder.ProtectKeysWithDpapi(protectToLocalMachine: false);
}

builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ServerSideTicketStore>();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "X-CSRF-TOKEN";
    options.Cookie.Name = "__Host-dtudo-xsrf";
    options.Cookie.HttpOnly = false;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
});

var authenticationBuilder = builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = CookieScheme;
    options.DefaultSignInScheme = CookieScheme;
    options.DefaultChallengeScheme = OpenIdConnectScheme;
    options.DefaultSignOutScheme = OpenIdConnectScheme;
})
.AddCookie(CookieScheme, options =>
{
    options.Cookie.Name = "__Host-dtudo-bff";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.Path = "/";
    options.Cookie.IsEssential = true;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(configuredGatewayOptions.SessionIdleTimeoutMinutes);
    options.SlidingExpiration = true;
    options.LoginPath = "/bff/login";
    options.LogoutPath = null;
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/bff"))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        }

        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
});

builder.Services.PostConfigure<CookieAuthenticationOptions>(CookieScheme, options =>
{
    options.LogoutPath = null;
});

    authenticationBuilder.AddOpenIdConnect(OpenIdConnectScheme, options =>
    {
        options.Authority = configuredOpenIdConnectOptions.Authority;
        options.ClientId = configuredOpenIdConnectOptions.ClientId;
        options.ClientSecret = configuredOpenIdConnectOptions.ClientSecret;
        options.SignInScheme = CookieScheme;
        options.CallbackPath = "/signin-oidc";
        options.SignedOutCallbackPath = "/signout-callback-oidc";
        options.ResponseType = "code";
        options.ResponseMode = "query";
        options.UsePkce = true;
        options.SaveTokens = true;
        options.RequireHttpsMetadata = true;
        options.GetClaimsFromUserInfoEndpoint = false;
        options.MapInboundClaims = false;
        options.Scope.Clear();
        foreach (var scope in configuredOpenIdConnectOptions.Scopes)
        {
            options.Scope.Add(scope);
        }
        options.CorrelationCookie.HttpOnly = true;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.CorrelationCookie.SameSite = SameSiteMode.None;
        options.CorrelationCookie.IsEssential = true;
        options.NonceCookie.HttpOnly = true;
        options.NonceCookie.SecurePolicy = CookieSecurePolicy.Always;
        options.NonceCookie.SameSite = SameSiteMode.None;
        options.NonceCookie.IsEssential = true;
        options.Events.OnRedirectToIdentityProvider = context =>
        {
            context.ProtocolMessage.IssuerAddress = BuildPublicUrl(
                configuredGatewayOptions,
                "/identity/connect/authorize");
            context.ProtocolMessage.SetParameter("resource", "urn:dtudo:api-musicx");
            context.ProtocolMessage.RedirectUri = BuildPublicUrl(
                configuredGatewayOptions,
                context.Options.CallbackPath.Value!);
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToIdentityProviderForSignOut = context =>
        {
            if (!RedirectAllowlist.TryGetAllowedRedirect(
                    context.Properties?.RedirectUri,
                    configuredGatewayOptions,
                    out var finalRedirect))
            {
                context.HandleResponse();
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return Task.CompletedTask;
            }

            if (context.Properties is not null)
            {
                context.Properties.RedirectUri = finalRedirect;
            }

            context.ProtocolMessage.IssuerAddress = BuildPublicUrl(
                configuredGatewayOptions,
                "/identity/connect/logout");
            context.ProtocolMessage.PostLogoutRedirectUri = BuildPublicUrl(
                configuredGatewayOptions,
                context.Options.SignedOutCallbackPath.Value!);
            return Task.CompletedTask;
        };
        options.Events.OnSignedOutCallbackRedirect = context =>
        {
            if (!RedirectAllowlist.TryGetAllowedRedirect(
                    context.Properties?.RedirectUri,
                    configuredGatewayOptions,
                    out var finalRedirect))
            {
                finalRedirect = "/";
            }

            finalRedirect = BuildFrontendRedirect(configuredGatewayOptions, finalRedirect);
            context.Response.Redirect(finalRedirect);
            context.HandleResponse();
            return Task.CompletedTask;
        };
        options.Events.OnRemoteFailure = context =>
        {
            context.HandleResponse();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            return context.Response.WriteAsJsonAsync(new { error = "authentication_failed" });
        };
    });

builder.Services
    .AddOptions<CookieAuthenticationOptions>(CookieScheme)
    .Configure<ServerSideTicketStore>((options, ticketStore) => options.SessionStore = ticketStore);

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(GatewayRouteConfiguration.AnonymousPolicy, policy =>
        policy.RequireAssertion(_ => true));
    options.AddPolicy(GatewayRouteConfiguration.AuthenticatedCatalogPolicy, policy =>
        policy.RequireAuthenticatedUser());
    options.AddPolicy(BffPolicy, policy =>
    {
        policy.AddAuthenticationSchemes(CookieScheme);
        policy.RequireAuthenticatedUser();
    });
});

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        GatewayRouteConfiguration.CreateRoutes(),
        GatewayRouteConfiguration.CreateClusters(
            configuredGatewayOptions.ApiMyAnimesBaseUrl,
            configuredGatewayOptions.ApiMusicXBaseUrl,
            configuredGatewayOptions.ApiIdentityBaseUrl));

var app = builder.Build();

app.UseForwardedHeaders();
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";
        context.Response.Headers["X-Robots-Tag"] = "noindex, nofollow, noarchive";
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
        context.Response.Headers["Content-Security-Policy"] = "default-src 'self'; img-src 'self' https: data:; connect-src 'self'; style-src 'self'; script-src 'self'; font-src 'self' data:; object-src 'none'; frame-ancestors 'none'; base-uri 'self'";
        if (!app.Environment.IsDevelopment())
        {
            context.Response.Headers["Strict-Transport-Security"] = "max-age=31536000; includeSubDomains";
        }
        context.Response.Headers.Remove("Server");
        return Task.CompletedTask;
    });

    await next(context);
});
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/catalog"))
    {
        context.Request.Headers.Remove("Authorization");
    }
    else if (context.Request.Path.StartsWithSegments("/identity/connect/authorize")
        || context.Request.Path.StartsWithSegments("/identity/connect/logout"))
    {
        context.Request.Headers.Cookie = RemoveGatewayCookies(context.Request.Headers.Cookie);
    }

    await next(context);
});
app.UseCors("PublicCatalog");
if (!app.Environment.IsDevelopment())
{
    app.UseRateLimiter();
}
app.UseAuthentication();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/catalog/music"))
    {
        context.Request.Headers.Remove("Authorization");
        var accessToken = await context.GetTokenAsync(CookieScheme, "access_token");
        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            context.Request.Headers.Authorization = "Bearer " + accessToken;
        }
    }

    await next(context);
});
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("live"),
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsJsonAsync(new
        {
            status = report.Status == HealthStatus.Healthy ? "ok" : "unavailable"
        });
    }
}).AllowAnonymous();

    app.MapGet("/bff/login", async (HttpContext context) =>
    {
        if (!RedirectAllowlist.TryGetAllowedRedirect(
                context.Request.Query["returnUrl"].ToString(),
                configuredGatewayOptions,
                out var redirect))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_redirect" });
            return;
        }

        await context.ChallengeAsync(
            OpenIdConnectScheme,
            new AuthenticationProperties
            {
                RedirectUri = BuildFrontendRedirect(configuredGatewayOptions, redirect)
            });
    }).AllowAnonymous();

    app.MapGet("/bff/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
    {
        var tokens = antiforgery.GetAndStoreTokens(context);
        return Results.Ok(new { token = tokens.RequestToken });
    }).AllowAnonymous();

    app.MapGet("/bff/logout", () =>
        Results.Redirect(BuildFrontendRedirect(configuredGatewayOptions, "/auth/logout")))
        .AllowAnonymous();

    app.MapGet("/bff/me", (HttpContext context) =>
    {
        var principal = context.User;
        return Results.Ok(new
        {
            authenticated = true,
            user = new
            {
                subject = principal.FindFirstValue("sub"),
                name = principal.FindFirstValue("name") ?? principal.Identity?.Name,
                email = principal.FindFirstValue("email"),
                roles = principal.FindAll("role").Select(claim => claim.Value).ToArray()
            }
        });
    }).RequireAuthorization(BffPolicy);

    app.MapPost("/bff/logout", async (HttpContext context, IAntiforgery antiforgery) =>
    {
        if (!RedirectAllowlist.TryGetAllowedRedirect(
                context.Request.Query["returnUrl"].ToString(),
                configuredGatewayOptions,
                out var redirect))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_redirect" });
            return;
        }

        if (!await antiforgery.IsRequestValidAsync(context))
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "invalid_csrf" });
            return;
        }

        await context.SignOutAsync(CookieScheme);
        await context.SignOutAsync(
            OpenIdConnectScheme,
            new AuthenticationProperties { RedirectUri = redirect });
    }).RequireAuthorization();

app.MapReverseProxy();

app.Run();

static bool IsAuthenticationPath(PathString path) =>
    path.StartsWithSegments("/bff")
    || path.StartsWithSegments("/identity")
    || path.StartsWithSegments("/signin-oidc")
    || path.StartsWithSegments("/signout-callback-oidc");

static string BuildPublicUrl(GatewayOptions options, string path) =>
    options.PublicOrigin.TrimEnd('/') + "/" + path.TrimStart('/');

static string BuildFrontendRedirect(GatewayOptions options, string redirect)
{
    if (string.IsNullOrWhiteSpace(options.FrontendOrigin)
        || !redirect.StartsWith("/", StringComparison.Ordinal)
        || redirect.StartsWith("//", StringComparison.Ordinal))
    {
        return redirect;
    }

    return options.FrontendOrigin.TrimEnd('/') + redirect;
}

static string RemoveGatewayCookies(string? cookieHeader)
{
    if (string.IsNullOrWhiteSpace(cookieHeader))
    {
        return string.Empty;
    }

    return string.Join(
        "; ",
        cookieHeader
            .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(cookie =>
            {
                var separatorIndex = cookie.IndexOf('=');
                var name = separatorIndex >= 0 ? cookie[..separatorIndex] : cookie;
                return !name.Equals("__Host-dtudo-bff", StringComparison.Ordinal)
                    && !name.Equals("__Host-dtudo-xsrf", StringComparison.Ordinal)
                    && !name.StartsWith(".AspNetCore.Correlation.", StringComparison.Ordinal)
                    && !name.StartsWith(".AspNetCore.OpenIdConnect.Nonce.", StringComparison.Ordinal);
            }));
}

public partial class Program;
