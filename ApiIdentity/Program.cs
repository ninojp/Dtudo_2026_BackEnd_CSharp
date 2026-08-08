using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Identity;
using ApiIdentity.Models;
using ApiIdentity.Authorization;
using ApiIdentity.Mfa;
using ApiIdentity.Provisioning;
using ApiIdentity.Privacy;
using ApiIdentity;
using LibDtudo.Shared.Security;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using Fido2NetLib;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.Server.Kestrel.Https;
using System.Security.Claims;
using System.Text;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
var browserCookieScheme = IdentityConstants.ApplicationScheme;

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
    .Validate(options => !string.IsNullOrWhiteSpace(options.WinApp.ClientId),
        "OpenIddict:WinApp:ClientId nao configurado.")
    .Validate(options => WinAppOpenIddictOptions.IsValidLoopbackRedirectUri(options.WinApp.RedirectUri),
        "OpenIddict:WinApp:RedirectUri deve ser um callback HTTP fixo em 127.0.0.1 sem query ou fragmento.")
    .Validate(options => options.WinApp.Scopes is { Length: > 0 }
        && options.WinApp.Scopes.Distinct(StringComparer.Ordinal).Count() == options.WinApp.Scopes.Length,
        "OpenIddict:WinApp:Scopes deve conter escopos exclusivos.")
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
    options.Lockout.AllowedForNewUsers = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<IdentityDbContext>()
    .AddUserStore<ProtectedIdentityUserStore>()
    .AddSignInManager()
    .AddDefaultTokenProviders();
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme;
})
    .AddCookie(browserCookieScheme, options =>
    {
        options.Cookie.Name = "__Host-DtudoIdentity";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.LoginPath = "/account/login";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    });
builder.Services.AddAntiforgery();
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
builder.Services.AddScoped<IdentityPrivacyService>();
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
builder.Services.AddScoped<OpenIddictAuthorizationPrincipalFactory>();
builder.Services.AddScoped<OpenIddictConfigurationSeeder>();
builder.Services.AddScoped<IdentityAdministrationService>();
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
        options.SetRevocationEndpointUris("/connect/revocation");
        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();
        options.AllowClientCredentialsFlow();
        options.RequireProofKeyForCodeExchange();
        options.DisableAccessTokenEncryption();
        options.RegisterScopes(
            OpenIddictConstants.Scopes.Profile,
            "identity.login",
            "identity.provision");
        options.RegisterResources(
            "urn:dtudo:api-my-animes",
            "urn:dtudo:api-my-animelist",
            "urn:dtudo:api-file-storage");
        options.AddDevelopmentEncryptionCertificate();
        options.AddDevelopmentSigningCertificate();
        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddHealthChecks()
    .AddCheck("self", () => HealthCheckResult.Healthy(), tags: ["live"])
    .AddDbContextCheck<IdentityDbContext>("identity-db", tags: ["ready"]);

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    await scope.ServiceProvider
        .GetRequiredService<OpenIddictConfigurationSeeder>()
        .SeedAsync();
}

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
app.Use(async (context, next) =>
{
    var protectedPath = context.Request.Path.StartsWithSegments("/identity/security")
        || context.Request.Path.StartsWithSegments("/identity/admin")
        || context.Request.Path.StartsWithSegments("/identity/me");
    var bindingExempt = context.Request.Method == HttpMethods.Post
        && (context.Request.Path.Equals("/identity/security/sessions", StringComparison.OrdinalIgnoreCase)
            || IsSessionTokenBindingPath(context.Request.Path));
    var accessToken = GetBearerToken(context);
    if (protectedPath
        && !bindingExempt
        && accessToken is not null
        && context.User.Identity?.IsAuthenticated == true)
    {
        var tokenService = context.RequestServices.GetRequiredService<SecurityTokenService>();
        var tokenInfo = await tokenService.IntrospectAccessTokenAsync(accessToken, context.RequestAborted);
        if (tokenInfo is null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }
    }

    await next();
});
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/account/login", async (
    HttpContext context,
    IAntiforgery antiforgery) =>
{
    var returnUrl = GetSafeReturnUrl(context.Request.Query["returnUrl"]);
    var tokens = antiforgery.GetAndStoreTokens(context);
    return Results.Content(
        BuildLoginPage(returnUrl, tokens.RequestToken),
        "text/html; charset=utf-8");
});

