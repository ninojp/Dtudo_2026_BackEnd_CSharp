using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;
using DtudoGateway.Configuration;
using DtudoGateway.Infrastructure;
using Yarp.ReverseProxy;

const string CookieScheme = "BffCookie";
const string OpenIdConnectScheme = "oidc";

var builder = WebApplication.CreateBuilder(args);

var configuredGatewayOptions = builder.Configuration
    .GetSection(GatewayOptions.SectionName)
    .Get<GatewayOptions>() ?? new GatewayOptions();
var configuredOpenIdConnectOptions = builder.Configuration
    .GetSection(GatewayOpenIdConnectOptions.SectionName)
    .Get<GatewayOpenIdConnectOptions>() ?? new GatewayOpenIdConnectOptions();

builder.Services.AddOptions<GatewayOptions>()
    .Bind(builder.Configuration.GetSection(GatewayOptions.SectionName))
    .Validate(GatewayOptionsValidator.IsValid, "Gateway deve conter origem HTTPS, allowlist de redirects e destino HTTPS.")
    .ValidateOnStart();
builder.Services.AddOptions<GatewayOpenIdConnectOptions>()
    .Bind(builder.Configuration.GetSection(GatewayOpenIdConnectOptions.SectionName))
    .Validate(GatewayOptionsValidator.IsValid, "OpenIdConnect deve conter authority HTTPS, client id, segredo externo e escopo openid.")
    .ValidateOnStart();

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

builder.Services.AddAuthentication(options =>
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
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;
    options.LoginPath = "/bff/login";
    options.LogoutPath = "/bff/logout";
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
})
.AddOpenIdConnect(OpenIdConnectScheme, options =>
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
});

builder.Services.AddReverseProxy()
    .LoadFromMemory(
        GatewayRouteConfiguration.CreateRoutes(),
        GatewayRouteConfiguration.CreateClusters(
            configuredGatewayOptions.ApiMyAnimesBaseUrl,
            configuredGatewayOptions.ApiIdentityBaseUrl));

var app = builder.Build();

app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/api/catalog"))
    {
        context.Request.Headers.Remove("Authorization");
        context.Request.Headers.Remove("Cookie");
    }
    else if (context.Request.Path.StartsWithSegments("/identity/connect/authorize")
        || context.Request.Path.StartsWithSegments("/identity/connect/logout"))
    {
        context.Request.Headers.Cookie = RemoveGatewayCookies(context.Request.Headers.Cookie);
    }

    await next(context);
});
app.UseAuthentication();
app.UseAuthorization();

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
        new AuthenticationProperties { RedirectUri = redirect });
}).AllowAnonymous();

app.MapGet("/bff/antiforgery", (HttpContext context, IAntiforgery antiforgery) =>
{
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Ok(new { token = tokens.RequestToken });
}).AllowAnonymous();

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
}).RequireAuthorization();

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

static string BuildPublicUrl(GatewayOptions options, string path) =>
    options.PublicOrigin.TrimEnd('/') + "/" + path.TrimStart('/');

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
