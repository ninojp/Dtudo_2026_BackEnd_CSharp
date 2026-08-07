using System.Security.Cryptography;
using System.Text;
using ApiIdentity.Configuration;
using ApiIdentity.Data;
using ApiIdentity.Models;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.EntityFrameworkCore.Storage;

namespace ApiIdentity.Mfa;

public sealed class SecurityTokenService
{
    private readonly IdentityDbContext _context;
    private readonly SecuritySessionService _sessionService;
    private readonly IdentitySecurityAuditWriter _auditWriter;
    private readonly TimeProvider _timeProvider;
    private readonly IdentitySessionOptions _options;

    public SecurityTokenService(
        IdentityDbContext context,
        SecuritySessionService sessionService,
        IdentitySecurityAuditWriter auditWriter,
        TimeProvider timeProvider,
        IOptions<IdentitySessionOptions> options)
    {
        _context = context;
        _sessionService = sessionService;
        _auditWriter = auditWriter;
        _timeProvider = timeProvider;
        _options = options.Value;
    }

    public async Task<SecurityTokenPair?> IssueAsync(
        string accountId,
        string deviceName,
        string actor,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var session = await _sessionService.CreateAsync(
            accountId,
            deviceName,
            actor,
            cancellationToken);
        if (session is null)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        var familyId = Guid.NewGuid();
        var pair = CreateTokenPair(
            session.DeviceId,
            session.SessionId,
            session.SessionExpiresAtUtc,
            _timeProvider.GetUtcNow());
        _context.SecurityTokens.AddRange(
            CreateToken(
                pair.AccessToken,
                IdentitySecurityTokenTypes.Access,
                accountId,
                pair.SessionId,
                pair.DeviceId,
                familyId,
                pair.AccessTokenExpiresAtUtc,
                pair.SessionExpiresAtUtc),
            CreateToken(
                pair.RefreshToken,
                IdentitySecurityTokenTypes.Refresh,
                accountId,
                pair.SessionId,
                pair.DeviceId,
                familyId,
                pair.RefreshTokenExpiresAtUtc,
                pair.SessionExpiresAtUtc));
        _auditWriter.Record(
            actor,
            "identity.security.tokens.issued",
            $"session:{pair.SessionId:D}",
            "succeeded",
            pair.DeviceId.ToString("D"),
            "opaque-access-and-refresh-tokens-issued");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return pair;
    }

    public async Task<SecurityTokenRefreshResult> RefreshAsync(
        string refreshToken,
        string actor,
        CancellationToken cancellationToken = default)
    {
        if (!IsCandidateToken(refreshToken))
        {
            return new SecurityTokenRefreshResult(SecurityTokenRefreshStatus.Invalid);
        }

        var tokenHash = HashToken(refreshToken);
        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        var storedToken = await _context.SecurityTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                token => token.TokenHash == tokenHash
                    && token.TokenType == IdentitySecurityTokenTypes.Refresh,
                cancellationToken);
        if (storedToken is null)
        {
            return await CompleteFailureAsync(
                transaction,
                actor,
                "identity.security.refresh.rejected",
                "refresh-token",
                "invalid-refresh-token",
                SecurityTokenRefreshStatus.Invalid,
                cancellationToken);
        }

        var now = _timeProvider.GetUtcNow();
        if (storedToken.UsedAtUtc is not null)
        {
            await RevokeFamilyAsync(storedToken, actor, now, cancellationToken);
            _auditWriter.Record(
                actor,
                "identity.security.refresh.reuse-detected",
                $"session:{storedToken.SessionId:D}",
                "blocked",
                storedToken.DeviceId.ToString("D"),
                "refresh-token-reuse-revoked-family-and-session");
            await _context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return new SecurityTokenRefreshResult(SecurityTokenRefreshStatus.ReuseDetected);
        }

        if (storedToken.RevokedAtUtc is not null)
        {
            return await CompleteFailureAsync(
                transaction,
                actor,
                "identity.security.refresh.rejected",
                $"session:{storedToken.SessionId:D}",
                "revoked-refresh-token",
                SecurityTokenRefreshStatus.Revoked,
                cancellationToken,
                storedToken.DeviceId);
        }