app.MapPost("/account/login", async (
    HttpContext context,
    IAntiforgery antiforgery,
    UserManager<IdentityAccount> userManager,
    SignInManager<IdentityAccount> signInManager,
    [FromForm] string? userName,
    [FromForm] string? password,
    [FromForm] string? returnUrl) =>
{
    await antiforgery.ValidateRequestAsync(context);
    var safeReturnUrl = GetSafeReturnUrl(returnUrl);
    var normalizedUserName = userName?.Trim() ?? string.Empty;
    var account = await userManager.FindByNameAsync(normalizedUserName)
        ?? await userManager.FindByEmailAsync(normalizedUserName);
    if (account is not null)
    {
        var result = await signInManager.CheckPasswordSignInAsync(
            account,
            password ?? string.Empty,
            lockoutOnFailure: true);
        if (result.Succeeded)
        {
            await signInManager.SignInAsync(account, isPersistent: false);
            return Results.Redirect(safeReturnUrl);
        }
    }

    return Results.Content(
        BuildLoginPage(safeReturnUrl, antiforgery.GetAndStoreTokens(context).RequestToken, "Credenciais invalidas ou conta indisponivel."),
        "text/html; charset=utf-8",
        Encoding.UTF8,
        StatusCodes.Status401Unauthorized);
});

app.MapMethods("/connect/authorize", [HttpMethods.Get, HttpMethods.Post], async (
    HttpContext context,
    OpenIddictAuthorizationPrincipalFactory principalFactory,
    CancellationToken cancellationToken) =>
{
    var request = Microsoft.AspNetCore.OpenIddictServerAspNetCoreHelpers
        .GetOpenIddictServerRequest(context);
    if (request is null)
    {
        return Results.BadRequest();
    }

    var browserAuthentication = await context.AuthenticateAsync(browserCookieScheme);
    if (!browserAuthentication.Succeeded || browserAuthentication.Principal is null)
    {
        var returnUrl = context.Request.PathBase + context.Request.Path + context.Request.QueryString;
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = returnUrl },
            [browserCookieScheme]);
    }

    var principal = await principalFactory.CreateAsync(
        browserAuthentication.Principal,
        request,
        cancellationToken);
    if (principal is null)
    {
        await context.SignOutAsync(browserCookieScheme);
        return Results.Challenge(
            new AuthenticationProperties
            {
                RedirectUri = context.Request.PathBase + context.Request.Path + context.Request.QueryString
            },
            [browserCookieScheme]);
    }

    return Results.SignIn(
        principal,
        authenticationScheme: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme);
});

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

    var result = await service.ProvisionAsync(request, cancellationToken: cancellationToken);
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

    return await service.RevokeInitialSecretAsync(activationId, cancellationToken: cancellationToken)
        ? Results.NoContent()
        : Results.NotFound();
});

if (app.Environment.IsDevelopment())
{
    localProvisioning.MapPost("/development/reset-password", async (
        DevelopmentPasswordResetRequest request,
        HttpContext context,
        LocalProvisioningRequestGuard guard,
        UserManager<IdentityAccount> userManager,
        TimeProvider timeProvider,
        CancellationToken cancellationToken) =>
    {
        if (!guard.IsAuthorized(context))
        {
            return Results.NotFound();
        }

        var account = await userManager.FindByNameAsync(request.UserName.Trim());
        if (account is null)
        {
            return Results.NotFound();
        }

        if (account.PasswordHash is not null)
        {
            var removePassword = await userManager.RemovePasswordAsync(account);
            if (!removePassword.Succeeded)
            {
                return Results.BadRequest();
            }
        }

        var addPassword = await userManager.AddPasswordAsync(account, request.Password);
        if (!addPassword.Succeeded)
        {
            return Results.BadRequest(new { error = "invalid_password" });
        }

        account.IsActivationCompleted = true;
        account.ActivatedAtUtc = timeProvider.GetUtcNow();
        account.LockoutEnd = null;
        account.AccessFailedCount = 0;

        await userManager.SetLockoutEndDateAsync(account, null);
        await userManager.ResetAccessFailedCountAsync(account);
        await userManager.UpdateSecurityStampAsync(account);
        var update = await userManager.UpdateAsync(account);

        return update.Succeeded ? Results.NoContent() : Results.BadRequest();
    });
}

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

