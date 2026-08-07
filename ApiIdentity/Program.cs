using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Identity;
using ApiIdentity.Models;
using ApiIdentity.Authorization;
using ApiIdentity.Mfa;
using ApiIdentity.Provisioning;
using ApiIdentity;
using LibDtudo.Shared.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using Fido2NetLib;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Claims;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

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

builder.Services.AddOptions<LocalProvisioningOptions>()
    .Bind(builder.Configuration.GetSection(LocalProvisioningOptions.SectionName))
    .Validate(
        options => LocalProvisioningOptions.HasValidAdministrationSecret(options.AdministrationSecret),
        "LocalProvisioning:AdministrationSecret deve ser Base64Url com pelo menos 32 bytes aleatorios e ficar fora do repositorio.")
    .Validate(
        options => options.InitialSecretLifetimeMinutes is >= 5 and <= 1_440,
        "LocalProvisioning:InitialSecretLifetimeMinutes deve estar entre 5 e 1440 minutos.")
    .ValidateOnStart();

builder.Services.AddOptions<IdentityMfaOptions>()
    .Bind(builder.Configuration.GetSection(IdentityMfaOptions.SectionName))
    .Validate(
        options => !string.IsNullOrWhiteSpace(options.RelyingPartyDomain)
            && !string.IsNullOrWhiteSpace(options.RelyingPartyName),
        "IdentityMfa:RelyingPartyDomain e IdentityMfa:RelyingPartyName sao obrigatorios.")
    .Validate(
        options => options.Origins is { Length: > 0 }
            && options.Origins.All(origin => Uri.TryCreate(origin, UriKind.Absolute, out var uri)
                && uri.Scheme == Uri.UriSchemeHttps),
        "IdentityMfa:Origins deve conter pelo menos uma origem HTTPS absoluta.")
    .Validate(
        options => options.Fido2TimeoutMilliseconds is >= 10_000 and <= 120_000
            && options.Fido2TimestampDriftMilliseconds is >= 0 and <= 120_000
            && options.ChallengeLifetimeSeconds is >= 30 and <= 900
            && options.StepUpLifetimeSeconds is >= 30 and <= 900
            && options.LocalRecoveryLifetimeMinutes is >= 5 and <= 60
            && options.SnapshotLifetimeHours is >= 1 and <= 168
            && options.RecoveryCodeCount is >= 5 and <= 20
            && options.ClockSkewSeconds is >= 0 and <= 120,
        "Os limites de IdentityMfa estao fora do intervalo permitido.")
    .ValidateOnStart();

builder.Services.AddOptions<IdentitySessionOptions>()
    .Bind(builder.Configuration.GetSection(IdentitySessionOptions.SectionName))
    .Validate(
        options => options.LifetimeDays is >= 1 and <= 30
            && options.AccessTokenLifetimeSeconds is >= 60 and <= 900
            && options.RefreshTokenLifetimeDays is >= 1
            && options.RefreshTokenLifetimeDays <= options.LifetimeDays
            && options.TokenEntropyBytes is >= 32 and <= 64,
        "Os limites de IdentitySessions estao fora do intervalo permitido.")
    .ValidateOnStart();

builder.Services.AddOptions<ServiceTokenIssuerOptions>()
    .Bind(builder.Configuration.GetSection(ServiceTokenIssuerOptions.SectionName))
    .Validate(
        options => !options.Enabled
            || (options.AccessTokenLifetimeSeconds is >= 60 and <= 900
                && options.Clients.Length > 0
                && options.Clients.All(client =>
                    !string.IsNullOrWhiteSpace(client.ClientId)
                    && client.CertificateThumbprints.Length is >= 1 and <= 2
                    && client.CertificateThumbprints.All(IsThumbprint)
                    && client.AllowedScopes.Length > 0
                    && client.AllowedScopes.Distinct(StringComparer.Ordinal).Count() == client.AllowedScopes.Length
                    && client.AllowedAudiences.Length > 0
                    && client.AllowedAudiences.All(IsAbsoluteAudience)
                    && client.AllowedAudiences.Distinct(StringComparer.Ordinal).Count() == client.AllowedAudiences.Length
                    && client.CertificateThumbprints.Distinct(StringComparer.OrdinalIgnoreCase).Count() == client.CertificateThumbprints.Length)
                && options.Clients.Select(client => client.ClientId)
                    .Distinct(StringComparer.Ordinal).Count() == options.Clients.Length
                && options.Clients.SelectMany(client => client.CertificateThumbprints)
                    .Distinct(StringComparer.OrdinalIgnoreCase).Count()
                    == options.Clients.SelectMany(client => client.CertificateThumbprints).Count()),
        "ServiceAuthentication deve conter clientes, thumbprints, escopos e audiences exclusivos validos quando habilitado.")
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
    options.Stores.SchemaVersion = IdentitySchemaVersions.Version3;
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredUniqueChars = 4;
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddUserStore<ProtectedIdentityUserStore>()
    .AddDefaultTokenProviders();
