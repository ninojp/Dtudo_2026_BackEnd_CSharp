using ApiIdentity.Data;
using ApiIdentity.Configuration;
using ApiIdentity.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class SecuritySessionService
{
    private readonly IdentityDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly IdentitySessionOptions _options;

    public SecuritySessionService(
        IdentityDbContext context,
        TimeProvider timeProvider,
        IdentitySecurityAuditWriter auditWriter,
        IOptions<IdentitySessionOptions> options)
    {
        _context = context;
        _timeProvider = timeProvider;
        _auditWriter = auditWriter;
        _options = options.Value;
    }

    public async Task<SecurityDeviceSessionResult?> CreateAsync(
        string accountId,
        string name,
        string actor,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(name)
            || name.Trim().Length > 120
            || !await _context.Users.AsNoTracking().AnyAsync(
                account => account.Id == accountId,
                cancellationToken))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
    var sessionExpiresAtUtc = now.AddDays(_options.LifetimeDays);
        var device = new IdentitySecurityDevice
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Name = name.Trim(),
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            TrustedAtUtc = now,
            TrustedUntilUtc = sessionExpiresAtUtc
        };
        var session = new IdentitySecuritySession
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            DeviceId = device.Id,
            CreatedAtUtc = now,
            LastSeenAtUtc = now,
            ExpiresAtUtc = sessionExpiresAtUtc
        };
        _context.SecurityDevices.Add(device);
        _context.SecuritySessions.Add(session);
        _auditWriter.Record(
            actor,
            "identity.security.session.created",
            $"account:{accountId}",
            "succeeded",
            device.Id.ToString("D"),
            "session-created");
        await _context.SaveChangesAsync(cancellationToken);
        return new SecurityDeviceSessionResult(
            device.Id,
            session.Id,
            now,
            session.ExpiresAtUtc,
            device.TrustedUntilUtc);
    }

    public async Task<bool> IsActiveBindingAsync(
        string accountId,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId)
            || !SecurityContextFactory.TryGetIds(context, out var sessionId, out var deviceId))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        return await _context.SecuritySessions.AnyAsync(
            session => session.Id == sessionId
                && session.AccountId == accountId
                && session.DeviceId == deviceId
                && session.RevokedAtUtc == null
                && session.ExpiresAtUtc > now
                && session.Device != null
                && session.Device.AccountId == accountId
                && session.Device.RevokedAtUtc == null
                && session.Device.TrustedUntilUtc > now
                && session.Account != null
                && (!session.Account.LockoutEnabled
                    || session.Account.LockoutEnd == null
                    || session.Account.LockoutEnd <= now),
            cancellationToken);
    }

    public async Task<bool> TouchAsync(
        string accountId,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        if (!SecurityContextFactory.TryGetIds(context, out var sessionId, out var deviceId))
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        var session = await _context.SecuritySessions
            .Include(item => item.Device)
            .Include(item => item.Account)
            .SingleOrDefaultAsync(
            item => item.Id == sessionId
                && item.AccountId == accountId
                && item.DeviceId == deviceId
                && item.RevokedAtUtc == null
                && item.ExpiresAtUtc > now
                && item.Device != null
                && item.Device.RevokedAtUtc == null
                && item.Device.TrustedUntilUtc > now
                && item.Account != null
                && (!item.Account.LockoutEnabled
                    || item.Account.LockoutEnd == null
                    || item.Account.LockoutEnd <= now),
            cancellationToken);
        if (session is null || session.Device is null)
        {
            return false;
        }

        session.LastSeenAtUtc = now;
        session.Device.LastSeenAtUtc = now;
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

    public async Task<IReadOnlyList<SecurityDeviceView>> GetDevicesAsync(
        string accountId,
        bool includeRevoked,
        CancellationToken cancellationToken = default)
    {
        var devices = await _context.SecurityDevices.AsNoTracking()
            .Where(device => device.AccountId == accountId
                && (includeRevoked || device.RevokedAtUtc == null))
            .OrderByDescending(device => device.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
        return devices.Select(device => new SecurityDeviceView(
            device.Id,
            device.Name,
            device.CreatedAtUtc,
            device.LastSeenAtUtc,
            device.TrustedUntilUtc,
            device.RevokedAtUtc is not null)).ToArray();
    }

    public async Task<IReadOnlyList<SecuritySessionView>> GetSessionsAsync(
        string accountId,
        bool includeRevoked,
        CancellationToken cancellationToken = default)
    {
        var sessions = await _context.SecuritySessions.AsNoTracking()
            .Where(session => session.AccountId == accountId
                && (includeRevoked || session.RevokedAtUtc == null))
            .OrderByDescending(session => session.LastSeenAtUtc)
            .ToListAsync(cancellationToken);
        return sessions.Select(session => new SecuritySessionView(
            session.Id,
            session.DeviceId,
            session.CreatedAtUtc,
            session.LastSeenAtUtc,
            session.ExpiresAtUtc,
            session.RevokedAtUtc is not null)).ToArray();
    }

    public async Task<bool> RevokeSessionAsync(
        string accountId,
        Guid sessionId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var session = await _context.SecuritySessions.SingleOrDefaultAsync(
            item => item.Id == sessionId && item.AccountId == accountId,
            cancellationToken);
        if (session is null || session.RevokedAtUtc is not null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        session.RevokedAtUtc = now;
        await RevokeContextAsync(accountId, session.Id.ToString("D"), null, now, cancellationToken);
        _auditWriter.Record(
            actor,
            "identity.security.session.revoked",
            $"session:{sessionId:D}",
            "succeeded",
            session.DeviceId.ToString("D"),
            "session-revoked");
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

    public async Task<bool> RevokeDeviceAsync(
        string accountId,
        Guid deviceId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var device = await _context.SecurityDevices.SingleOrDefaultAsync(
            item => item.Id == deviceId && item.AccountId == accountId,
            cancellationToken);
        if (device is null || device.RevokedAtUtc is not null)
        {
            return false;
        }

        var now = _timeProvider.GetUtcNow();
        device.RevokedAtUtc = now;
        await _context.SecuritySessions
            .Where(session => session.AccountId == accountId && session.DeviceId == deviceId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, now), cancellationToken);
        await RevokeContextAsync(accountId, null, deviceId.ToString("D"), now, cancellationToken);
        _auditWriter.Record(
            actor,
            "identity.security.device.revoked",
            $"device:{deviceId:D}",
            "succeeded",
            deviceId.ToString("D"),
            "device-revoked");
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

    public async Task<int> RevokeAllAsync(
        string accountId,
        string actor,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var affected = await _context.SecuritySessions
            .Where(session => session.AccountId == accountId && session.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, now), cancellationToken);
        affected += await _context.SecurityDevices
            .Where(device => device.AccountId == accountId && device.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters.SetProperty(item => item.RevokedAtUtc, now), cancellationToken);
        await RevokeContextAsync(accountId, null, null, now, cancellationToken);
        _auditWriter.Record(
            actor,
            "identity.security.sessions.revoked-all",
            $"account:{accountId}",
            "succeeded",
            "all-devices",
            "bulk-session-revocation");
        await _context.SaveChangesAsync(cancellationToken);
        return affected;
    }

    public Task<int> RevokeSecurityStateAsync(
        string accountId,
        DateTimeOffset? now = null,
        CancellationToken cancellationToken = default)
    {
        var occurredAtUtc = now ?? _timeProvider.GetUtcNow();
        return RevokeSecurityStateCoreAsync(accountId, occurredAtUtc, cancellationToken);
    }

    private async Task<int> RevokeSecurityStateCoreAsync(
        string accountId,
        DateTimeOffset occurredAtUtc,
        CancellationToken cancellationToken)
    {
        var affected = await _context.SecuritySessions
            .Where(item => item.AccountId == accountId && item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAtUtc, occurredAtUtc),
                cancellationToken);
        affected += await _context.SecurityDevices
            .Where(item => item.AccountId == accountId && item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAtUtc, occurredAtUtc),
                cancellationToken);
        affected += await RevokeContextAsync(
            accountId,
            null,
            null,
            occurredAtUtc,
            cancellationToken);
        return affected;
    }

    private async Task<int> RevokeContextAsync(
        string accountId,
        string? sessionId,
        string? deviceId,
        DateTimeOffset now,
        CancellationToken cancellationToken,
        bool includeSessions = false,
        bool includeDevices = false)
    {
        var challenges = _context.SecurityChallenges.Where(item => item.AccountId == accountId
            && item.ConsumedAtUtc == null
            && item.RevokedAtUtc == null);
        var grants = _context.StepUpGrants.Where(item => item.AccountId == accountId
            && item.RevokedAtUtc == null);
        if (!includeSessions && !string.IsNullOrWhiteSpace(sessionId))
        {
            challenges = challenges.Where(item => item.SessionId == sessionId);
            grants = grants.Where(item => item.SessionId == sessionId);
        }
        else if (!includeDevices && !string.IsNullOrWhiteSpace(deviceId))
        {
            challenges = challenges.Where(item => item.DeviceId == deviceId);
            grants = grants.Where(item => item.DeviceId == deviceId);
        }

        var tokens = _context.SecurityTokens
            .Where(item => item.AccountId == accountId && item.RevokedAtUtc == null);
        if (!includeSessions && Guid.TryParse(sessionId, out var sessionGuid))
        {
            tokens = tokens.Where(item => item.SessionId == sessionGuid);
        }
        else if (!includeDevices && Guid.TryParse(deviceId, out var deviceGuid))
        {
            tokens = tokens.Where(item => item.DeviceId == deviceGuid);
        }

        var affected = await challenges.ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.RevokedAtUtc, now),
            cancellationToken);
        affected += await grants.ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.RevokedAtUtc, now),
            cancellationToken);
        affected += await tokens.ExecuteUpdateAsync(
            setters => setters.SetProperty(item => item.RevokedAtUtc, now),
            cancellationToken);
        return affected;
    }
}
