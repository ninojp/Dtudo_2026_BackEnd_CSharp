using ApiMyAnimes.Data;
using ApiMyAnimes.Configuration;
using ApiMyAnimes.Infrastructure;
using ApiMyAnimes.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore;
using LibDtudo.Shared.Logging;
using LibDtudo.Shared.Security;
using Microsoft.IdentityModel.Tokens;
using Serilog;
using Serilog.Events;
using System.Reflection;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

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

builder.Services.AddOptions<DatabaseOptions>()
    .Bind(builder.Configuration.GetSection(DatabaseOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.LocalDbConnection), "ConnectionStrings:LocalDbConnection nao configurada.")
    .ValidateOnStart();

// Add services to the container.
//===============================
// Configuração do Entity.Framework.Core para MyAnimeContext usando SQL Server
builder.Services.AddDbContext<MyAnimesContext>((serviceProvider, options) =>
    options.UseSqlServer(serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value.LocalDbConnection));

builder.Services.AddControllers().AddNewtonsoftJson();

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
    ApiAuthorizationPolicies.AddPermissionPolicy(options, "permission:identity.self.read", "identity.self.read", "identity.self.read");
});

builder.Services.AddMemoryCache();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddScoped<AnimeBuscaLocalService>();
builder.Services.AddScoped<AnimeTitleConflictService>();
builder.Services.AddScoped<ISecurityAuditWriter, SecurityAuditWriter>();
builder.Services.AddSingleton<LocalAuthService>();
builder.Services.AddOptions<AuthOptions>()
    .Bind(builder.Configuration.GetSection(AuthOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.UsersFilePath), "Auth:UsersFilePath nao configurado.")
    .ValidateOnStart();

builder.Services.AddOptions<ApiMyAnimeListOptions>()
    .Bind(builder.Configuration.GetSection(ApiMyAnimeListOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out var uri)
        && uri.Scheme == Uri.UriSchemeHttps, "ApiMyAnimeList:BaseUrl deve ser uma URL HTTPS absoluta.")
    .ValidateOnStart();

builder.Services.AddOptions<ServiceClientCredentialsOptions>()
    .Bind(builder.Configuration.GetSection(ServiceClientCredentialsOptions.SectionName))
    .Validate(
        options => !options.Enabled
            || (Uri.TryCreate(options.TokenEndpoint, UriKind.Absolute, out var endpoint)
                && endpoint.Scheme == Uri.UriSchemeHttps
                && !string.IsNullOrWhiteSpace(options.ClientId)
                && Uri.TryCreate(options.Audience, UriKind.Absolute, out _)
                && options.Scopes.Length > 0
                && options.CertificateThumbprints.Length is >= 1 and <= 2
                && options.CertificateThumbprints.All(value =>
                    value.Length == 40 && value.All(char.IsAsciiHexDigit))),
        "ServiceAuthentication:ApiMyAnimeList deve conter endpoint HTTPS, client ID, audience, escopos e thumbprints validos quando habilitado.")
    .ValidateOnStart();

builder.Services.AddSingleton<ServiceCertificateValidator>();
builder.Services.AddSingleton<ServiceCertificateStore>();
builder.Services.AddSingleton(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ServiceClientCredentialsOptions>>().Value;
    return new ServiceAccessTokenProvider(
        options,
        serviceProvider.GetRequiredService<ServiceCertificateStore>(),
        TimeProvider.System);
});
builder.Services.AddTransient<ServiceAccessTokenHandler>();
builder.Services.AddHttpClient<MyAnimeListImportClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<ApiMyAnimeListOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(30);
})
    .ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    {
        var options = serviceProvider.GetRequiredService<IOptions<ServiceClientCredentialsOptions>>().Value;
        if (!options.Enabled)
        {
            return new HttpClientHandler();
        }

        var certificate = serviceProvider.GetRequiredService<ServiceCertificateStore>()
            .LoadClientCertificate(
                options.CertificateStore,
                options.ToBinding(),
                DateTimeOffset.UtcNow);
        if (certificate is null)
        {
            throw new InvalidOperationException("Certificado de cliente do servico nao encontrado no Certificate Store.");
        }

        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual
        };
        handler.ClientCertificates.Add(certificate);
        return handler;
    })
    .AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
    .AddHttpMessageHandler<ServiceAccessTokenHandler>();

builder.Services.AddEndpointsApiExplorer();

// Configuração do Swagger para documentação da API
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Api Local MyAnimes",
        Version = "v1",
        Description = "Esta é uma Api Local que manipula (CRUD completo) um Banco de dados Relacional local que contém informações relacionadas as minhas coleções de animes. MyAnime (DBtabela) representa coleções nomeadas que agrupam APENAS os IDs dos animes, e Anime (DBtabela) contém informações detalhadas sobre cada anime."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    c.IncludeXmlComments(xmlPath);
});
//=======================================================================
// Configuração de CORS para permitir acesso apenas do frontend DtudoSite
var dtudoSiteOrigins = builder.Configuration
    .GetSection("Cors:AllowedOrigins")
    .Get<string[]>() ?? ["http://localhost:5173"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(dtudoSiteOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
//=======================================================================
var app = builder.Build();

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

if (app.Environment.IsDevelopment())
{
    app.UseWhen(
        context => context.Request.Path.StartsWithSegments("/swagger"),
        branch => branch.Use(async (context, next) =>
        {
            if (!(context.User.Identity?.IsAuthenticated ?? false))
            {
                await context.ChallengeAsync();
                return;
            }

            if (!ApiAuthorizationPolicies.HasPermissionAndScope(context.User, "health.read", "health.read"))
            {
                await context.ForbidAsync();
                return;
            }

            await next(context);
        }));

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api Local MyAnimes v1");
        options.RoutePrefix = "swagger";
    });
}

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
            policy.RequireAssertion(context => HasPermissionAndScope(context.User, permission, scope));
        });
    }

    public static bool HasPermissionAndScope(ClaimsPrincipal principal, string permission, string scope)
        => HasPermission(principal, permission) && HasScope(principal, scope);

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
