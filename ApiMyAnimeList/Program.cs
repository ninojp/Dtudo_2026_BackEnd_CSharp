using ApiMyAnimeList.Configuration;
using ApiMyAnimeList.Infrastructure;
using ApiMyAnimeList.Services;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using LibDtudo.Shared.Security;
using Serilog;
using Serilog.Events;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);

var configuredServiceAuthentication = builder.Configuration
    .GetSection(ServiceTokenIssuerOptions.SectionName)
    .Get<ServiceTokenIssuerOptions>() ?? new ServiceTokenIssuerOptions();
builder.WebHost.ConfigureKestrel(options =>
{
    options.ConfigureHttpsDefaults(https =>
    {
        if (!configuredServiceAuthentication.Enabled)
        {
            return;
        }

        https.ClientCertificateMode = ClientCertificateMode.AllowCertificate;
        https.ClientCertificateValidation = (certificate, _, _) =>
            certificate is not null
            && configuredServiceAuthentication.Clients.Any(binding =>
                new ServiceCertificateValidator().Validate(
                    certificate,
                    binding.ClientId,
                    binding,
                    DateTimeOffset.UtcNow).Succeeded);
    });
});

builder.Host.UseSerilog((context, services, loggerConfiguration) =>
{
    loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.With<SensitiveDataRedactionEnricher>();

    var seqUrl = context.Configuration["Seq:Url"];
    if (Uri.TryCreate(seqUrl, UriKind.Absolute, out var seqUri)
        && seqUri.Scheme is "http" or "https")
    {
        loggerConfiguration.WriteTo.Seq(seqUri.ToString());
    }
});

builder.Services.AddControllers();

builder.Services.AddOptions<ServiceTokenIssuerOptions>()
    .Bind(builder.Configuration.GetSection(ServiceTokenIssuerOptions.SectionName))
    .Validate(
        options => !options.Enabled
            || (options.Clients.Length > 0
                && options.Clients.All(client =>
                    !string.IsNullOrWhiteSpace(client.ClientId)
                    && client.CertificateThumbprints.Length is >= 1 and <= 2
                    && client.CertificateThumbprints.All(value =>
                        value.Length == 40 && value.All(char.IsAsciiHexDigit))
                    && client.AllowedScopes.Length > 0
                    && client.AllowedScopes.Distinct(StringComparer.Ordinal).Count() == client.AllowedScopes.Length
                    && client.AllowedAudiences.Length > 0
                    && client.AllowedAudiences.All(value => Uri.TryCreate(value, UriKind.Absolute, out _))
                    && client.AllowedAudiences.Distinct(StringComparer.Ordinal).Count() == client.AllowedAudiences.Length
                    && client.CertificateThumbprints.Distinct(StringComparer.OrdinalIgnoreCase).Count() == client.CertificateThumbprints.Length)
                && options.Clients.Select(client => client.ClientId)
                    .Distinct(StringComparer.Ordinal).Count() == options.Clients.Length
                && options.Clients.SelectMany(client => client.CertificateThumbprints)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    == options.Clients.SelectMany(client => client.CertificateThumbprints).Count()),
        "ServiceAuthentication deve conter bindings de client ID, certificados, escopos e audiences validos quando habilitado.")
    .ValidateOnStart();
builder.Services.AddSingleton<ServiceCertificateValidator>();

builder.Services.AddOptions<ApiAuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(ApiAuthorizationOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer)
        && issuer.Scheme == Uri.UriSchemeHttps, "Authentication:Issuer deve ser uma URL HTTPS absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience), "Authentication:Audience nao configurado.")
    .ValidateOnStart();

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var authorizationOptions = builder.Configuration
        .GetSection(ApiAuthorizationOptions.SectionName)
        .Get<ApiAuthorizationOptions>() ?? new ApiAuthorizationOptions();

    options.Authority = authorizationOptions.Issuer;
    options.Audience = authorizationOptions.Audience;
    options.RequireHttpsMetadata = true;
    options.MapInboundClaims = false;
    options.SaveToken = false;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authorizationOptions.Issuer,
        ValidateAudience = true,
        ValidAudience = authorizationOptions.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };
});

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:catalog.read", "catalog.read", "catalog.read");
    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:catalog.write", "catalog.write", "catalog.write");
    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:catalog.delete", "catalog.delete", "catalog.delete");
    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:health.read", "health.read", "health.read");
    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:service.mal.read", "service.mal.read", "service.mal.read");
});

builder.Services.AddOpenApi();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddOptions<MyAnimeListOptions>()
    .Bind(builder.Configuration.GetSection(MyAnimeListOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps, "MyAnimeList:BaseUrl deve ser uma URL HTTPS absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ClientId), "MyAnimeList:ClientId não configurado.")
    .Validate(options => options.TimeoutSeconds is >= 1 and <= 300, "MyAnimeList:TimeoutSeconds deve estar entre 1 e 300.")
    .Validate(options => options.MaxRetries is >= 0 and <= 10, "MyAnimeList:MaxRetries deve estar entre 0 e 10.")
    .Validate(options => options.CacheMinutes is >= 1 and <= 1440, "MyAnimeList:CacheMinutes deve estar entre 1 e 1440.")
    .ValidateOnStart();

builder.Services.AddHttpClient<MyAnimeListClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<MyAnimeListOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(options.TimeoutSeconds);
    client.DefaultRequestHeaders.Add("X-MAL-CLIENT-ID", options.ClientId);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Dtudo-ApiMyAnimeList/1.0");
}).AddHttpMessageHandler<CorrelationIdDelegatingHandler>();

var dtudoSiteOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options => options.AddPolicy("AllowFrontend", policy =>
    policy.WithOrigins(dtudoSiteOrigins).AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().RequireAuthorization("permission:health.read");
}

app.UseMiddleware<RequestCorrelationMiddleware>();
app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (httpContext, _, exception) =>
        exception is not null || httpContext.Response.StatusCode >= 500
            ? LogEventLevel.Error
            : httpContext.Response.StatusCode >= 400
                ? LogEventLevel.Warning
                : LogEventLevel.Information;
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
    {
        diagnosticContext.Set("RequestMethod", httpContext.Request.Method);
        diagnosticContext.Set("RequestPath", httpContext.Request.Path.Value ?? "/");
        diagnosticContext.Set("ResponseStatusCode", httpContext.Response.StatusCode);
    };
});

app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseMiddleware<ServiceClientCertificateMiddleware>();
app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program;

internal sealed class ApiAuthorizationOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}

internal static class ApiAuthorizationPolicies
{
    public static void AddPermissionPolicy(
        AuthorizationOptions options,
        string policyName,
        string permission,
        string scope)
    {
        options.AddPolicy(policyName, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                HasPermission(context.User, permission) && HasScope(context.User, scope));
        });
    }

    private static bool HasPermission(ClaimsPrincipal principal, string permission)
        => principal.Claims.Any(claim =>
            claim.Type == "permission"
            && string.Equals(claim.Value, permission, StringComparison.Ordinal));

    private static bool HasScope(ClaimsPrincipal principal, string scope)
        => principal.Claims
            .Where(claim => claim.Type is "scope" or "scp")
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
}
