using System.Security.Claims;
using ApiIdentity.Data;
using ApiIdentity.Mfa;
using ApiIdentity.Models;
using ApiIdentity.Provisioning;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApiIdentity.Authorization;

public sealed class IdentityAdministrationService
{
    private const string ProvisionAction = AuthorizationCatalog.Permissions.IdentityProvision;

    private readonly IdentityDbContext _context;
    private readonly UserManager<IdentityAccount> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AccountProvisioningService _provisioningService;
    private readonly SecuritySessionService _sessionService;
    private readonly StepUpService _stepUpService;
    private readonly IdentityProvisioningAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;

    public IdentityAdministrationService(
        IdentityDbContext context,
        UserManager<IdentityAccount> userManager,
        RoleManager<IdentityRole> roleManager,
        AccountProvisioningService provisioningService,
        SecuritySessionService sessionService,
        StepUpService stepUpService,
        IdentityProvisioningAuditWriter auditWriter,
        TimeProvider timeProvider)
    {
        _context = context;
        _userManager = userManager;
        _roleManager = roleManager;
        _provisioningService = provisioningService;
        _sessionService = sessionService;
        _stepUpService = stepUpService;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
    }

    public async Task<bool> IsActiveSessionAsync(
        ClaimsPrincipal principal,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetPrincipalAccountId(principal);
        return actorId is not null
            && await _sessionService.IsActiveBindingAsync(actorId, context, cancellationToken);
    }

    public async Task<IReadOnlyList<IdentityAdminAccountView>> GetAccountsAsync(
        CancellationToken cancellationToken = default)
    {
        var accounts = await _context.Users.AsNoTracking()
            .OrderBy(account => account.UserName)
            .ToListAsync(cancellationToken);
        var result = new List<IdentityAdminAccountView>(accounts.Count);
        foreach (var account in accounts)
        {
            var roles = await _userManager.GetRolesAsync(account);
            result.Add(new IdentityAdminAccountView(
                account.Id,
                account.UserName,
                account.Email,
                account.IsActivationCompleted,
                IsLocked(account),
                account.LockoutEnd,
                roles.ToArray()));
        }

        return result;
    }

    public IReadOnlyList<IdentityAdminRoleView> GetRoles() => AuthorizationCatalog.AllRoles
        .Select(role => new IdentityAdminRoleView(role.Id, role.Name, role.PermissionKeys))
        .ToArray();

    public IReadOnlyList<IdentityAdminPermissionView> GetPermissions() => AuthorizationCatalog.AllPermissions
        .Select(permission => new IdentityAdminPermissionView(permission.Key, permission.Description))
        .ToArray();

