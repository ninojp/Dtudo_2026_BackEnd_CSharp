using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class SecuritySnapshotService
{
    private const int SnapshotSchemaVersion = 1;

    private readonly IdentityDbContext _context;
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly IUserStore<IdentityAccount> _userStore;
    private readonly StepUpService _stepUpService;
    private readonly SecuritySessionService _sessionService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityMfaOptions _options;

    public SecuritySnapshotService(
        IdentityDbContext context,
        UserManager<IdentityAccount> userManager,
        IUserStore<IdentityAccount> userStore,
        StepUpService stepUpService,
        SecuritySessionService sessionService,
        IdentitySecurityAuditWriter auditWriter,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        IOptions<IdentityMfaOptions> options)
    {
        _context = context;
        _userManager = userManager;
        _userStore = userStore;
        _stepUpService = stepUpService;
        _sessionService = sessionService;
        _auditWriter = auditWriter;
        _protector = dataProtectionProvider.CreateProtector(
            "Dtudo2026",
            "ApiIdentity",
            "SecuritySnapshot");
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<SecuritySnapshotResult?> CreateAsync(
        ClaimsPrincipal principal,
        string accountId,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var actor = await FindAuthorizedActorAsync(principal, accountId, cancellationToken);
        if (actor is null
            || !await _sessionService.IsActiveBindingAsync(actor.Id, context, cancellationToken))
        {
            return null;
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null)
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var payload = await CreatePayloadAsync(account, now);
        var snapshot = new IdentitySecuritySnapshot
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            ProtectedPayload = _protector.Protect(JsonSerializer.Serialize(payload)),
            CreatedBy = actor.Id,
            CreatedAtUtc = now,
            ExpiresAtUtc = now.AddHours(_options.SnapshotLifetimeHours)
        };
        _context.SecuritySnapshots.Add(snapshot);
        _auditWriter.Record(
            actor.Id,
            "identity.security.snapshot.created",
            $"account:{account.Id}",
            "succeeded",
            GetDeviceId(principal),
            "protected-security-snapshot-created");
        await _context.SaveChangesAsync(cancellationToken);
        return new SecuritySnapshotResult(snapshot.Id);
    }

    public async Task<SecurityOperationResult> RestoreAsync(
        ClaimsPrincipal principal,
        string accountId,
        Guid snapshotId,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var actor = await FindAuthorizedActorAsync(principal, accountId, cancellationToken);
        if (actor is null
            || !await _stepUpService.IsAllowedAsync(
                principal,
                AuthorizationCatalog.Permissions.IdentityProvision,
                context,
                cancellationToken))
        {
            return new SecurityOperationResult(false);
        }

        var snapshot = await _context.SecuritySnapshots.SingleOrDefaultAsync(
            item => item.Id == snapshotId && item.AccountId == accountId,
            cancellationToken);
        if (snapshot is null
            || snapshot.RestoredAtUtc is not null
            || snapshot.RevokedAtUtc is not null
            || snapshot.ExpiresAtUtc <= _timeProvider.GetUtcNow())
        {
            return new SecurityOperationResult(false);
        }

        SecuritySnapshotPayload payload;
        try
        {
            payload = JsonSerializer.Deserialize<SecuritySnapshotPayload>(
                _protector.Unprotect(snapshot.ProtectedPayload))!;
            if (payload is null || payload.SchemaVersion != SnapshotSchemaVersion)
            {
                return new SecurityOperationResult(false);
            }
        }
        catch (CryptographicException)
        {
            return new SecurityOperationResult(false);
        }
        catch (JsonException)
        {
            return new SecurityOperationResult(false);
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null)
        {
            return new SecurityOperationResult(false);
        }

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var existingPasskeys = await _userManager.GetPasskeysAsync(account);
            foreach (var passkey in existingPasskeys)
            {
                var removal = await _userManager.RemovePasskeyAsync(account, passkey.CredentialId);
                if (!removal.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SecurityOperationResult(false);
                }
            }

            foreach (var passkey in payload.Passkeys)
            {
                var restored = await _userManager.AddOrUpdatePasskeyAsync(
                    account,
                    ToPasskey(passkey));
                if (!restored.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SecurityOperationResult(false);
                }
            }

            if (_userStore is not IUserAuthenticatorKeyStore<IdentityAccount> keyStore)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SecurityOperationResult(false);
            }

            if (string.IsNullOrWhiteSpace(payload.AuthenticatorKey))
            {
                var resetKey = await _userManager.ResetAuthenticatorKeyAsync(account);
                if (!resetKey.Succeeded)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return new SecurityOperationResult(false);
                }
            }
            else
            {
                await keyStore.SetAuthenticatorKeyAsync(
                    account,
                    payload.AuthenticatorKey,
                    cancellationToken);
            }

            var enabled = await _userManager.SetTwoFactorEnabledAsync(
                account,
                payload.TwoFactorEnabled);
            if (!enabled.Succeeded)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SecurityOperationResult(false);
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(
                account,
                payload.TwoFactorEnabled ? _options.RecoveryCodeCount : 0);
            if (recoveryCodes is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return new SecurityOperationResult(false);
            }

            await _sessionService.RevokeSecurityStateAsync(
                account.Id,
                cancellationToken: cancellationToken);
            snapshot.RestoredAtUtc = _timeProvider.GetUtcNow();
            snapshot.RestoredBy = actor.Id;
            _auditWriter.Record(
                actor.Id,
                "identity.security.snapshot.restored",
                $"snapshot:{snapshotId:D}",
                "succeeded",
                GetDeviceId(principal),
                "protected-security-state-restored");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SecurityOperationResult(true, snapshot.Id, recoveryCodes.ToArray());
        }
        catch (DbUpdateConcurrencyException)
        {
            await transaction.RollbackAsync(cancellationToken);
            _context.ChangeTracker.Clear();
            return new SecurityOperationResult(false);
        }
    }

    public async Task<bool> RevokeAsync(
        ClaimsPrincipal principal,
        string accountId,
        Guid snapshotId,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var actor = await FindAuthorizedActorAsync(principal, accountId, cancellationToken);
        if (actor is null
            || !await _stepUpService.IsAllowedAsync(
                principal,
                AuthorizationCatalog.Permissions.IdentityProvision,
                context,
                cancellationToken))
        {
            return false;
        }

        var snapshot = await _context.SecuritySnapshots.SingleOrDefaultAsync(
            item => item.Id == snapshotId && item.AccountId == accountId,
            cancellationToken);
        if (snapshot is null
            || snapshot.RestoredAtUtc is not null
            || snapshot.RevokedAtUtc is not null)
        {
            return false;
        }

        snapshot.RevokedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            actor.Id,
            "identity.security.snapshot.revoked",
            $"snapshot:{snapshotId:D}",
            "succeeded",
            GetDeviceId(principal),
            "protected-security-snapshot-revoked");
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return false;
        }
    }

    private async Task<SecuritySnapshotPayload> CreatePayloadAsync(
        IdentityAccount account,
        DateTimeOffset capturedAtUtc)
    {
        var passkeys = await _userManager.GetPasskeysAsync(account);
        return new SecuritySnapshotPayload(
            SnapshotSchemaVersion,
            await _userManager.GetTwoFactorEnabledAsync(account),
            await _userManager.GetAuthenticatorKeyAsync(account),
            passkeys.Select(passkey => new PasskeySnapshotEntry(
                Convert.ToBase64String(passkey.CredentialId),
                Convert.ToBase64String(passkey.PublicKey),
                passkey.CreatedAt,
                passkey.SignCount,
                passkey.Transports,
                passkey.IsUserVerified,
                passkey.IsBackupEligible,
                passkey.IsBackedUp,
                Convert.ToBase64String(passkey.AttestationObject),
                Convert.ToBase64String(passkey.ClientDataJson),
                passkey.Name)).ToArray(),
            capturedAtUtc);
    }

    private async Task<IdentityAccount?> FindAuthorizedActorAsync(
        ClaimsPrincipal principal,
        string accountId,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(accountId))
        {
            return null;
        }

        var actorId = GetPrincipalAccountId(principal);
        if (actorId is null)
        {
            return null;
        }

        var managesAllAccounts = principal.HasClaim(
                AuthorizationCatalog.PermissionClaimType,
                AuthorizationCatalog.Permissions.IdentityProvision)
            || principal.IsInRole(AuthorizationCatalog.Roles.SuperAdministrator);
        if (!managesAllAccounts
            && !string.Equals(actorId, accountId, StringComparison.Ordinal))
        {
            return null;
        }

        return await _userManager.FindByIdAsync(actorId);
    }

    private static UserPasskeyInfo ToPasskey(PasskeySnapshotEntry passkey) =>
        new(
            Convert.FromBase64String(passkey.CredentialId),
            Convert.FromBase64String(passkey.PublicKey),
            passkey.CreatedAt,
            passkey.SignCount,
            passkey.Transports,
            passkey.IsUserVerified,
            passkey.IsBackupEligible,
            passkey.IsBackedUp,
            Convert.FromBase64String(passkey.AttestationObject),
            Convert.FromBase64String(passkey.ClientDataJson))
        {
            Name = passkey.Name
        };

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
            : null;

    private static string GetDeviceId(ClaimsPrincipal principal) =>
        principal.FindFirst("device_id")?.Value ?? "unknown-device";

    private sealed record SecuritySnapshotPayload(
        int SchemaVersion,
        bool TwoFactorEnabled,
        string? AuthenticatorKey,
        IReadOnlyList<PasskeySnapshotEntry> Passkeys,
        DateTimeOffset CapturedAtUtc);

    private sealed record PasskeySnapshotEntry(
        string CredentialId,
        string PublicKey,
        DateTimeOffset CreatedAt,
        uint SignCount,
        string[]? Transports,
        bool IsUserVerified,
        bool IsBackupEligible,
        bool IsBackedUp,
        string AttestationObject,
        string ClientDataJson,
        string? Name);
}