        if (storedToken.ExpiresAtUtc <= now)
        {
            return await CompleteFailureAsync(
                transaction,
                actor,
                "identity.security.refresh.rejected",
                $"session:{storedToken.SessionId:D}",
                "expired-refresh-token",
                SecurityTokenRefreshStatus.Expired,
                cancellationToken,
                storedToken.DeviceId);
        }

        var sessionIsActive = await _sessionService.IsActiveBindingAsync(
            storedToken.AccountId,
            SecurityContextFactory.FromIds(storedToken.SessionId, storedToken.DeviceId),
            cancellationToken);
        if (!sessionIsActive)
        {
            await _context.SecurityTokens
                .Where(token => token.Id == storedToken.Id && token.RevokedAtUtc == null)
                .ExecuteUpdateAsync(
                    setters => setters.SetProperty(token => token.RevokedAtUtc, now),
                    cancellationToken);
            return await CompleteFailureAsync(
                transaction,
                actor,
                "identity.security.refresh.rejected",
                $"session:{storedToken.SessionId:D}",
                "inactive-session-or-device",
                SecurityTokenRefreshStatus.SessionInactive,
                cancellationToken,
                storedToken.DeviceId);
        }

        var session = await _context.SecuritySessions
            .AsNoTracking()
            .SingleAsync(
                item => item.Id == storedToken.SessionId
                    && item.AccountId == storedToken.AccountId
                    && item.DeviceId == storedToken.DeviceId,
                cancellationToken);
        var replacementRefreshTokenId = Guid.NewGuid();
        var consumed = await _context.SecurityTokens
            .Where(token => token.Id == storedToken.Id
                && token.TokenType == IdentitySecurityTokenTypes.Refresh
                && token.UsedAtUtc == null
                && token.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(token => token.UsedAtUtc, now)
                    .SetProperty(token => token.ReplacedByTokenId, replacementRefreshTokenId),
                cancellationToken);
        if (consumed != 1)
        {
            var currentToken = await _context.SecurityTokens
                .AsNoTracking()
                .SingleAsync(token => token.Id == storedToken.Id, cancellationToken);
            if (currentToken.UsedAtUtc is not null)
            {
                await RevokeFamilyAsync(currentToken, actor, now, cancellationToken);
                _auditWriter.Record(
                    actor,
                    "identity.security.refresh.reuse-detected",
                    $"session:{currentToken.SessionId:D}",
                    "blocked",
                    currentToken.DeviceId.ToString("D"),
                    "concurrent-refresh-reuse-revoked-family-and-session");
                await _context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return new SecurityTokenRefreshResult(SecurityTokenRefreshStatus.ReuseDetected);
            }

            return await CompleteFailureAsync(
                transaction,
                actor,
                "identity.security.refresh.rejected",
                $"session:{storedToken.SessionId:D}",
                "refresh-token-state-changed",
                SecurityTokenRefreshStatus.Revoked,
                cancellationToken,
                storedToken.DeviceId);
        }

