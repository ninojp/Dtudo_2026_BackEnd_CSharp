using System.Security.Claims;
using ApiIdentity.Authorization;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class StepUpService
{
    private static readonly HashSet<string> SupportedMethods =
    [
        IdentityMfaMethods.Passkey,
        IdentityMfaMethods.Totp,
        IdentityMfaMethods.RecoveryCode,
        IdentityMfaMethods.LocalRecovery
    ];

    private readonly IdentityDbContext _context;
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly SecuritySessionService _sessionService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityMfaOptions _options;

    public StepUpService(
        IdentityDbContext context,
        UserManager<IdentityAccount> userManager,
        SecuritySessionService sessionService,
        IdentitySecurityAuditWriter auditWriter,
        TimeProvider timeProvider,
        IOptions<IdentityMfaOptions> options)
    {
        _context = context;
        _userManager = userManager;
        _sessionService = sessionService;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<StepUpGrantResult?> GrantAsync(
        ClaimsPrincipal principal,
        string action,
        string method,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAuthorizedAccountAsync(
            principal,
            action,
            method,
            context,
            cancellationToken);
        if (account is null)
        {
            return null;
        }

        var normalizedContext = SecurityContextFactory.Normalize(context);
        var grantedAtUtc = _timeProvider.GetUtcNow();
        var grant = new IdentityStepUpGrant
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            Action = action.Trim(),
            Method = method.Trim(),
            SessionId = normalizedContext.SessionId,
            DeviceId = normalizedContext.DeviceId,
            GrantedAtUtc = grantedAtUtc,
            ExpiresAtUtc = grantedAtUtc.AddSeconds(_options.StepUpLifetimeSeconds)
        };
        _context.StepUpGrants.Add(grant);
        _auditWriter.Record(
            account.Id,
            "identity.security.step-up.granted",
            $"action:{grant.Action}",
            "succeeded",
            grant.DeviceId!,
            $"method:{grant.Method}");
        await _context.SaveChangesAsync(cancellationToken);
        return new StepUpGrantResult(grant.Id, grant.ExpiresAtUtc);
    }

    public async Task<bool> IsAllowedAsync(
        ClaimsPrincipal principal,
        string action,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var account = await GetAuthorizedAccountAsync(
            principal,
            action,
            method: null,
            context,
            cancellationToken);
        if (account is null)
        {
            return false;
        }

        var normalizedContext = SecurityContextFactory.Normalize(context);
        var now = _timeProvider.GetUtcNow();
        var earliestAllowedGrant = now.Subtract(TimeSpan.FromSeconds(_options.ClockSkewSeconds));
        return await _context.StepUpGrants.AnyAsync(
            grant => grant.AccountId == account.Id
                && grant.Action == action.Trim()
                && (grant.Method == IdentityMfaMethods.Passkey
                    || grant.Method == IdentityMfaMethods.Totp
                    || grant.Method == IdentityMfaMethods.RecoveryCode
                    || grant.Method == IdentityMfaMethods.LocalRecovery)
                && grant.RevokedAtUtc == null
                && grant.GrantedAtUtc <= now.Add(TimeSpan.FromSeconds(_options.ClockSkewSeconds))
                && grant.ExpiresAtUtc > earliestAllowedGrant
                && grant.SessionId == normalizedContext.SessionId
                && grant.DeviceId == normalizedContext.DeviceId,
            cancellationToken);
    }

    public async Task<bool> RevokeAsync(
        ClaimsPrincipal principal,
        Guid grantId,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetPrincipalAccountId(principal);
        if (accountId is null)
        {
            return false;
        }

        var grant = await _context.StepUpGrants.SingleOrDefaultAsync(
            item => item.Id == grantId && item.AccountId == accountId,
            cancellationToken);
        if (grant is null || grant.RevokedAtUtc is not null)
        {
            return false;
        }

        grant.RevokedAtUtc = _timeProvider.GetUtcNow();
        _auditWriter.Record(
            accountId,
            "identity.security.step-up.revoked",
            $"grant:{grantId:D}",
            "succeeded",
            grant.DeviceId ?? "unknown-device",
            "grant-revoked");
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<int> RevokeAllAsync(
        ClaimsPrincipal principal,
        CancellationToken cancellationToken = default)
    {
        var accountId = GetPrincipalAccountId(principal);
        if (accountId is null)
        {
            return 0;
        }

        var count = await _context.StepUpGrants
            .Where(item => item.AccountId == accountId && item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAtUtc, _timeProvider.GetUtcNow()),
                cancellationToken);
        _auditWriter.Record(
            accountId,
            "identity.security.step-up.revoked-all",
            $"account:{accountId}",
            "succeeded",
            "all-devices",
            "bulk-grant-revocation");
        await _context.SaveChangesAsync(cancellationToken);
        return count;
    }

    private async Task<IdentityAccount?> GetAuthorizedAccountAsync(
        ClaimsPrincipal principal,
        string action,
        string? method,
        SecurityContext context,
        CancellationToken cancellationToken)
    {
        if (principal.Identity?.IsAuthenticated != true
            || string.IsNullOrWhiteSpace(action)
            || !AuthorizationCatalog.AllPermissions.Any(permission =>
                permission.Key == action.Trim())
            || (method is not null
                && !SupportedMethods.Contains(method.Trim()))
            || !SecurityContextFactory.TryGetIds(context, out _, out _))
        {
            return null;
        }

        var accountId = GetPrincipalAccountId(principal);
        if (accountId is null)
        {
            return null;
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null
            || !await HasPermissionAsync(principal, account, action.Trim())
            || !await _sessionService.IsActiveBindingAsync(account.Id, context, cancellationToken))
        {
            return null;
        }

        return account;
    }

    private async Task<bool> HasPermissionAsync(
        ClaimsPrincipal principal,
        IdentityAccount account,
        string action)
    {
        if (principal.HasClaim(AuthorizationCatalog.PermissionClaimType, action))
        {
            return true;
        }

        var roles = await _userManager.GetRolesAsync(account);
        return roles.Any(roleName => AuthorizationCatalog.AllRoles
            .Any(role => string.Equals(role.Name, roleName, StringComparison.Ordinal)
                && role.PermissionKeys.Contains(action, StringComparer.Ordinal)));
    }

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            return null;
        }

        return principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");
    }
}