    public async Task<IReadOnlyList<IdentityAdminDeviceView>> GetDevicesAsync(
        bool includeRevoked,
        CancellationToken cancellationToken = default)
    {
        return await _context.SecurityDevices.AsNoTracking()
            .Where(device => includeRevoked || device.RevokedAtUtc == null)
            .OrderByDescending(device => device.LastSeenAtUtc)
            .Select(device => new IdentityAdminDeviceView(
                device.AccountId,
                device.Id,
                device.Name,
                device.CreatedAtUtc,
                device.LastSeenAtUtc,
                device.TrustedUntilUtc,
                device.RevokedAtUtc != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IdentityAdminSessionView>> GetSessionsAsync(
        bool includeRevoked,
        CancellationToken cancellationToken = default)
    {
        return await _context.SecuritySessions.AsNoTracking()
            .Where(session => includeRevoked || session.RevokedAtUtc == null)
            .OrderByDescending(session => session.LastSeenAtUtc)
            .Select(session => new IdentityAdminSessionView(
                session.AccountId,
                session.Id,
                session.DeviceId,
                session.CreatedAtUtc,
                session.LastSeenAtUtc,
                session.ExpiresAtUtc,
                session.RevokedAtUtc != null))
            .ToListAsync(cancellationToken);
    }

    public async Task<IdentityAdminProvisionResult> ProvisionAsync(
        ClaimsPrincipal principal,
        IdentityAdminProvisionRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = await GetAuthorizedActorAsync(
            principal,
            request.SessionId,
            request.DeviceId,
            cancellationToken);
        if (actorId is null)
        {
            return new IdentityAdminProvisionResult(false);
        }

        var result = await _provisioningService.ProvisionAsync(
            new ProvisionAccountRequest(request.UserName, request.Email, request.RoleName),
            actorId,
            cancellationToken);
        return new IdentityAdminProvisionResult(result.Succeeded, result.Delivery);
    }

    public async Task<bool> AssignRoleAsync(
        ClaimsPrincipal principal,
        string accountId,
        IdentityAdminRoleAssignmentRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = await GetAuthorizedActorAsync(
            principal,
            request.SessionId,
            request.DeviceId,
            cancellationToken);
        if (actorId is null || !IsCatalogRole(request.RoleName))
        {
            return false;
        }

        var account = await _userManager.FindByIdAsync(accountId);
        var roleName = request.RoleName.Trim();
        if (account is null
            || (!request.Assign
                && account.Id == actorId
                && string.Equals(roleName, AuthorizationCatalog.Roles.SuperAdministrator, StringComparison.Ordinal)))
        {
            return false;
        }

        var result = request.Assign
            ? await _userManager.AddToRoleAsync(account, roleName)
            : await _userManager.RemoveFromRoleAsync(account, roleName);
        if (!result.Succeeded)
        {
            return false;
        }

        _auditWriter.Record(
            actorId,
            request.Assign ? "identity.admin.role-assigned" : "identity.admin.role-removed",
            $"account:{accountId}",
            "succeeded",
            request.DeviceId ?? "unknown-device",
            $"role:{roleName}");
        return await _context.SaveChangesAsync(cancellationToken) >= 0;
    }

    public async Task<bool> SetLockAsync(
        ClaimsPrincipal principal,
        string accountId,
        IdentityAdminLockRequest request,
        CancellationToken cancellationToken = default)
    {
        var actorId = await GetAuthorizedActorAsync(
            principal,
            request.SessionId,
            request.DeviceId,
            cancellationToken);
        if (actorId is null)
        {
            return false;
        }

        var account = await _userManager.FindByIdAsync(accountId);
        if (account is null)
        {
            return false;
        }

        var lockoutResult = request.Lock
            ? await _userManager.SetLockoutEndDateAsync(account, _timeProvider.GetUtcNow().AddYears(10))
            : await _userManager.SetLockoutEndDateAsync(account, null);
        if (!lockoutResult.Succeeded)
        {
            return false;
        }

        if (request.Lock)
        {
            await _sessionService.RevokeSecurityStateAsync(accountId, _timeProvider.GetUtcNow(), cancellationToken);
        }

        _auditWriter.Record(
            actorId,
            request.Lock ? "identity.admin.account-locked" : "identity.admin.account-unlocked",
            $"account:{accountId}",
            "succeeded",
            request.DeviceId ?? "unknown-device",
            request.Lock ? "administrative-lock" : "administrative-unlock");
        return await _context.SaveChangesAsync(cancellationToken) >= 0;
    }

    public async Task<bool> RevokeSessionAsync(
        ClaimsPrincipal principal,
        Guid sessionId,
        string? requestSessionId,
        string? requestDeviceId,
        CancellationToken cancellationToken = default)
    {
        var actorId = await GetAuthorizedActorAsync(
            principal,
            requestSessionId,
            requestDeviceId,
            cancellationToken);
        if (actorId is null)
        {
            return false;
        }

        var target = await _context.SecuritySessions.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        return target is not null
            && await _sessionService.RevokeSessionAsync(target.AccountId, sessionId, actorId, cancellationToken);
    }

    public async Task<bool> RevokeDeviceAsync(
        ClaimsPrincipal principal,
        Guid deviceId,
        string? requestSessionId,
        string? requestDeviceId,
        CancellationToken cancellationToken = default)
    {
        var actorId = await GetAuthorizedActorAsync(
            principal,
            requestSessionId,
            requestDeviceId,
            cancellationToken);
        if (actorId is null)
        {
            return false;
        }

        var target = await _context.SecurityDevices.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == deviceId, cancellationToken);
        return target is not null
            && await _sessionService.RevokeDeviceAsync(target.AccountId, deviceId, actorId, cancellationToken);
    }

    private async Task<string?> GetAuthorizedActorAsync(
        ClaimsPrincipal principal,
        string? sessionId,
        string? deviceId,
        CancellationToken cancellationToken)
    {
        var actorId = GetPrincipalAccountId(principal);
        if (actorId is null)
        {
            return null;
        }

        var context = new SecurityContext(sessionId, deviceId);
        return await _stepUpService.IsAllowedAsync(
                principal,
                ProvisionAction,
                context,
                cancellationToken)
            ? actorId
            : null;
    }

    private static bool IsCatalogRole(string? roleName) => AuthorizationCatalog.AllRoles
        .Any(role => string.Equals(role.Name, roleName?.Trim(), StringComparison.Ordinal));

    private static bool IsLocked(IdentityAccount account) =>
        account.LockoutEnabled
        && account.LockoutEnd is { } lockoutEnd
        && lockoutEnd > DateTimeOffset.UtcNow;

    private static string? GetPrincipalAccountId(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true
            ? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
            : null;
}