builder.Services.AddSingleton<IFido2>(serviceProvider =>
{
    var options = serviceProvider.GetRequiredService<IOptions<IdentityMfaOptions>>().Value;
    return new Fido2(new Fido2Configuration
    {
        ServerDomain = options.RelyingPartyDomain,
        ServerName = options.RelyingPartyName,
        Origins = options.Origins.ToHashSet(StringComparer.Ordinal),
        Timeout = (uint)options.Fido2TimeoutMilliseconds,
        TimestampDriftTolerance = options.Fido2TimestampDriftMilliseconds,
        ChallengeSize = 32
    });
});
builder.Services.AddAuthorization(options =>
{
    foreach (var permission in AuthorizationCatalog.AllPermissions)
    {
        options.AddPolicy(AuthorizationCatalog.PolicyName(permission.Key), policy => policy
            .RequireAuthenticatedUser()
            .RequireClaim(AuthorizationCatalog.PermissionClaimType, permission.Key));
    }
});
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IdentityProvisioningAuditWriter>();
builder.Services.AddScoped<AccountProvisioningService>();
builder.Services.AddScoped<IdentitySecurityAuditWriter>();
builder.Services.AddScoped<SecuritySessionService>();
builder.Services.AddScoped<SecurityTokenService>();
builder.Services.AddScoped<IdentitySecurityChallengeService>();
builder.Services.AddScoped<StepUpService>();
builder.Services.AddScoped<TotpMfaService>();
builder.Services.AddScoped<PasskeyMfaService>();
builder.Services.AddScoped<LocalRecoveryService>();
builder.Services.AddScoped<SecuritySnapshotService>();
builder.Services.AddSingleton<ServiceCertificateValidator>();
builder.Services.AddSingleton<ServiceTokenRequestValidator>();
builder.Services.AddSingleton<ServiceCertificateStore>();
builder.Services.AddSingleton<ServiceTokenEndpoint>();
builder.Services.AddSingleton<LocalProvisioningRequestGuard>();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("local-provisioning", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("initial-account-activation", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
    options.AddPolicy("local-recovery", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
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
        options.AllowClientCredentialsFlow();
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();
        options.UseAspNetCore();
    });

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: ["ready"]);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseRateLimiter();
app.Use(async (context, next) =>
{
    if (string.Equals(context.Request.Path.Value, "/connect/token", StringComparison.OrdinalIgnoreCase))
    {
        var serviceTokenEndpoint = context.RequestServices.GetRequiredService<ServiceTokenEndpoint>();
        if (await serviceTokenEndpoint.TryHandleAsync(context))
        {
            return;
        }
    }

    await next();
});
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

var localProvisioning = app.MapGroup("/internal/identity")
    .RequireRateLimiting("local-provisioning");

localProvisioning.MapPost("/bootstrap", async (
    BootstrapAccountRequest request,
    HttpContext context,
    LocalProvisioningRequestGuard guard,
    AccountProvisioningService service,
    CancellationToken cancellationToken) =>
{
    if (!guard.IsAuthorized(context))
    {
        return Results.NotFound();
    }

    var result = await service.BootstrapAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Delivery) : Results.Conflict();
});

localProvisioning.MapPost("/accounts", async (
    ProvisionAccountRequest request,
    HttpContext context,
    LocalProvisioningRequestGuard guard,
    AccountProvisioningService service,
    CancellationToken cancellationToken) =>
{
    if (!guard.IsAuthorized(context))
    {
        return Results.NotFound();
    }

    var result = await service.ProvisionAsync(request, cancellationToken);
    return result.Succeeded ? Results.Ok(result.Delivery) : Results.BadRequest();
});

