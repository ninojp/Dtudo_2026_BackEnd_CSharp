using System.Security.Claims;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Identity;
using ApiIdentity.Mfa;
using ApiIdentity.Models;
using Fido2NetLib;
using Fido2NetLib.Objects;
using ApiIdentity.Provisioning;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Tests;

public sealed class IdentitySecurityServiceTests
{
    private const string Password = "Dtudo2026!SecurityPassword";
    private const string RecoveredPassword = "Dtudo2026!RecoveredPassword";
    private static readonly DateTimeOffset InitialTime =
        new(2026, 8, 6, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task ChallengesRequireActiveBindingAndRejectWrongContext()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "challenge-user");
            var context = await CreateSecurityContextAsync(services, accountId, "challenge-device");

            await using var scope = services.CreateAsyncScope();
            var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
            var accountWithoutBinding = await CreateAccountAsync(services, "unbound-user");

            var rejected = await challenges.CreateAsync(
                accountWithoutBinding,
                "test",
                "payload",
                context,
                TimeSpan.FromSeconds(5));
            Assert.Null(rejected);

            var challenge = await challenges.CreateAsync(
                accountId,
                "test",
                "payload",
                context,
                TimeSpan.FromSeconds(5));
            Assert.NotNull(challenge);

            var wrongContext = new SecurityContext(Guid.NewGuid().ToString("D"), context.DeviceId);
            Assert.Null(await challenges.ReadAsync<string>(challenge!.Id, accountId, "test", wrongContext));
            Assert.Equal("payload", await challenges.ReadAsync<string>(
                challenge.Id,
                accountId,
                "test",
                context));

