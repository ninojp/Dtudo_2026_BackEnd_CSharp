using System.Text.Json;
using System.Security.Cryptography;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ApiIdentity.Mfa;

public sealed class IdentitySecurityChallengeService
{
    private readonly IdentityDbContext _context;
    private readonly IDataProtector _protector;
    private readonly TimeProvider _timeProvider;
    private readonly IdentityMfaOptions _options;
    private readonly SecuritySessionService _sessionService;

    public IdentitySecurityChallengeService(
        IdentityDbContext context,
        IDataProtectionProvider dataProtectionProvider,
        TimeProvider timeProvider,
        IOptions<IdentityMfaOptions> options,
        SecuritySessionService sessionService)
    {
        _context = context;
        _protector = dataProtectionProvider.CreateProtector(
            "Dtudo2026",
            "ApiIdentity",
            "SecurityChallenge");
        _timeProvider = timeProvider;
        _options = options.Value;
        _sessionService = sessionService;
    }

    public async Task<IdentitySecurityChallenge?> CreateAsync<TPayload>(
        string accountId,
        string kind,
        TPayload payload,
        SecurityContext context,
        TimeSpan lifetime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId)
            || string.IsNullOrWhiteSpace(kind)
            || payload is null
            || lifetime <= TimeSpan.Zero
            || lifetime > TimeSpan.FromSeconds(_options.ChallengeLifetimeSeconds))
        {
            return null;
        }

        var normalizedContext = SecurityContextFactory.Normalize(context);
        if (!SecurityContextFactory.TryGetIds(normalizedContext, out _, out _)
            || !await _context.Users.AsNoTracking().AnyAsync(
            account => account.Id == accountId,
            cancellationToken))
        {
            return null;
        }

        if (!await _sessionService.IsActiveBindingAsync(accountId, normalizedContext, cancellationToken))
        {
            return null;
        }

        var createdAtUtc = _timeProvider.GetUtcNow();
        var challenge = new IdentitySecurityChallenge
        {
            Id = Guid.NewGuid(),
            AccountId = accountId,
            Kind = kind,
            ProtectedPayload = _protector.Protect(JsonSerializer.Serialize(payload)),
            SessionId = normalizedContext.SessionId,
            DeviceId = normalizedContext.DeviceId,
            CreatedAtUtc = createdAtUtc,
            ExpiresAtUtc = createdAtUtc.Add(lifetime)
        };
        _context.SecurityChallenges.Add(challenge);
        await _context.SaveChangesAsync(cancellationToken);
        return challenge;
    }

    public async Task<TPayload?> ReadAsync<TPayload>(
        Guid challengeId,
        string accountId,
        string kind,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var challenge = await FindActiveAsync(
            challengeId,
            accountId,
            kind,
            context,
            cancellationToken);
        if (challenge is null)
        {
            return default;
        }

        try
        {
            var json = _protector.Unprotect(challenge.ProtectedPayload);
            return JsonSerializer.Deserialize<TPayload>(json);
        }
        catch (CryptographicException)
        {
            return default;
        }
        catch (JsonException)
        {
            return default;
        }
    }

    public async Task<bool> ConsumeAsync(
        Guid challengeId,
        string accountId,
        string kind,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var challenge = await FindActiveAsync(
            challengeId,
            accountId,
            kind,
            context,
            cancellationToken);
        if (challenge is null)
        {
            return false;
        }

        challenge.ConsumedAtUtc = _timeProvider.GetUtcNow();
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

    public async Task<TPayload?> ReadAndConsumeAsync<TPayload>(
        Guid challengeId,
        string accountId,
        string kind,
        SecurityContext context,
        CancellationToken cancellationToken = default)
    {
        var challenge = await FindActiveAsync(
            challengeId,
            accountId,
            kind,
            context,
            cancellationToken);
        if (challenge is null)
        {
            return default;
        }

        TPayload? payload;
        try
        {
            var json = _protector.Unprotect(challenge.ProtectedPayload);
            payload = JsonSerializer.Deserialize<TPayload>(json);
        }
        catch (CryptographicException)
        {
            payload = default;
        }
        catch (JsonException)
        {
            payload = default;
        }

        if (payload is null)
        {
            challenge.ConsumedAtUtc = _timeProvider.GetUtcNow();
            try
            {
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateConcurrencyException)
            {
                _context.ChangeTracker.Clear();
            }

            return default;
        }

        challenge.ConsumedAtUtc = _timeProvider.GetUtcNow();
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return payload;
        }
        catch (DbUpdateConcurrencyException)
        {
            _context.ChangeTracker.Clear();
            return default;
        }
    }

    public async Task<bool> RevokeAsync(
        Guid challengeId,
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var challenge = await _context.SecurityChallenges.SingleOrDefaultAsync(
            item => item.Id == challengeId && item.AccountId == accountId,
            cancellationToken);
        if (challenge is null || challenge.ConsumedAtUtc is not null || challenge.RevokedAtUtc is not null)
        {
            return false;
        }

        challenge.RevokedAtUtc = _timeProvider.GetUtcNow();
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

    public Task<int> RevokeAllAsync(
        string accountId,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        return _context.SecurityChallenges
            .Where(item => item.AccountId == accountId
                && item.ConsumedAtUtc == null
                && item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(item => item.RevokedAtUtc, now), cancellationToken);
    }

    private async Task<IdentitySecurityChallenge?> FindActiveAsync(
        Guid challengeId,
        string accountId,
        string kind,
        SecurityContext context,
        CancellationToken cancellationToken)
    {
        var challenge = await _context.SecurityChallenges.SingleOrDefaultAsync(
            item => item.Id == challengeId
                && item.AccountId == accountId
                && item.Kind == kind,
            cancellationToken);
        if (challenge is null
            || challenge.ConsumedAtUtc is not null
            || challenge.RevokedAtUtc is not null
            || challenge.ExpiresAtUtc <= _timeProvider.GetUtcNow()
            || !MatchesContext(challenge, context)
            || !SecurityContextFactory.TryGetIds(
                SecurityContextFactory.Normalize(context),
                out _,
                out _)
            || !await _sessionService.IsActiveBindingAsync(
                accountId,
                SecurityContextFactory.Normalize(context),
                cancellationToken))
        {
            return null;
        }

        return challenge;
    }

    private static bool MatchesContext(IdentitySecurityChallenge challenge, SecurityContext context)
    {
        var normalizedContext = SecurityContextFactory.Normalize(context);
        return string.Equals(challenge.SessionId, normalizedContext.SessionId, StringComparison.Ordinal)
            && string.Equals(challenge.DeviceId, normalizedContext.DeviceId, StringComparison.Ordinal);
    }

}