localProvisioning.MapPost("/activation-secrets/{activationId:guid}/revoke", async (
    Guid activationId,
    HttpContext context,
    LocalProvisioningRequestGuard guard,
    AccountProvisioningService service,
    CancellationToken cancellationToken) =>
{
    if (!guard.IsAuthorized(context))
    {
        return Results.NotFound();
    }

    return await service.RevokeInitialSecretAsync(activationId, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
});

app.MapPost("/identity/activate-initial-account", async (
    InitialAccountActivationRequest request,
    AccountProvisioningService service,
    CancellationToken cancellationToken) =>
    Results.Ok(await service.ActivateAsync(request, cancellationToken)))
    .RequireRateLimiting("initial-account-activation");

app.MapPost("/identity/security/tokens/refresh", async (
    SecurityTokenRefreshRequest request,
    SecurityTokenService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RefreshAsync(
        request.RefreshToken,
        "refresh-client",
        cancellationToken);
    return result.Status == SecurityTokenRefreshStatus.Succeeded
        ? Results.Ok(result.Tokens)
        : Results.Unauthorized();
});

var security = app.MapGroup("/identity/security")
    .RequireAuthorization();

security.MapPost("/sessions", async (
    SecuritySessionCreateRequest request,
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.CreateAsync(
        accountId,
        request.Name,
        accountId,
        cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/sessions/tokens", async (
    SecuritySessionCreateRequest request,
    HttpContext httpContext,
    SecurityTokenService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.IssueAsync(
        accountId,
        request.Name,
        accountId,
        cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapGet("/devices", async (
    bool includeRevoked,
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(await service.GetDevicesAsync(accountId, includeRevoked, cancellationToken));
});

security.MapGet("/sessions", async (
    bool includeRevoked,
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(await service.GetSessionsAsync(accountId, includeRevoked, cancellationToken));
});

security.MapDelete("/sessions/{sessionId:guid}", async (
    Guid sessionId,
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RevokeSessionAsync(accountId, sessionId, accountId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
});

security.MapDelete("/devices/{deviceId:guid}", async (
    Guid deviceId,
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RevokeDeviceAsync(accountId, deviceId, accountId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
});

security.MapPost("/sessions/revoke-all", async (
    HttpContext httpContext,
    SecuritySessionService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(new { Revoked = await service.RevokeAllAsync(accountId, accountId, cancellationToken) });
});

security.MapPost("/totp/setup", async (
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.BeginSetupAsync(httpContext.User, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/totp/confirm", async (
    TotpSetupConfirmationRequest request,
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ConfirmSetupAsync(httpContext.User, request.Token, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/totp/step-up", async (
    StepUpVerificationRequest request,
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.VerifyAndGrantAsync(
        httpContext.User,
        request.Token,
        request.Action,
        new SecurityContext(request.SessionId, request.DeviceId),
        cancellationToken);
    return result is null ? Results.Forbid() : Results.Ok(result);
});

security.MapPost("/recovery-code/step-up", async (
    StepUpVerificationRequest request,
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RedeemRecoveryCodeAndGrantAsync(
        httpContext.User,
        request.Token,
        request.Action,
        new SecurityContext(request.SessionId, request.DeviceId),
        cancellationToken);
    return result is null ? Results.Forbid() : Results.Ok(result);
});

security.MapPost("/recovery-codes/regenerate", async (
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GenerateRecoveryCodesAsync(httpContext.User, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/totp/disable", async (
    HttpContext httpContext,
    TotpMfaService service,
    CancellationToken cancellationToken) =>
    await service.DisableAsync(httpContext.User, cancellationToken)
        ? Results.NoContent()
        : Results.BadRequest());

security.MapPost("/passkeys/registration/options", async (
    SecurityContextRequest request,
    HttpContext httpContext,
    PasskeyMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.BeginRegistrationAsync(
        httpContext.User,
        passkeyName: null,
        request.ToContext(),
        cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/passkeys/registration/complete", async (
    PasskeyRegistrationRequest request,
    HttpContext httpContext,
    PasskeyMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CompleteRegistrationAsync(
        httpContext.User,
        request.ChallengeId,
        request.Response,
        request.Name,
        new SecurityContext(request.SessionId, request.DeviceId),
        cancellationToken);
    return result ? Results.NoContent() : Results.BadRequest();
});

security.MapPost("/passkeys/authentication/options", async (
    SecurityContextRequest request,
    HttpContext httpContext,
    PasskeyMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.BeginAuthenticationAsync(
        httpContext.User,
        request.ToContext(),
        cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
});

security.MapPost("/passkeys/authentication/complete", async (
    PasskeyAuthenticationRequest request,
    HttpContext httpContext,
    PasskeyMfaService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CompleteAuthenticationAndGrantAsync(
        httpContext.User,
        request.ChallengeId,
        request.Response,
        request.Action,
        new SecurityContext(request.SessionId, request.DeviceId),
        cancellationToken);
    return result is null ? Results.Forbid() : Results.Ok(result);
});

security.MapDelete("/passkeys/{credentialId}", async (
    string credentialId,
    HttpContext httpContext,
    PasskeyMfaService service,
    CancellationToken cancellationToken) =>
{
    byte[] decoded;
    try
    {
        decoded = WebEncoders.Base64UrlDecode(credentialId);
    }
    catch (FormatException)
    {
        return Results.BadRequest();
    }

    return await service.RemoveAsync(httpContext.User, decoded, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
});

security.MapGet("/step-up/{action}", async (
    string action,
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    StepUpService service,
    CancellationToken cancellationToken) =>
    await service.IsAllowedAsync(
        httpContext.User,
        action,
        new SecurityContext(sessionId, deviceId),
        cancellationToken)
        ? Results.NoContent()
        : Results.Forbid());

security.MapPost("/step-up/revoke-all", async (
    HttpContext httpContext,
    StepUpService service,
    CancellationToken cancellationToken) =>
    Results.Ok(new { Revoked = await service.RevokeAllAsync(httpContext.User, cancellationToken) }));

security.MapDelete("/step-up/{grantId:guid}", async (
    Guid grantId,
    HttpContext httpContext,
    StepUpService service,
    CancellationToken cancellationToken) =>
    await service.RevokeAsync(httpContext.User, grantId, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound());

security.MapPost("/accounts/{accountId}/snapshots", async (
    string accountId,
    SecurityContextRequest request,
    HttpContext httpContext,
    SecuritySnapshotService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.CreateAsync(
        httpContext.User,
        accountId,
        request.ToContext(),
        cancellationToken);
    return result is null ? Results.Forbid() : Results.Ok(result);
});

security.MapPost("/accounts/{accountId}/snapshots/restore", async (
    string accountId,
    SecuritySnapshotRestoreRequest request,
    HttpContext httpContext,
    SecuritySnapshotService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.RestoreAsync(
        httpContext.User,
        accountId,
        request.SnapshotId,
        new SecurityContext(request.SessionId, request.DeviceId),
        cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.Forbid();
});

security.MapPost("/accounts/{accountId}/snapshots/{snapshotId:guid}/revoke", async (
    string accountId,
    Guid snapshotId,
    SecurityContextRequest request,
    HttpContext httpContext,
    SecuritySnapshotService service,
    CancellationToken cancellationToken) =>
    await service.RevokeAsync(
        httpContext.User,
        accountId,
        snapshotId,
        request.ToContext(),
        cancellationToken)
        ? Results.NoContent()
        : Results.Forbid());

var localRecovery = app.MapGroup("/internal/identity/security")
    .RequireAuthorization(AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.IdentityProvision))
    .RequireRateLimiting("local-recovery");

localRecovery.MapPost("/tokens/introspect", async (
    SecurityTokenIntrospectionRequest request,
    SecurityTokenService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.IntrospectAccessTokenAsync(
        request.AccessToken,
        cancellationToken);
    return result is null
        ? Results.Unauthorized()
        : Results.Ok(new
        {
            Active = true,
            result.AccountId,
            result.DeviceId,
            result.SessionId,
            result.ExpiresAtUtc
        });
});

localRecovery.MapPost("/recovery-tickets", async (
    LocalRecoveryIssueRequest request,
    HttpContext httpContext,
    LocalRecoveryService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.IssueAsync(httpContext.User, request.AccountId, cancellationToken);
    return result is null ? Results.Forbid() : Results.Ok(result);
});

localRecovery.MapPost("/recovery-tickets/{ticketId:guid}/revoke", async (
    Guid ticketId,
    HttpContext httpContext,
    LocalRecoveryService service,
    CancellationToken cancellationToken) =>
    await service.RevokeAsync(httpContext.User, ticketId, cancellationToken)
        ? Results.NoContent()
        : Results.NotFound());

app.MapPost("/identity/recover/local", async (
    LocalRecoveryRedeemRequest request,
    LocalRecoveryService service,
    CancellationToken cancellationToken) =>
    await service.RedeemAsync(request, cancellationToken)
        ? Results.NoContent()
        : Results.BadRequest())
    .RequireRateLimiting("local-recovery");

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

static bool IsThumbprint(string value) =>
    value.Length == 40
        && value.All(character => char.IsAsciiHexDigit(character));

static bool IsAbsoluteAudience(string value) =>
    Uri.TryCreate(value, UriKind.Absolute, out _);

static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
    principal.Identity?.IsAuthenticated == true
        ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
        : null;

public partial class Program;
