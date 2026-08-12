using System.Reflection;
using System.Security.Claims;
using ApiDiscogs.Configuration;
using ApiDiscogs.Infrastructure;
using ApiDiscogs.Services;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;

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

builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DiscogsExceptionHandler>();
builder.Services.AddScoped<IDiscogsService, DiscogsService>();
builder.Services.AddTransient<CorrelationIdDelegatingHandler>();
builder.Services.AddTransient<DiscogsAuthenticationHandler>();
builder.Services.AddTransient<DiscogsEgressHandler>();

builder.Services.AddOptions<DiscogsOptions>()
    .Bind(builder.Configuration.GetSection(DiscogsOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.Token)
        && options.Token.Length <= 512,
        "ApiDiscogs:Token deve ser fornecido por user-secrets ou ambiente seguro e ter no maximo 512 caracteres.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.UserAgent)
        && options.UserAgent.Length <= 128
        && !options.UserAgent.Contains('\r')
        && !options.UserAgent.Contains('\n'),
        "ApiDiscogs:UserAgent deve ser informado sem quebras de linha e ter no maximo 128 caracteres.")
    .Validate(DiscogsEgressHandler.IsValidBaseUrl,
        "ApiDiscogs:BaseUrl deve ser HTTPS em api.discogs.com na porta 443.")
    .Validate(DiscogsEgressHandler.IsValidAllowedHosts,
        "ApiDiscogs:AllowedHosts deve conter somente api.discogs.com.")
    .Validate(DiscogsEgressHandler.IsValidPathPrefix,
        "ApiDiscogs:AllowedPathPrefix deve ser um prefixo absoluto terminado em / e compatível com BaseUrl.")
    .Validate(options => options.TimeoutSeconds is >= 1 and <= 300,
        "ApiDiscogs:TimeoutSeconds deve estar entre 1 e 300.")
    .Validate(options => options.MaxRetries is >= 0 and <= 10,
        "ApiDiscogs:MaxRetries deve estar entre 0 e 10.")
    .Validate(options => options.RetryDelayMilliseconds is >= 1 and <= 5000,
        "ApiDiscogs:RetryDelayMilliseconds deve estar entre 1 e 5000.")
    .Validate(options => options.CacheMinutes is >= 1 and <= 1440,
        "ApiDiscogs:CacheMinutes deve estar entre 1 e 1440.")
    .Validate(options => options.MaxResponseBytes is >= 1024 and <= 20_000_000,
        "ApiDiscogs:MaxResponseBytes deve estar entre 1024 e 20000000.")
    .Validate(options => options.TotalTimeoutSeconds is >= 1 and <= 900
        && options.TotalTimeoutSeconds >= options.TimeoutSeconds,
        "ApiDiscogs:TotalTimeoutSeconds deve ser maior ou igual ao timeout por tentativa e estar entre 1 e 900.")
    .Validate(options => options.CircuitBreakerFailureRatio is > 0 and <= 1,
        "ApiDiscogs:CircuitBreakerFailureRatio deve estar entre 0 e 1.")
    .Validate(options => options.CircuitBreakerMinimumThroughput is >= 2 and <= 1000,
        "ApiDiscogs:CircuitBreakerMinimumThroughput deve estar entre 2 e 1000.")
    .Validate(options => options.CircuitBreakerSamplingSeconds is >= 1 and <= 3600,
        "ApiDiscogs:CircuitBreakerSamplingSeconds deve estar entre 1 e 3600.")
    .Validate(options => options.CircuitBreakerBreakSeconds is >= 1 and <= 3600,
        "ApiDiscogs:CircuitBreakerBreakSeconds deve estar entre 1 e 3600.")
    .ValidateOnStart();

builder.Services.AddOptions<ApiAuthorizationOptions>()
    .Bind(builder.Configuration.GetSection(ApiAuthorizationOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer)
        && issuer.Scheme == Uri.UriSchemeHttps,
        "Authentication:Issuer deve ser uma URL HTTPS absoluta.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.Audience),
        "Authentication:Audience nao configurado.")
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

    ApiAuthorizationPolicies.AddPermissionPolicy(
        options,
        ApiAuthorizationPolicies.HealthReadPolicy,
        "health.read",
        "health.read");
    ApiAuthorizationPolicies.AddExternalCatalogReadPolicy(options);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "ApiDiscogs",
        Version = "v1",
        Description = "API local de leitura que encapsula a API externa Discogs sem persistência da Colecao."
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Informe um token JWT no formato Bearer {token}.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

builder.Services.AddOptions<ApiCorsOptions>()
    .Bind(builder.Configuration.GetSection(ApiCorsOptions.SectionName))
    .Validate(options => options.AllowedOrigins is { Length: > 0 }
        && options.AllowedOrigins.Distinct(StringComparer.OrdinalIgnoreCase).Count() == options.AllowedOrigins.Length
        && options.AllowedOrigins.All(origin =>
            ApiCorsOptions.IsValidOrigin(origin, !builder.Environment.IsDevelopment())),
        "Cors:AllowedOrigins deve conter origins HTTP(S) unicos; em producao, use somente HTTPS e nunca o wildcard.")
    .ValidateOnStart();

var configuredCorsOrigins = builder.Configuration
    .GetSection(ApiCorsOptions.SectionName)
    .Get<ApiCorsOptions>()?.AllowedOrigins ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowConfiguredOrigins", policy =>
    {
        policy.WithOrigins(configuredCorsOrigins)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});

builder.Services.AddHttpClient<DiscogsClient>((serviceProvider, client) =>
{
    var options = serviceProvider.GetRequiredService<IOptions<DiscogsOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl, UriKind.Absolute);
    client.Timeout = Timeout.InfiniteTimeSpan;
    client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
})
.ConfigurePrimaryHttpMessageHandler(serviceProvider =>
    DiscogsEgressHandler.CreatePrimaryHandler(
        serviceProvider.GetRequiredService<IOptions<DiscogsOptions>>().Value))
.AddHttpMessageHandler<CorrelationIdDelegatingHandler>()
.AddHttpMessageHandler<DiscogsAuthenticationHandler>()
.AddHttpMessageHandler<DiscogsEgressHandler>()
.AddResilienceHandler("discogs", (pipeline, context) =>
    DiscogsResilience.Configure(
        pipeline,
        context.ServiceProvider.GetRequiredService<IOptions<DiscogsOptions>>().Value));

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
app.UseExceptionHandler();

app.UseHttpsRedirection();
app.UseCors("AllowConfiguredOrigins");
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
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "ApiDiscogs v1");
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
    public const string HealthReadPolicy = "permission:health.read";
    public const string ExternalCatalogReadPolicy = "permission:catalog.external.read";

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
                HasPermissionAndScope(context.User, permission, scope));
        });
    }

    public static void AddExternalCatalogReadPolicy(AuthorizationOptions options)
    {
        options.AddPolicy(ExternalCatalogReadPolicy, policy =>
        {
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context =>
                HasPermissionAndScope(context.User, "catalog.read", "catalog.read"));
        });
    }

    public static bool HasPermissionAndScope(
        ClaimsPrincipal principal,
        string permission,
        string scope)
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