var identityProvisionPolicy = AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.IdentityProvision);
var security = app.MapGroup("/identity/security")
    .RequireAuthorization();

security.MapPost("/sessions", async (
    SecuritySessionCreateRequest request,
    HttpContext httpContext,
    SecuritySessionService service,
    SecurityTokenService tokenService,
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
    if (result is null)
    {
        return Results.BadRequest();
    }

    var accessToken = GetBearerToken(httpContext);
    var accessTokenExpiresAtUtc = GetAccessTokenExpiry(httpContext.User);
    if (accessToken is null
        || accessTokenExpiresAtUtc is not { } expiresAtUtc
        || !await tokenService.BindAccessTokenAsync(
            accountId,
            accessToken,
            result.SessionId,
            result.DeviceId,
            expiresAtUtc,
            accountId,
            cancellationToken))
    {
        await service.RevokeSessionAsync(accountId, result.SessionId, accountId, cancellationToken);
        return Results.Unauthorized();
    }

    return Results.Ok(result);
});

security.MapPost("/sessions/{sessionId:guid}/token", async (
    Guid sessionId,
    SecuritySessionTokenBindingRequest request,
    HttpContext httpContext,
    SecurityTokenService tokenService,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    var accessToken = GetBearerToken(httpContext);
    var accessTokenExpiresAtUtc = GetAccessTokenExpiry(httpContext.User);
    if (accountId is null
        || accessToken is null
        || accessTokenExpiresAtUtc is not { } expiresAtUtc
        || !await tokenService.BindAccessTokenAsync(
            accountId,
            accessToken,
            sessionId,
            request.DeviceId,
            expiresAtUtc,
            accountId,
            cancellationToken))
    {
        return Results.Unauthorized();
    }

    return Results.NoContent();
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

var administration = app.MapGroup("/identity/admin")
    .RequireAuthorization(identityProvisionPolicy);

administration.MapGet("/accounts", async (
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    if (!await service.IsActiveSessionAsync(
        httpContext.User,
        new SecurityContext(sessionId, deviceId),
        cancellationToken))
    {
        return Results.Forbid();
    }

    return Results.Ok(await service.GetAccountsAsync(cancellationToken));
});

administration.MapGet("/roles", async (
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    if (!await service.IsActiveSessionAsync(
        httpContext.User,
        new SecurityContext(sessionId, deviceId),
        cancellationToken))
    {
        return Results.Forbid();
    }

    return Results.Ok(service.GetRoles());
});

administration.MapGet("/permissions", async (
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    if (!await service.IsActiveSessionAsync(
        httpContext.User,
        new SecurityContext(sessionId, deviceId),
        cancellationToken))
    {
        return Results.Forbid();
    }

    return Results.Ok(service.GetPermissions());
});

administration.MapGet("/devices", async (
    bool includeRevoked,
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    if (!await service.IsActiveSessionAsync(
        httpContext.User,
        new SecurityContext(sessionId, deviceId),
        cancellationToken))
    {
        return Results.Forbid();
    }

    return Results.Ok(await service.GetDevicesAsync(includeRevoked, cancellationToken));
});

administration.MapGet("/sessions", async (
    bool includeRevoked,
    string? sessionId,
    string? deviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    if (!await service.IsActiveSessionAsync(
        httpContext.User,
        new SecurityContext(sessionId, deviceId),
        cancellationToken))
    {
        return Results.Forbid();
    }

    return Results.Ok(await service.GetSessionsAsync(includeRevoked, cancellationToken));
});

administration.MapPost("/accounts", async (
    IdentityAdminProvisionRequest request,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.ProvisionAsync(httpContext.User, request, cancellationToken);
    return result.Succeeded ? Results.Ok(result) : Results.BadRequest();
});

administration.MapPost("/accounts/{accountId}/roles", async (
    string accountId,
    IdentityAdminRoleAssignmentRequest request,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
    await service.AssignRoleAsync(httpContext.User, accountId, request, cancellationToken)
        ? Results.NoContent()
        : Results.BadRequest());

administration.MapPost("/accounts/{accountId}/lock", async (
    string accountId,
    IdentityAdminLockRequest request,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
    await service.SetLockAsync(httpContext.User, accountId, request, cancellationToken)
        ? Results.NoContent()
        : Results.BadRequest());

administration.MapDelete("/sessions/{sessionId:guid}", async (
    Guid sessionId,
    string? requestSessionId,
    string? requestDeviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
    await service.RevokeSessionAsync(
        httpContext.User,
        sessionId,
        requestSessionId,
        requestDeviceId,
        cancellationToken)
        ? Results.NoContent()
        : Results.NotFound());

administration.MapDelete("/devices/{deviceId:guid}", async (
    Guid deviceId,
    string? requestSessionId,
    string? requestDeviceId,
    HttpContext httpContext,
    IdentityAdministrationService service,
    CancellationToken cancellationToken) =>
    await service.RevokeDeviceAsync(
        httpContext.User,
        deviceId,
        requestSessionId,
        requestDeviceId,
        cancellationToken)
        ? Results.NoContent()
        : Results.NotFound());

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

var personal = app.MapGroup("/identity/me")
    .RequireAuthorization();
var personalReadPolicy = AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.PersonalRead);
var personalWritePolicy = AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.PersonalWrite);
var privacyExportPolicy = AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.PrivacyExport);
var privacyDeletePolicy = AuthorizationCatalog.PolicyName(AuthorizationCatalog.Permissions.PrivacyDelete);

personal.MapGet("/age-confirmation", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.GetAdultAgeConfirmationAsync(accountId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(personalReadPolicy);

personal.MapPost("/age-confirmation", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.ConfirmAdultAgeAsync(accountId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapGet("/terms/{documentType}/current", async (
    string documentType,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var result = await service.GetActiveTermsAsync(documentType, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(personalReadPolicy);

personal.MapPost("/terms/{termsDocumentId:guid}/accept", async (
    Guid termsDocumentId,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.AcceptTermsAsync(accountId, termsDocumentId, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapGet("/favorites", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(await service.GetFavoritesAsync(accountId, cancellationToken));
}).RequireAuthorization(personalReadPolicy);

personal.MapPost("/favorites", async (
    PersonalResourceRequest request,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.AddFavoriteAsync(accountId, request, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapDelete("/favorites/{favoriteId:guid}", async (
    Guid favoriteId,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RemoveFavoriteAsync(accountId, favoriteId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
}).RequireAuthorization(personalWritePolicy);

personal.MapGet("/preferences", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(await service.GetPreferencesAsync(accountId, cancellationToken));
}).RequireAuthorization(personalReadPolicy);

personal.MapPut("/preferences", async (
    PersonalPreferenceRequest request,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.SetPreferenceAsync(accountId, request, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapDelete("/preferences/{key}", async (
    string key,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RemovePreferenceAsync(accountId, key, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
}).RequireAuthorization(personalWritePolicy);

personal.MapGet("/lists", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : Results.Ok(await service.GetListsAsync(accountId, cancellationToken));
}).RequireAuthorization(personalReadPolicy);

personal.MapPost("/lists", async (
    PersonalListRequest request,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.CreateListAsync(accountId, request, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapDelete("/lists/{listId:guid}", async (
    Guid listId,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RemoveListAsync(accountId, listId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
}).RequireAuthorization(personalWritePolicy);

personal.MapPost("/lists/{listId:guid}/items", async (
    Guid listId,
    PersonalListItemRequest request,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.AddListItemAsync(accountId, listId, request, cancellationToken);
    return result is null ? Results.BadRequest() : Results.Ok(result);
}).RequireAuthorization(personalWritePolicy);

personal.MapDelete("/lists/{listId:guid}/items/{listItemId:guid}", async (
    Guid listId,
    Guid listItemId,
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    return accountId is null
        ? Results.Unauthorized()
        : await service.RemoveListItemAsync(accountId, listId, listItemId, cancellationToken)
            ? Results.NoContent()
            : Results.NotFound();
}).RequireAuthorization(personalWritePolicy);

personal.MapPost("/data-export", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.ExportAsync(accountId, cancellationToken);
    return result is null ? Results.NotFound() : Results.Ok(result);
}).RequireAuthorization(privacyExportPolicy);

personal.MapPost("/deletion-request", async (
    HttpContext httpContext,
    IdentityPrivacyService service,
    CancellationToken cancellationToken) =>
{
    var accountId = GetPrincipalAccountId(httpContext.User);
    if (accountId is null)
    {
        return Results.Unauthorized();
    }

    var result = await service.RequestDeletionAsync(accountId, cancellationToken);
    return result is null ? Results.Conflict() : Results.Ok(result);
}).RequireAuthorization(privacyDeletePolicy);

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

static string? GetBearerToken(HttpContext context)
{
    var authorization = context.Request.Headers.Authorization.ToString();
    const string prefix = "Bearer ";
    return authorization.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
        ? authorization[prefix.Length..].Trim()
        : null;
}

    static bool IsSessionTokenBindingPath(PathString path)
    {
        var segments = path.Value?.Split('/', StringSplitOptions.RemoveEmptyEntries);
        return segments is { Length: 5 }
        && string.Equals(segments[0], "identity", StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[1], "security", StringComparison.OrdinalIgnoreCase)
        && string.Equals(segments[2], "sessions", StringComparison.OrdinalIgnoreCase)
        && Guid.TryParse(segments[3], out _)
        && string.Equals(segments[4], "token", StringComparison.OrdinalIgnoreCase);
    }

static DateTimeOffset? GetAccessTokenExpiry(ClaimsPrincipal principal)
{
    var expiration = principal.FindFirst("exp")?.Value
        ?? principal.FindFirst(ClaimTypes.Expiration)?.Value;
    return long.TryParse(expiration, out var seconds)
        ? DateTimeOffset.FromUnixTimeSeconds(seconds)
        : DateTimeOffset.TryParse(expiration, out var parsed) ? parsed : null;
}

static string GetSafeReturnUrl(string? returnUrl)
{
    if (string.IsNullOrWhiteSpace(returnUrl)
        || !Uri.TryCreate(returnUrl, UriKind.Relative, out var uri)
        || !returnUrl.StartsWith("/", StringComparison.Ordinal)
        || returnUrl.StartsWith("//", StringComparison.Ordinal)
        || uri.IsAbsoluteUri)
    {
        return "/";
    }

    return returnUrl;
}

static string BuildLoginPage(string returnUrl, string? requestVerificationToken, string? error = null)
{
    var encodedReturnUrl = WebUtility.HtmlEncode(returnUrl);
    var encodedToken = WebUtility.HtmlEncode(requestVerificationToken ?? string.Empty);
    var encodedError = string.IsNullOrWhiteSpace(error)
        ? string.Empty
        : $"<p role=\"alert\">{WebUtility.HtmlEncode(error)}</p>";
    return "<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><title>Entrar no Dtudo</title>"
        + "<meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head><body>"
        + "<main><h1>Entrar no Dtudo</h1>"
        + encodedError
        + "<form method=\"post\" action=\"/account/login\">"
        + $"<input type=\"hidden\" name=\"returnUrl\" value=\"{encodedReturnUrl}\">"
        + $"<input type=\"hidden\" name=\"__RequestVerificationToken\" value=\"{encodedToken}\">"
        + "<label>Usuario ou email<input name=\"userName\" autocomplete=\"username\" required></label>"
        + "<label>Senha<input type=\"password\" name=\"password\" autocomplete=\"current-password\" required></label>"
        + "<button type=\"submit\">Entrar</button></form></main></body></html>";
}

    public sealed record DevelopmentPasswordResetRequest(string UserName, string Password);

public partial class Program;