        var pair = CreateTokenPair(
            storedToken.DeviceId,
            storedToken.SessionId,
            session.ExpiresAtUtc,
            now);
        _context.SecurityTokens.AddRange(
            CreateToken(
                pair.AccessToken,
                IdentitySecurityTokenTypes.Access,
                storedToken.AccountId,
                pair.SessionId,
                pair.DeviceId,
                storedToken.FamilyId,
                pair.AccessTokenExpiresAtUtc,
                pair.SessionExpiresAtUtc),
            CreateToken(
                pair.RefreshToken,
                IdentitySecurityTokenTypes.Refresh,
                storedToken.AccountId,
                pair.SessionId,
                pair.DeviceId,
                storedToken.FamilyId,
                pair.RefreshTokenExpiresAtUtc,
                pair.SessionExpiresAtUtc,
                replacementRefreshTokenId));
        _auditWriter.Record(
            actor,
            "identity.security.refresh.rotated",
            $"session:{pair.SessionId:D}",
            "succeeded",
            pair.DeviceId.ToString("D"),
            "refresh-token-rotated");
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SecurityTokenRefreshResult(SecurityTokenRefreshStatus.Succeeded, pair);
    }

    public async Task<SecurityAccessTokenInfo?> IntrospectAccessTokenAsync(
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        if (!IsCandidateToken(accessToken))
        {
            return null;
        }

        var now = _timeProvider.GetUtcNow();
        var token = await _context.SecurityTokens
            .AsNoTracking()
            .SingleOrDefaultAsync(
                item => item.TokenHash == HashToken(accessToken)
                    && item.TokenType == IdentitySecurityTokenTypes.Access
                    && item.RevokedAtUtc == null
                    && item.ExpiresAtUtc > now,
                cancellationToken);
        if (token is null)
        {
            return null;
        }

        var active = await _sessionService.IsActiveBindingAsync(
            token.AccountId,
            SecurityContextFactory.FromIds(token.SessionId, token.DeviceId),
            cancellationToken);
        return active
            ? new SecurityAccessTokenInfo(
                token.AccountId,
                token.DeviceId,
                token.SessionId,
                token.ExpiresAtUtc)
            : null;
    }

    private async Task RevokeFamilyAsync(
        IdentitySecurityToken token,
        string actor,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        await _context.SecurityTokens
            .Where(item => item.AccountId == token.AccountId
                && item.FamilyId == token.FamilyId
                && item.RevokedAtUtc == null)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(item => item.RevokedAtUtc, now),
                cancellationToken);
        await _sessionService.RevokeSessionAsync(
            token.AccountId,
            token.SessionId,
            actor,
            cancellationToken);
    }

    private async Task<SecurityTokenRefreshResult> CompleteFailureAsync(
        IDbContextTransaction transaction,
        string actor,
        string action,
        string target,
        string reason,
        SecurityTokenRefreshStatus status,
        CancellationToken cancellationToken,
        Guid? deviceId = null)
    {
        _auditWriter.Record(
            string.IsNullOrWhiteSpace(actor) ? "unknown" : actor,
            action,
            target,
            "blocked",
            deviceId?.ToString("D") ?? "unknown-device",
            reason);
        await _context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new SecurityTokenRefreshResult(status);
    }

    private SecurityTokenPair CreateTokenPair(
        Guid deviceId,
        Guid sessionId,
        DateTimeOffset sessionExpiresAtUtc,
        DateTimeOffset now)
    {
        var accessToken = CreateOpaqueToken();
        var refreshToken = CreateOpaqueToken();
        var accessExpiresAtUtc = Min(
            now.AddSeconds(_options.AccessTokenLifetimeSeconds),
            sessionExpiresAtUtc);
        var refreshExpiresAtUtc = Min(
            now.AddDays(_options.RefreshTokenLifetimeDays),
            sessionExpiresAtUtc);
        return new SecurityTokenPair(
            deviceId,
            sessionId,
            accessToken,
            refreshToken,
            accessExpiresAtUtc,
            refreshExpiresAtUtc,
            sessionExpiresAtUtc);
    }

    private IdentitySecurityToken CreateToken(
        string rawToken,
        string tokenType,
        string accountId,
        Guid sessionId,
        Guid deviceId,
        Guid familyId,
        DateTimeOffset expiresAtUtc,
        DateTimeOffset sessionExpiresAtUtc,
        Guid? tokenId = null)
    {
        return new IdentitySecurityToken
        {
            Id = tokenId ?? Guid.NewGuid(),
            AccountId = accountId,
            SessionId = sessionId,
            DeviceId = deviceId,
            FamilyId = familyId,
            TokenHash = HashToken(rawToken),
            TokenType = tokenType,
            CreatedAtUtc = _timeProvider.GetUtcNow(),
            ExpiresAtUtc = Min(expiresAtUtc, sessionExpiresAtUtc)
        };
    }

    private string CreateOpaqueToken() => WebEncoders.Base64UrlEncode(
        RandomNumberGenerator.GetBytes(_options.TokenEntropyBytes));

    private static bool IsCandidateToken(string? token) =>
        !string.IsNullOrWhiteSpace(token)
        && token.Length is >= 20 and <= 512;

    private static string HashToken(string token) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private static DateTimeOffset Min(DateTimeOffset first, DateTimeOffset second) =>
        first <= second ? first : second;

}