            await using var verificationScope = services.CreateAsyncScope();
            var database = verificationScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var persisted = await database.SecurityChallenges.SingleAsync(item => item.Id == challenge.Id);
            Assert.NotEqual("payload", persisted.ProtectedPayload);
            Assert.Equal(context.SessionId, persisted.SessionId);
            Assert.Equal(context.DeviceId, persisted.DeviceId);
        });
    }

    [Fact]
    public async Task ChallengesExpireConsumeOnceAndRejectConcurrentReplay()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "challenge-replay-user");
            var context = await CreateSecurityContextAsync(services, accountId, "challenge-replay-device");

            Guid challengeId;
            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                var challenge = await challenges.CreateAsync(
                    accountId,
                    "test",
                    "single-use",
                    context,
                    TimeSpan.FromSeconds(5));
                challengeId = Require(challenge).Id;
            }

            var results = await Task.WhenAll(
                ConsumeChallengeAsync(services, challengeId, accountId, context),
                ConsumeChallengeAsync(services, challengeId, accountId, context));

            Assert.Equal(1, results.Count(value => value == "single-use"));

            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                Assert.Null(await challenges.ReadAsync<string>(
                    challengeId,
                    accountId,
                    "test",
                    context));

                var expiringChallenge = await challenges.CreateAsync(
                    accountId,
                    "expiring",
                    "expires",
                    context,
                    TimeSpan.FromSeconds(5));
                Assert.NotNull(expiringChallenge);
                timeProvider.Advance(TimeSpan.FromSeconds(6));
                Assert.Null(await challenges.ReadAsync<string>(
                    expiringChallenge!.Id,
                    accountId,
                    "expiring",
                    context));
            }
        });
    }

    [Fact]
    public async Task RevokingSessionRevokesItsChallengesAndStepUpGrants()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "revocation-user");
            var session = await CreateSessionAsync(services, accountId, "revocation-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
            var principal = CreatePrincipal(
                accountId,
                includeProvisionPermission: true,
                includeCatalogDeletePermission: true);

            Guid challengeId;
            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                var challenge = await challenges.CreateAsync(
                    accountId,
                    "test",
                    "revoked",
                    context,
                    TimeSpan.FromSeconds(5));
                challengeId = Require(challenge).Id;

                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                var grant = await stepUp.GrantAsync(
                    principal,
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    IdentityMfaMethods.Totp,
                    context);
                Assert.NotNull(grant);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.True(await sessions.RevokeSessionAsync(accountId, session.SessionId, accountId));
                Assert.False(await sessions.IsActiveBindingAsync(accountId, context));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.Null(await challenges.ReadAsync<string>(
                    challengeId,
                    accountId,
                    "test",
                    context));
                Assert.False(await stepUp.IsAllowedAsync(
                    principal,
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context));
            }
        });
    }

    [Fact]
    public async Task StepUpUsesExactPermissionMethodBindingExpiryAndClockSkew()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "step-up-user");
            var session = await CreateSessionAsync(services, accountId, "step-up-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
            var principal = CreatePrincipal(
                accountId,
                includeProvisionPermission: true,
                includeCatalogDeletePermission: true);
            var unauthenticated = new ClaimsPrincipal(new ClaimsIdentity());

            await using var scope = services.CreateAsyncScope();
            var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
            var action = AuthorizationCatalog.Permissions.CatalogDelete;

            Assert.False(await stepUp.IsAllowedAsync(principal, action, context));
            Assert.Null(await stepUp.GrantAsync(
                principal,
                "catalog.unknown",
                IdentityMfaMethods.Totp,
                context));
            Assert.Null(await stepUp.GrantAsync(
                principal,
                action,
                "unknown-method",
                context));
            Assert.Null(await stepUp.GrantAsync(
                unauthenticated,
                action,
                IdentityMfaMethods.Totp,
                context));

            var grant = await stepUp.GrantAsync(
                principal,
                action,
                IdentityMfaMethods.Totp,
                context);
            Assert.NotNull(grant);
            Assert.True(await stepUp.IsAllowedAsync(principal, action, context));
            Assert.False(await stepUp.IsAllowedAsync(
                principal,
                AuthorizationCatalog.Permissions.CatalogWrite,
                context));
            Assert.False(await stepUp.IsAllowedAsync(
                principal,
                action,
                new SecurityContext(Guid.NewGuid().ToString("D"), context.DeviceId)));

            timeProvider.Advance(TimeSpan.FromSeconds(6));
            Assert.True(await stepUp.IsAllowedAsync(principal, action, context));
            timeProvider.Advance(TimeSpan.FromSeconds(3));
            Assert.False(await stepUp.IsAllowedAsync(principal, action, context));

            var secondGrant = await stepUp.GrantAsync(
                principal,
                action,
                IdentityMfaMethods.RecoveryCode,
                context);
            Assert.NotNull(secondGrant);
            Assert.True(await stepUp.RevokeAsync(principal, secondGrant!.GrantId));
            Assert.False(await stepUp.IsAllowedAsync(principal, action, context));
        });
    }

    [Fact]
    public async Task SessionsTouchListAndBulkRevokeAllSecurityBindings()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "session-user");
            var otherAccountId = await CreateAccountAsync(services, "other-session-user");
            var first = await CreateSessionAsync(services, accountId, "first-device");
            var second = await CreateSessionAsync(services, accountId, "second-device");
            var firstContext = SecurityContextFactory.FromIds(first.SessionId, first.DeviceId);
            var otherContext = SecurityContextFactory.FromIds(second.SessionId, second.DeviceId);

            await using (var scope = services.CreateAsyncScope())
            {
                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                timeProvider.Advance(TimeSpan.FromMinutes(1));
                Assert.True(await sessions.TouchAsync(accountId, firstContext));
                var devices = await sessions.GetDevicesAsync(accountId, includeRevoked: false);
                var activeSessions = await sessions.GetSessionsAsync(accountId, includeRevoked: false);
                Assert.Equal(2, devices.Count);
                Assert.Equal(2, activeSessions.Count);
                Assert.True(activeSessions.Single(item => item.SessionId == first.SessionId).LastSeenAtUtc
                    > first.CreatedAtUtc);
                Assert.False(await sessions.IsActiveBindingAsync(otherAccountId, firstContext));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                var challenge = await challenges.CreateAsync(
                    accountId,
                    "bulk",
                    "payload",
                    firstContext,
                    TimeSpan.FromSeconds(5));
                Assert.NotNull(challenge);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.True(await sessions.RevokeSessionAsync(accountId, first.SessionId, accountId));
                Assert.False(await sessions.IsActiveBindingAsync(accountId, firstContext));
                var allSessions = await sessions.GetSessionsAsync(accountId, includeRevoked: true);
                Assert.True(allSessions.Single(item => item.SessionId == first.SessionId).IsRevoked);
                Assert.True(await sessions.RevokeDeviceAsync(accountId, second.DeviceId, accountId));
            }

            var third = await CreateSessionAsync(services, accountId, "third-device");
            await using (var scope = services.CreateAsyncScope())
            {
                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                var affected = await sessions.RevokeAllAsync(accountId, accountId);
                Assert.True(affected >= 2);
                Assert.False(await sessions.IsActiveBindingAsync(
                    accountId,
                    SecurityContextFactory.FromIds(third.SessionId, third.DeviceId)));
                Assert.Empty(await sessions.GetDevicesAsync(accountId, includeRevoked: false));
            }
        });
    }

    [Fact]
    public async Task TrustedSessionTokensUseThirtyDayLifetimeShortAccessAndIntrospection()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "token-lifetime-user");
            SecurityTokenPair pair;

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                pair = Require(await tokens.IssueAsync(
                    accountId,
                    "trusted-device",
                    accountId));

                Assert.Equal(InitialTime.AddDays(30), pair.SessionExpiresAtUtc);
                Assert.Equal(InitialTime.AddMinutes(5), pair.AccessTokenExpiresAtUtc);
                Assert.Equal(InitialTime.AddDays(30), pair.RefreshTokenExpiresAtUtc);
                Assert.NotEqual(pair.AccessToken, pair.RefreshToken);

                var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var persistedTokens = await database.SecurityTokens
                    .Where(token => token.SessionId == pair.SessionId)
                    .ToListAsync();
                Assert.Equal(2, persistedTokens.Count);
                Assert.DoesNotContain(
                    persistedTokens,
                    token => token.TokenHash.Contains(pair.AccessToken, StringComparison.Ordinal)
                        || token.TokenHash.Contains(pair.RefreshToken, StringComparison.Ordinal));

                var device = await database.SecurityDevices.SingleAsync(
                    item => item.Id == pair.DeviceId);
                var session = await database.SecuritySessions.SingleAsync(
                    item => item.Id == pair.SessionId);
                Assert.Equal(InitialTime, device.TrustedAtUtc);
                Assert.Equal(pair.SessionExpiresAtUtc, device.TrustedUntilUtc);
                Assert.Equal(pair.SessionExpiresAtUtc, session.ExpiresAtUtc);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                Assert.NotNull(await tokens.IntrospectAccessTokenAsync(pair.AccessToken));
            }

            timeProvider.Advance(TimeSpan.FromMinutes(5));
            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                Assert.Null(await tokens.IntrospectAccessTokenAsync(pair.AccessToken));
            }
        });
    }

    [Fact]
    public async Task SessionAndRefreshExpireAtTheThirtyDayBoundary()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "session-expiry-user");
            SecurityTokenPair pair;
            await using (var scope = services.CreateAsyncScope())
            {
                pair = Require(await scope.ServiceProvider
                    .GetRequiredService<SecurityTokenService>()
                    .IssueAsync(accountId, "expiring-device", accountId));
            }

            timeProvider.Advance(TimeSpan.FromDays(30));
            await using var finalScope = services.CreateAsyncScope();
            var tokens = finalScope.ServiceProvider.GetRequiredService<SecurityTokenService>();
            var sessions = finalScope.ServiceProvider.GetRequiredService<SecuritySessionService>();
            Assert.Null(await tokens.IntrospectAccessTokenAsync(pair.AccessToken));
            Assert.Equal(
                SecurityTokenRefreshStatus.Expired,
                (await tokens.RefreshAsync(pair.RefreshToken, accountId)).Status);
            Assert.False(await sessions.IsActiveBindingAsync(
                accountId,
                SecurityContextFactory.FromIds(pair.SessionId, pair.DeviceId)));
        });
    }

    [Fact]
    public async Task RefreshRotationRejectsReplayAndRevokesFamilyAndSession()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "refresh-replay-user");
            SecurityTokenPair original;
            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                original = Require(await tokens.IssueAsync(
                    accountId,
                    "refresh-device",
                    accountId));
            }

            SecurityTokenPair rotated;
            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                var result = await tokens.RefreshAsync(original.RefreshToken, accountId);
                Assert.Equal(SecurityTokenRefreshStatus.Succeeded, result.Status);
                rotated = Require(result.Tokens);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                Assert.NotNull(await tokens.IntrospectAccessTokenAsync(rotated.AccessToken));
                var replay = await tokens.RefreshAsync(original.RefreshToken, accountId);
                Assert.Equal(SecurityTokenRefreshStatus.ReuseDetected, replay.Status);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                Assert.Null(await tokens.IntrospectAccessTokenAsync(rotated.AccessToken));
                Assert.False(await scope.ServiceProvider
                    .GetRequiredService<SecuritySessionService>()
                    .IsActiveBindingAsync(
                        accountId,
                        SecurityContextFactory.FromIds(original.SessionId, original.DeviceId)));

                var persistedTokens = await database.SecurityTokens
                    .Where(token => token.SessionId == original.SessionId)
                    .ToListAsync();
                Assert.All(persistedTokens, token => Assert.NotNull(token.RevokedAtUtc));
                Assert.Contains(
                    await database.ProvisioningAuditEvents.ToListAsync(),
                    audit => audit.Action == "identity.security.refresh.reuse-detected"
                        && audit.Target == $"session:{original.SessionId:D}");
            }
        });
    }

    [Fact]
    public async Task ConcurrentRefreshRotationAllowsOnlyOneWinner()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "refresh-concurrency-user");
            SecurityTokenPair original;
            await using (var scope = services.CreateAsyncScope())
            {
                original = Require(await scope.ServiceProvider
                    .GetRequiredService<SecurityTokenService>()
                    .IssueAsync(accountId, "concurrent-device", accountId));
            }

            var results = await Task.WhenAll(
                RefreshTokenAsync(services, original.RefreshToken),
                RefreshTokenAsync(services, original.RefreshToken));

            Assert.Equal(1, results.Count(result =>
                result.Status == SecurityTokenRefreshStatus.Succeeded));
            Assert.Equal(1, results.Count(result =>
                result.Status == SecurityTokenRefreshStatus.ReuseDetected));

            await using var finalScope = services.CreateAsyncScope();
            var database = finalScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var familyTokens = await database.SecurityTokens
                .Where(token => token.SessionId == original.SessionId)
                .ToListAsync();
            Assert.All(familyTokens, token => Assert.NotNull(token.RevokedAtUtc));
        });
    }

    [Fact]
    public async Task BlockingDeviceInvalidatesActiveAccessAndPrivilegedOperation()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "blocked-device-user");
            SecurityTokenPair pair;
            var principal = CreatePrincipal(
                accountId,
                includeCatalogDeletePermission: true);
            await using (var scope = services.CreateAsyncScope())
            {
                pair = Require(await scope.ServiceProvider
                    .GetRequiredService<SecurityTokenService>()
                    .IssueAsync(accountId, "blocked-device", accountId));
                var context = SecurityContextFactory.FromIds(pair.SessionId, pair.DeviceId);
                Assert.NotNull(await scope.ServiceProvider
                    .GetRequiredService<StepUpService>()
                    .GrantAsync(
                        principal,
                        AuthorizationCatalog.Permissions.CatalogDelete,
                        IdentityMfaMethods.Totp,
                        context));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.True(await sessions.RevokeDeviceAsync(accountId, pair.DeviceId, accountId));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                var context = SecurityContextFactory.FromIds(pair.SessionId, pair.DeviceId);
                Assert.Null(await tokens.IntrospectAccessTokenAsync(pair.AccessToken));
                Assert.False(await scope.ServiceProvider
                    .GetRequiredService<StepUpService>()
                    .IsAllowedAsync(
                        principal,
                        AuthorizationCatalog.Permissions.CatalogDelete,
                        context));
                var refresh = await tokens.RefreshAsync(pair.RefreshToken, accountId);
                Assert.Equal(SecurityTokenRefreshStatus.Revoked, refresh.Status);
            }
        });
    }

    [Fact]
    public async Task OidcAccessTokenBindingFollowsSessionRevocationWithoutPersistingClearToken()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "oidc-binding-user");
            var session = await CreateSessionAsync(services, accountId, "oidc-browser-device");
            var accessToken = "eyJhbGciOiJSUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJhY2NvdW50In0.signature";
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);

            await using (var scope = services.CreateAsyncScope())
            {
                var tokens = scope.ServiceProvider.GetRequiredService<SecurityTokenService>();
                Assert.True(await tokens.BindAccessTokenAsync(
                    accountId,
                    accessToken,
                    session.SessionId,
                    session.DeviceId,
                    InitialTime.AddMinutes(5),
                    accountId));
                Assert.NotNull(await tokens.IntrospectAccessTokenAsync(accessToken));

                var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var persisted = await database.SecurityTokens
                    .SingleAsync(token => token.SessionId == session.SessionId);
                Assert.DoesNotContain(accessToken, persisted.TokenHash, StringComparison.Ordinal);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                Assert.True(await scope.ServiceProvider
                    .GetRequiredService<SecuritySessionService>()
                    .RevokeSessionAsync(accountId, session.SessionId, accountId));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                Assert.Null(await scope.ServiceProvider
                    .GetRequiredService<SecurityTokenService>()
                    .IntrospectAccessTokenAsync(accessToken));
                Assert.False(await scope.ServiceProvider
                    .GetRequiredService<SecuritySessionService>()
                    .IsActiveBindingAsync(accountId, context));
            }
        });
    }

    [Fact]
    public async Task AdministrationRequiresPermissionStepUpAndActiveBinding()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "administration-user");
            await using (var scope = services.CreateAsyncScope())
            {
                var provisioning = scope.ServiceProvider.GetRequiredService<AccountProvisioningService>();
                var bootstrap = await provisioning.BootstrapAsync(
                    new BootstrapAccountRequest("bootstrap-user", "bootstrap@example.test"));
                Assert.True(bootstrap.Succeeded);
            }

            var session = await CreateSessionAsync(services, accountId, "administration-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
            var request = new IdentityAdminProvisionRequest(
                "provisioned-user",
                "provisioned@example.test",
                AuthorizationCatalog.Roles.SiteUser,
                context.SessionId,
                context.DeviceId);
            var withoutPermission = CreatePrincipal(accountId);
            var withPermission = CreatePrincipal(accountId, includeProvisionPermission: true);

            await using (var scope = services.CreateAsyncScope())
            {
                var administration = scope.ServiceProvider.GetRequiredService<IdentityAdministrationService>();
                Assert.False((await administration.ProvisionAsync(
                    withoutPermission,
                    request)).Succeeded);
                Assert.False((await administration.ProvisionAsync(
                    withPermission,
                    request)).Succeeded);

                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.NotNull(await stepUp.GrantAsync(
                    withPermission,
                    AuthorizationCatalog.Permissions.IdentityProvision,
                    IdentityMfaMethods.Totp,
                    context));
                Assert.True((await administration.ProvisionAsync(
                    withPermission,
                    request)).Succeeded);

                Assert.True(await scope.ServiceProvider
                    .GetRequiredService<SecuritySessionService>()
                    .RevokeSessionAsync(accountId, session.SessionId, accountId));
                Assert.False((await administration.ProvisionAsync(
                    withPermission,
                    request)).Succeeded);
            }
        });
    }

    [Fact]
    public async Task TotpSetupVerificationRecoveryCodeAndResetInvalidateSecurityState()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "totp-user");
            var principal = CreatePrincipal(
                accountId,
                includeProvisionPermission: true,
                includeCatalogDeletePermission: true);
            RecoveryCodesResult recoveryCodes;

            await using (var scope = services.CreateAsyncScope())
            {
                var totp = scope.ServiceProvider.GetRequiredService<TotpMfaService>();
                var setup = Require(await totp.BeginSetupAsync(principal));
                Assert.Null(await totp.ConfirmSetupAsync(principal, "000000"));

                var token = CreateAuthenticatorToken(
                    setup.AuthenticatorKey,
                    TimeProvider.System.GetUtcNow());
                recoveryCodes = Require(await totp.ConfirmSetupAsync(principal, token));
            }

            Assert.Equal(5, recoveryCodes.Codes.Count);
            var session = await CreateSessionAsync(services, accountId, "totp-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);

            await using (var scope = services.CreateAsyncScope())
            {
                var totp = scope.ServiceProvider.GetRequiredService<TotpMfaService>();
                var invalid = await totp.VerifyAndGrantAsync(
                    principal,
                    "000000",
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context);
                Assert.Null(invalid);

                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                var authenticatorKey = await userManager.GetAuthenticatorKeyAsync(account!);
                var token = CreateAuthenticatorToken(
                    Require(authenticatorKey),
                    TimeProvider.System.GetUtcNow());
                var grant = await totp.VerifyAndGrantAsync(
                    principal,
                    token,
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context);
                Assert.NotNull(grant);

                var recoveryGrant = await totp.RedeemRecoveryCodeAndGrantAsync(
                    principal,
                    recoveryCodes.Codes[0],
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context);
                Assert.NotNull(recoveryGrant);
                Assert.Null(await totp.RedeemRecoveryCodeAndGrantAsync(
                    principal,
                    recoveryCodes.Codes[0],
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context));

                var regenerated = Require(await totp.GenerateRecoveryCodesAsync(principal));
                Assert.Equal(5, regenerated.Codes.Count);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.False(await stepUp.IsAllowedAsync(
                    principal,
                    AuthorizationCatalog.Permissions.CatalogDelete,
                    context));
            }

            var newSession = await CreateSessionAsync(services, accountId, "totp-reset-device");
            var newContext = SecurityContextFactory.FromIds(newSession.SessionId, newSession.DeviceId);
            await using (var scope = services.CreateAsyncScope())
            {
                var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
                Assert.NotNull(await challenges.CreateAsync(
                    accountId,
                    "totp-reset",
                    "payload",
                    newContext,
                    TimeSpan.FromSeconds(5)));

                var totp = scope.ServiceProvider.GetRequiredService<TotpMfaService>();
                Assert.True(await totp.DisableAsync(principal));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                Assert.False(account!.TwoFactorEnabled);
                Assert.Equal(0, await userManager.CountRecoveryCodesAsync(account));

                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.False(await sessions.IsActiveBindingAsync(accountId, newContext));
            }
        });
    }

    [Fact]
    public async Task LocalRecoveryIsHashedExpiresRevokesAndRedeemsOnlyOnce()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var actorId = await CreateAccountAsync(services, "recovery-actor");
            var targetId = await CreateAccountAsync(services, "recovery-target");
            var actor = CreatePrincipal(actorId, includeProvisionPermission: true);
            var targetSession = await CreateSessionAsync(services, targetId, "recovery-target-device");

            await using (var scope = services.CreateAsyncScope())
            {
                var recovery = scope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
                var first = Require(await recovery.IssueAsync(actor, targetId));
                var second = Require(await recovery.IssueAsync(actor, targetId));

                await using (var databaseScope = services.CreateAsyncScope())
                {
                    var database = databaseScope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                    var tickets = await database.RecoveryTickets
                        .Where(ticket => ticket.AccountId == targetId)
                        .OrderBy(ticket => ticket.CreatedAtUtc)
                        .ToListAsync();
                    Assert.Equal(2, tickets.Count);
                    var firstTicket = tickets.Single(ticket => ticket.Id == first.TicketId);
                    var secondTicket = tickets.Single(ticket => ticket.Id == second.TicketId);
                    Assert.NotEqual(first.Secret, firstTicket.SecretHash);
                    Assert.DoesNotContain(first.Secret, firstTicket.SecretHash, StringComparison.Ordinal);
                    Assert.NotNull(firstTicket.RevokedAtUtc);
                    Assert.Null(secondTicket.RevokedAtUtc);
                }

                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    first.TicketId,
                    first.Secret,
                    RecoveredPassword)));
                await using (var revokeScope = services.CreateAsyncScope())
                {
                    var revokeService = revokeScope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
                    Assert.True(await revokeService.RevokeAsync(actor, second.TicketId));
                }
                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    second.TicketId,
                    second.Secret,
                    RecoveredPassword)));

                LocalRecoveryTicketDelivery expired;
                await using (var issueScope = services.CreateAsyncScope())
                {
                    var issueService = issueScope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
                    expired = Require(await issueService.IssueAsync(actor, targetId));
                }
                timeProvider.Advance(TimeSpan.FromMinutes(6));
                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    expired.TicketId,
                    expired.Secret,
                    RecoveredPassword)));
            }
        });
    }

    [Fact]
    public async Task LocalRecoveryResetsPasswordFactorsAndSecurityBindings()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var actorId = await CreateAccountAsync(services, "successful-recovery-actor");
            var targetId = await CreateAccountAsync(services, "successful-recovery-target");
            var actor = CreatePrincipal(actorId, includeProvisionPermission: true);
            var targetSession = await CreateSessionAsync(services, targetId, "successful-recovery-device");
            LocalRecoveryTicketDelivery delivery;

            await using (var scope = services.CreateAsyncScope())
            {
                var recovery = scope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
                delivery = Require(await recovery.IssueAsync(actor, targetId));
                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    delivery.TicketId,
                    delivery.Secret,
                    "weak")));
                Assert.True(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    delivery.TicketId,
                    delivery.Secret,
                    RecoveredPassword)));
                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    delivery.TicketId,
                    delivery.Secret,
                    RecoveredPassword)));
                Assert.False(await recovery.RedeemAsync(new LocalRecoveryRedeemRequest(
                    Guid.NewGuid(),
                    "unknown",
                    RecoveredPassword)));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(targetId);
                Assert.True(await userManager.CheckPasswordAsync(account!, RecoveredPassword));
                Assert.False(account!.TwoFactorEnabled);

                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.False(await sessions.IsActiveBindingAsync(
                    targetId,
                    SecurityContextFactory.FromIds(targetSession.SessionId, targetSession.DeviceId)));

                var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var ticket = await database.RecoveryTickets.SingleAsync(item => item.Id == delivery.TicketId);
                Assert.NotNull(ticket.UsedAtUtc);
            }
        });
    }

    [Fact]
    public async Task LocalRecoveryAllowsOnlyOneConcurrentRedemption()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var actorId = await CreateAccountAsync(services, "concurrent-recovery-actor");
            var targetId = await CreateAccountAsync(services, "concurrent-recovery-target");
            var actor = CreatePrincipal(actorId, includeProvisionPermission: true);
            LocalRecoveryTicketDelivery delivery;
            await using (var scope = services.CreateAsyncScope())
            {
                var recovery = scope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
                delivery = Require(await recovery.IssueAsync(actor, targetId));
            }

            var request = new LocalRecoveryRedeemRequest(
                delivery.TicketId,
                delivery.Secret,
                RecoveredPassword);
            var results = await Task.WhenAll(
                RedeemRecoveryAsync(services, request),
                RedeemRecoveryAsync(services, request));

            Assert.Equal(1, results.Count(result => result));
        });
    }

    [Fact]
    public async Task PasskeyCeremoniesAreBoundToProtectedChallengesAndCredentials()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "passkey-user");
            var principal = CreatePrincipal(accountId, includeProvisionPermission: true);
            var session = await CreateSessionAsync(services, accountId, "passkey-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);

            await using (var scope = services.CreateAsyncScope())
            {
                var passkeys = scope.ServiceProvider.GetRequiredService<PasskeyMfaService>();
                var registration = Require(await passkeys.BeginRegistrationAsync(
                    principal,
                    "test-key",
                    context));
                Assert.Equal(IdentitySecurityChallengeKinds.PasskeyRegistration,
                    (await scope.ServiceProvider.GetRequiredService<IdentityDbContext>()
                        .SecurityChallenges.SingleAsync(item => item.Id == registration.ChallengeId)).Kind);

                Assert.Null(await passkeys.BeginRegistrationAsync(
                    principal,
                    "test-key",
                    new SecurityContext(Guid.NewGuid().ToString("D"), context.DeviceId)));
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                var passkey = new UserPasskeyInfo(
                    [1, 2, 3],
                    [4, 5, 6],
                    InitialTime,
                    0,
                    ["internal"],
                    true,
                    true,
                    false,
                    [7, 8],
                    [9, 10])
                {
                    Name = "stored-key"
                };
                var added = await userManager.AddOrUpdatePasskeyAsync(account!, passkey);
                Assert.True(added.Succeeded);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var passkeys = scope.ServiceProvider.GetRequiredService<PasskeyMfaService>();
                Assert.NotNull(await passkeys.BeginAuthenticationAsync(principal, context));
                Assert.False(await passkeys.RemoveAsync(principal, [99]));
                Assert.True(await passkeys.RemoveAsync(principal, [1, 2, 3]));
                Assert.Null(await passkeys.BeginAuthenticationAsync(principal, context));
            }
        });
    }

    [Fact]
    public async Task SnapshotsAreProtectedRestoredOnceAndRevokeCurrentSecurityState()
    {
        await WithTemporaryDatabaseAsync(async (services, _) =>
        {
            var accountId = await CreateAccountAsync(services, "snapshot-user");
            var principal = CreatePrincipal(accountId, includeProvisionPermission: true);
            string originalAuthenticatorKey;

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                Assert.True((await userManager.ResetAuthenticatorKeyAsync(account!)).Succeeded);
                originalAuthenticatorKey = Require(await userManager.GetAuthenticatorKeyAsync(account!));
                Assert.True((await userManager.SetTwoFactorEnabledAsync(account!, true)).Succeeded);
                var passkey = new UserPasskeyInfo(
                    [11, 12, 13],
                    [14, 15, 16],
                    InitialTime,
                    4,
                    ["internal"],
                    true,
                    true,
                    true,
                    [17, 18],
                    [19, 20])
                {
                    Name = "snapshot-key"
                };
                Assert.True((await userManager.AddOrUpdatePasskeyAsync(account!, passkey)).Succeeded);
            }

            var session = await CreateSessionAsync(services, accountId, "snapshot-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
            SecuritySnapshotResult snapshot;

            await using (var scope = services.CreateAsyncScope())
            {
                var snapshots = scope.ServiceProvider.GetRequiredService<SecuritySnapshotService>();
                snapshot = Require(await snapshots.CreateAsync(principal, accountId, context));
                var database = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                var persisted = await database.SecuritySnapshots.SingleAsync(item => item.Id == snapshot.SnapshotId);
                Assert.DoesNotContain(originalAuthenticatorKey, persisted.ProtectedPayload, StringComparison.Ordinal);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                foreach (var passkey in await userManager.GetPasskeysAsync(account!))
                {
                    Assert.True((await userManager.RemovePasskeyAsync(account!, passkey.CredentialId)).Succeeded);
                }
                Assert.True((await userManager.SetTwoFactorEnabledAsync(account!, false)).Succeeded);
                Assert.True((await userManager.ResetAuthenticatorKeyAsync(account!)).Succeeded);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.NotNull(await stepUp.GrantAsync(
                    principal,
                    AuthorizationCatalog.Permissions.IdentityProvision,
                    IdentityMfaMethods.LocalRecovery,
                    context));
                var snapshots = scope.ServiceProvider.GetRequiredService<SecuritySnapshotService>();
                var restored = await snapshots.RestoreAsync(
                    principal,
                    accountId,
                    snapshot.SnapshotId,
                    context);
                Assert.True(restored.Succeeded);
                Assert.Equal(snapshot.SnapshotId, restored.SnapshotId);
                Assert.Equal(5, restored.RecoveryCodes!.Count);
                var replay = await snapshots.RestoreAsync(
                    principal,
                    accountId,
                    snapshot.SnapshotId,
                    context);
                Assert.False(replay.Succeeded);
            }

            await using (var scope = services.CreateAsyncScope())
            {
                var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
                var account = await userManager.FindByIdAsync(accountId);
                Assert.True(account!.TwoFactorEnabled);
                Assert.Equal(originalAuthenticatorKey, await userManager.GetAuthenticatorKeyAsync(account));
                var passkeys = await userManager.GetPasskeysAsync(account);
                Assert.Single(passkeys);
                Assert.Equal("snapshot-key", passkeys[0].Name);

                var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
                Assert.False(await sessions.IsActiveBindingAsync(accountId, context));
            }
        });
    }

    [Fact]
    public async Task SnapshotsRejectExpiryAndRevocation()
    {
        await WithTemporaryDatabaseAsync(async (services, timeProvider) =>
        {
            var accountId = await CreateAccountAsync(services, "snapshot-expiry-user");
            var principal = CreatePrincipal(accountId, includeProvisionPermission: true);
            var session = await CreateSessionAsync(services, accountId, "snapshot-expiry-device");
            var context = SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
            SecuritySnapshotResult expired;

            await using (var scope = services.CreateAsyncScope())
            {
                var snapshots = scope.ServiceProvider.GetRequiredService<SecuritySnapshotService>();
                expired = Require(await snapshots.CreateAsync(principal, accountId, context));
            }
            timeProvider.Advance(TimeSpan.FromHours(2));
            await using (var scope = services.CreateAsyncScope())
            {
                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.NotNull(await stepUp.GrantAsync(
                    principal,
                    AuthorizationCatalog.Permissions.IdentityProvision,
                    IdentityMfaMethods.LocalRecovery,
                    context));
                var snapshots = scope.ServiceProvider.GetRequiredService<SecuritySnapshotService>();
                Assert.False((await snapshots.RestoreAsync(
                    principal,
                    accountId,
                    expired.SnapshotId,
                    context)).Succeeded);
            }

            var activeSession = await CreateSessionAsync(services, accountId, "snapshot-revoke-device");
            var activeContext = SecurityContextFactory.FromIds(activeSession.SessionId, activeSession.DeviceId);
            SecuritySnapshotResult revoked;
            await using (var scope = services.CreateAsyncScope())
            {
                var snapshots = scope.ServiceProvider.GetRequiredService<SecuritySnapshotService>();
                revoked = Require(await snapshots.CreateAsync(principal, accountId, activeContext));
                var stepUp = scope.ServiceProvider.GetRequiredService<StepUpService>();
                Assert.NotNull(await stepUp.GrantAsync(
                    principal,
                    AuthorizationCatalog.Permissions.IdentityProvision,
                    IdentityMfaMethods.LocalRecovery,
                    activeContext));
                Assert.True(await snapshots.RevokeAsync(
                    principal,
                    accountId,
                    revoked.SnapshotId,
                    activeContext));
                Assert.False((await snapshots.RestoreAsync(
                    principal,
                    accountId,
                    revoked.SnapshotId,
                    activeContext)).Succeeded);
            }
        });
    }

    private static async Task<string> CreateAccountAsync(
        IServiceProvider services,
        string userName,
        bool addSuperAdministratorRole = false)
    {
        await using var scope = services.CreateAsyncScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<IdentityAccount>>();
        var account = new IdentityAccount
        {
            UserName = userName,
            Email = $"{userName}@example.test",
            EmailConfirmed = true,
            IsActivationCompleted = true,
            ActivatedAtUtc = scope.ServiceProvider.GetRequiredService<TimeProvider>().GetUtcNow()
        };
        var result = await userManager.CreateAsync(account, Password);
        Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
        if (addSuperAdministratorRole)
        {
            var roleResult = await userManager.AddToRoleAsync(
                account,
                AuthorizationCatalog.Roles.SuperAdministrator);
            Assert.True(roleResult.Succeeded, string.Join("; ", roleResult.Errors.Select(error => error.Description)));
        }

        return account.Id;
    }

    private static async Task<SecurityDeviceSessionResult> CreateSessionAsync(
        IServiceProvider services,
        string accountId,
        string name)
    {
        await using var scope = services.CreateAsyncScope();
        var sessions = scope.ServiceProvider.GetRequiredService<SecuritySessionService>();
        return Require(await sessions.CreateAsync(accountId, name, accountId));
    }

    private static async Task<SecurityContext> CreateSecurityContextAsync(
        IServiceProvider services,
        string accountId,
        string name)
    {
        var session = await CreateSessionAsync(services, accountId, name);
        return SecurityContextFactory.FromIds(session.SessionId, session.DeviceId);
    }

    private static ClaimsPrincipal CreatePrincipal(
        string accountId,
        bool includeProvisionPermission = false,
        bool includeCatalogDeletePermission = false)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, accountId)
        };
        if (includeProvisionPermission)
        {
            claims.Add(new(
                AuthorizationCatalog.PermissionClaimType,
                AuthorizationCatalog.Permissions.IdentityProvision));
        }
        if (includeCatalogDeletePermission)
        {
            claims.Add(new(
                AuthorizationCatalog.PermissionClaimType,
                AuthorizationCatalog.Permissions.CatalogDelete));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, "test"));
    }

    private static T Require<T>(T? value)
        where T : class
    {
        Assert.NotNull(value);
        return value!;
    }

    private static string CreateAuthenticatorToken(
        string base32Key,
        DateTimeOffset utcNow)
    {
        var key = DecodeBase32(base32Key);
        var counter = (ulong)((utcNow.ToUniversalTime().Ticks
            - DateTimeOffset.UnixEpoch.Ticks)
            / TimeSpan.TicksPerSecond
            / 30);
        Span<byte> counterBytes = stackalloc byte[8];
        for (var index = counterBytes.Length - 1; index >= 0; index--)
        {
            counterBytes[index] = (byte)(counter & 0xff);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(key);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0f;
        var binaryCode = ((hash[offset] & 0x7f) << 24)
            | (hash[offset + 1] << 16)
            | (hash[offset + 2] << 8)
            | hash[offset + 3];
        return (binaryCode % 1_000_000).ToString("D6", CultureInfo.InvariantCulture);
    }

    private static byte[] DecodeBase32(string value)
    {
        var bytes = new List<byte>();
        var buffer = 0;
        var bitsInBuffer = 0;
        foreach (var character in value.Trim().ToUpperInvariant())
        {
            if (character == '=')
            {
                break;
            }

            var digit = character switch
            {
                >= 'A' and <= 'Z' => character - 'A',
                >= '2' and <= '7' => character - '2' + 26,
                _ => throw new FormatException("Invalid Base32 authenticator key.")
            };
            buffer = (buffer << 5) | digit;
            bitsInBuffer += 5;
            if (bitsInBuffer < 8)
            {
                continue;
            }

            bitsInBuffer -= 8;
            bytes.Add((byte)(buffer >> bitsInBuffer));
            buffer &= (1 << bitsInBuffer) - 1;
        }

        return bytes.ToArray();
    }

    private static async Task<string?> ConsumeChallengeAsync(
        IServiceProvider services,
        Guid challengeId,
        string accountId,
        SecurityContext context)
    {
        await using var scope = services.CreateAsyncScope();
        var challenges = scope.ServiceProvider.GetRequiredService<IdentitySecurityChallengeService>();
        return await challenges.ReadAndConsumeAsync<string>(
            challengeId,
            accountId,
            "test",
            context);
    }

    private static async Task<bool> RedeemRecoveryAsync(
        IServiceProvider services,
        LocalRecoveryRedeemRequest request)
    {
        await using var scope = services.CreateAsyncScope();
        var recovery = scope.ServiceProvider.GetRequiredService<LocalRecoveryService>();
        return await recovery.RedeemAsync(request);
    }

    private static async Task<SecurityTokenRefreshResult> RefreshTokenAsync(
        IServiceProvider services,
        string refreshToken)
    {
        await using var scope = services.CreateAsyncScope();
        return await scope.ServiceProvider
            .GetRequiredService<SecurityTokenService>()
            .RefreshAsync(refreshToken, "concurrent-refresh-test");
    }

    private static async Task WithTemporaryDatabaseAsync(
        Func<ServiceProvider, MutableTimeProvider, Task> test)
    {
        var databaseName = $"DtudoIdentity.Stage12Tests.{Guid.NewGuid():N}";
        var connectionString = new SqlConnectionStringBuilder
        {
            DataSource = "(localdb)\\MSSQLLocalDB",
            InitialCatalog = databaseName,
            IntegratedSecurity = true,
            Encrypt = false,
            TrustServerCertificate = true
        }.ConnectionString;
        var timeProvider = new MutableTimeProvider(InitialTime);
        var services = new ServiceCollection();
        services.AddDbContext<IdentityDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDataProtection()
            .SetApplicationName("Dtudo2026.ApiIdentity.Tests");
        services.AddIdentityCore<IdentityAccount>(options =>
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
        services.AddSingleton<TimeProvider>(timeProvider);
        services.AddSingleton<IOptions<IdentityMfaOptions>>(
            Options.Create(new IdentityMfaOptions
            {
                Fido2TimeoutMilliseconds = 60_000,
                Fido2TimestampDriftMilliseconds = 30_000,
                ChallengeLifetimeSeconds = 5,
                StepUpLifetimeSeconds = 5,
                LocalRecoveryLifetimeMinutes = 5,
                SnapshotLifetimeHours = 1,
                RecoveryCodeCount = 5,
                ClockSkewSeconds = 2,
                RelyingPartyDomain = "localhost",
                RelyingPartyName = "Dtudo2026 Tests",
                Origins = ["https://localhost:7243"]
            }));
        services.AddSingleton<IOptions<IdentitySessionOptions>>(
            Options.Create(new IdentitySessionOptions
            {
                LifetimeDays = 30,
                AccessTokenLifetimeSeconds = 300,
                RefreshTokenLifetimeDays = 30,
                TokenEntropyBytes = 32
            }));
        services.AddSingleton<IOptions<LocalProvisioningOptions>>(
            Options.Create(new LocalProvisioningOptions
            {
                AdministrationSecret = Microsoft.AspNetCore.WebUtilities.WebEncoders.Base64UrlEncode(
                    RandomNumberGenerator.GetBytes(32)),
                InitialSecretLifetimeMinutes = 60
            }));
        services.AddScoped<IdentityProvisioningAuditWriter>();
        services.AddScoped<IdentitySecurityAuditWriter>();
        services.AddScoped<AccountProvisioningService>();
        services.AddScoped<SecuritySessionService>();
        services.AddScoped<SecurityTokenService>();
        services.AddScoped<IdentitySecurityChallengeService>();
        services.AddScoped<StepUpService>();
        services.AddScoped<TotpMfaService>();
        services.AddScoped<PasskeyMfaService>();
        services.AddScoped<LocalRecoveryService>();
        services.AddScoped<SecuritySnapshotService>();
        services.AddScoped<IdentityAdministrationService>();
        services.AddSingleton<IFido2>(_ => new Fido2(new Fido2Configuration
        {
            ServerDomain = "localhost",
            ServerName = "Dtudo2026 Tests",
            Origins = new HashSet<string>(StringComparer.Ordinal) { "https://localhost:7243" },
            Timeout = 60_000,
            TimestampDriftTolerance = 30_000,
            ChallengeSize = 32
        }));

        await using var serviceProvider = services.BuildServiceProvider();
        try
        {
            await using (var scope = serviceProvider.CreateAsyncScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
                await context.Database.MigrateAsync();
            }

            await test(serviceProvider, timeProvider);
        }
        finally
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            await context.Database.EnsureDeletedAsync();
        }
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
