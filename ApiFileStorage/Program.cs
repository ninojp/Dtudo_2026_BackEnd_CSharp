using System.Reflection;
using System.Security.Claims;
using ApiFileStorage.Configuration;
using ApiFileStorage.Infrastructure;
using ApiFileStorage.Services;
using LibDtudo.Shared.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Tokens;
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

builder.Services.AddOptions<FileStorageOptions>()
    .Bind(builder.Configuration.GetSection(FileStorageOptions.SectionName))
    .Validate(options => options.Roots is { Length: > 0 }, "FileStorage:Roots deve conter pelo menos uma raiz.")
    .Validate(options => options.AllowedFileTypes is { Length: > 0 }, "FileStorage:AllowedFileTypes deve conter pelo menos um tipo permitido.")
    .Validate(options => options.Limits is not null
        && options.Limits.MaxFileSizeBytes > 0
        && options.Limits.MaxFileSizeBytes <= int.MaxValue
        && options.Limits.MaxFileNameLength is > 0 and <= 255
        && options.Limits.MinimumFreeSpaceBytes >= 0
        && options.Limits.MaxIdempotencyKeyLength > 0
        && options.Limits.ScannerTimeoutSeconds > 0
        && options.Limits.MaxBulkDeleteItems is > 0 and <= 500
        && options.Limits.DeletePreviewLifetimeSeconds is >= 30 and <= 900,
        "FileStorage:Limits possui valores invalidos.")
    .Validate(options => options.Scanner is not null, "FileStorage:Scanner deve ser configurado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ExportRootId)
        && !string.IsNullOrWhiteSpace(options.ExportPathPrefix),
        "FileStorage:ExportRootId e ExportPathPrefix sao obrigatorios.")
    .Validate(options => options.StepUp is not null
        && Uri.TryCreate(options.StepUp.IdentityBaseUrl, UriKind.Absolute, out var identityUri)
        && identityUri.Scheme == Uri.UriSchemeHttps
        && !string.IsNullOrWhiteSpace(options.StepUp.Action)
        && options.StepUp.TimeoutSeconds is >= 1 and <= 60,
        "FileStorage:StepUp deve apontar para uma Identity HTTPS valida.")
    .ValidateOnStart();

builder.Services.AddSingleton<StorageRootCatalog>();
builder.Services.AddSingleton<TimeProvider>(TimeProvider.System);
builder.Services.AddSingleton<IStoragePathResolver, SecureStoragePathResolver>();
builder.Services.AddSingleton<IStorageSpaceChecker, StorageSpaceChecker>();
builder.Services.AddSingleton<IFileScanner, CompositeFileScanner>();
builder.Services.AddSingleton<IFileStorageLifecycleService, FileStorageLifecycleService>();
builder.Services.AddSingleton<IFileStorageCommandService, FileStorageCommandService>();
builder.Services.AddSingleton<FileStorageDeletePreviewStore>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient<IFileStorageStepUpValidator, IdentityFileStorageStepUpValidator>((services, client) =>
{
    var stepUpOptions = services.GetRequiredService<Microsoft.Extensions.Options.IOptions<FileStorageOptions>>().Value.StepUp;
    client.BaseAddress = new Uri(stepUpOptions.IdentityBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
    client.Timeout = TimeSpan.FromSeconds(stepUpOptions.TimeoutSeconds);
});
builder.Services.AddHostedService<FileStorageRootValidationHostedService>();
builder.Services.AddHostedService<FileStorageReconciliationHostedService>();

var configuredMaxFileSize = builder.Configuration.GetValue<long?>("FileStorage:Limits:MaxFileSizeBytes")
    ?? 50 * 1024 * 1024;
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = configuredMaxFileSize > 0 && configuredMaxFileSize <= long.MaxValue - (1024 * 1024)
        ? configuredMaxFileSize + (1024 * 1024)
        : configuredMaxFileSize;
});

builder.Services.AddControllers();

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

    AuthorizationPolicyFactory.AddPermissionPolicy(options, "permission:filesystem.command", "filesystem.command", "filesystem.command");
    AuthorizationPolicyFactory.AddPermissionPolicy(options, "permission:health.read", "health.read", "health.read");
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "Api File Storage",
        Version = "v1",
        Description = "Servico interno para resolucao segura de IDs logicos de arquivos."
    });
    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    options.IncludeXmlComments(xmlPath);
});

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

            if (!AuthorizationPolicyFactory.HasPermissionAndScope(context.User, "health.read", "health.read"))
            {
                await context.ForbidAsync();
                return;
            }

            await next(context);
        }));

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Api File Storage v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program;

internal sealed class FileStorageRootValidationHostedService(StorageRootCatalog rootCatalog) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = rootCatalog;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

internal sealed class ApiAuthorizationOptions
{
    public const string SectionName = "Authentication";

    public string Issuer { get; set; } = string.Empty;

    public string Audience { get; set; } = string.Empty;
}

internal static class AuthorizationPolicyFactory
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

