using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using ApiIdentity.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

var builder = WebApplication.CreateBuilder(args);

if (!builder.Environment.IsDevelopment() || !OperatingSystem.IsWindows())
{
    throw new InvalidOperationException(
    "ApiIdentity requer certificados OpenIddict e protecao de chaves configurados fora de Development ou Windows.");
}

builder.Services.AddOptions<IdentityDatabaseOptions>()
    .Bind(builder.Configuration.GetSection(IdentityDatabaseOptions.SectionName))
    .Configure(options => options.ConnectionString = builder.Configuration.GetConnectionString("IdentityDb") ?? string.Empty)
    .Validate(options => !string.IsNullOrWhiteSpace(options.DatabaseName), "IdentityDatabase:DatabaseName nao configurado.")
    .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "ConnectionStrings:IdentityDb nao configurada.")
    .Validate(UsesConfiguredDatabase, "ConnectionStrings:IdentityDb deve apontar para IdentityDatabase:DatabaseName.")
    .ValidateOnStart();

builder.Services.AddOptions<OpenIddictServerConfigurationOptions>()
    .Bind(builder.Configuration.GetSection(OpenIddictServerConfigurationOptions.SectionName))
    .Validate(options => Uri.TryCreate(options.Issuer, UriKind.Absolute, out var issuer)
        && issuer.Scheme == Uri.UriSchemeHttps,
        "OpenIddict:Issuer deve ser uma URL HTTPS absoluta.")
    .ValidateOnStart();

var dataProtectionKeyDirectory = new DirectoryInfo(Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
    "Dtudo2026",
    "ApiIdentity",
    "DataProtectionKeys"));

builder.Services.AddDataProtection()
    .SetApplicationName("Dtudo2026.ApiIdentity")
    .PersistKeysToFileSystem(dataProtectionKeyDirectory)
    .ProtectKeysWithDpapi(protectToLocalMachine: false);

builder.Services.AddDbContext<IdentityDbContext>((serviceProvider, options) =>
{
    var databaseOptions = serviceProvider.GetRequiredService<IOptions<IdentityDatabaseOptions>>().Value;
    options.UseSqlServer(databaseOptions.ConnectionString, sqlServerOptions =>
        sqlServerOptions.MigrationsAssembly(typeof(Program).Assembly.FullName));
});

builder.Services.AddIdentityCore<IdentityAccount>(options =>
{
    options.User.RequireUniqueEmail = true;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>();
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AuthorizationCatalog.AllPermissions)
    {
        options.AddPolicy(AuthorizationCatalog.PolicyName(permission.Key), policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(AuthorizationCatalog.PermissionClaimType, permission.Key));
    }
});

builder.Services.AddOpenIddict()
    .AddCore(options => options.UseEntityFrameworkCore().UseDbContext<IdentityDbContext>())
    .AddServer(options =>
    {
        var openIddictOptions = builder.Configuration
            .GetSection(OpenIddictServerConfigurationOptions.SectionName)
            .Get<OpenIddictServerConfigurationOptions>()!;

        options.SetIssuer(new Uri(openIddictOptions.Issuer, UriKind.Absolute));
        options.SetConfigurationEndpointUris("/.well-known/openid-configuration");
        options.SetJsonWebKeySetEndpointUris("/.well-known/jwks");
    options.SetAuthorizationEndpointUris("/connect/authorize");
    options.SetTokenEndpointUris("/connect/token");
    options.AllowAuthorizationCodeFlow();
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();
        options.UseAspNetCore();
    });

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: ["ready"]);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = healthCheck => healthCheck.Tags.Contains("ready")
});

app.Run();

static bool UsesConfiguredDatabase(IdentityDatabaseOptions options)
{
    try
    {
        var builder = new SqlConnectionStringBuilder(options.ConnectionString);
        return string.Equals(builder.InitialCatalog, options.DatabaseName, StringComparison.Ordinal);
    }
    catch (ArgumentException)
    {
        return false;
    }
}

public partial class Program;
