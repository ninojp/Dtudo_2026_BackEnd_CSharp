using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using DtudoGateway.Configuration;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace DtudoGateway.Infrastructure;

public sealed class ServerSideTicketStore(
    IDistributedCache cache,
    TimeProvider timeProvider,
    IOptions<GatewayOptions> gatewayOptions) : ITicketStore
{
    private const string CacheKeyPrefix = "dtudo-gateway:auth-ticket:";
    private const string SubjectIndexKeyPrefix = "dtudo-gateway:auth-subject:";
    public const string AbsoluteIssuedAtProperty = "dtudo:absolute-issued-at";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        EnsureIssuedAt(ticket);
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var saved = await SaveAsync(key, ticket);
        if (!saved)
        {
            throw new InvalidOperationException("Nao foi possivel criar uma sessao autenticada expirada.");
        }

        var subject = GetSubject(ticket);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var subjectIndexKey = GetSubjectIndexKey(subject);
            var previousKey = await cache.GetStringAsync(subjectIndexKey);
            await cache.SetStringAsync(subjectIndexKey, key, CreateCacheOptions(ticket));
            if (!string.IsNullOrWhiteSpace(previousKey) && !string.Equals(previousKey, key, StringComparison.Ordinal))
            {
                await RemoveAsync(previousKey);
            }
        }

        return key;
    }

    public async Task RenewAsync(string key, AuthenticationTicket ticket)
    {
        await SaveAsync(key, ticket);
    }

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var serializedTicket = await cache.GetAsync(GetCacheKey(key));
        return serializedTicket is null
            ? null
            : TicketSerializer.Default.Deserialize(serializedTicket);
    }

    public async Task RemoveAsync(string key)
    {
        var ticket = await RetrieveAsync(key);
        await cache.RemoveAsync(GetCacheKey(key));

        var subject = ticket is null ? null : GetSubject(ticket);
        if (!string.IsNullOrWhiteSpace(subject))
        {
            var subjectIndexKey = GetSubjectIndexKey(subject);
            var currentKey = await cache.GetStringAsync(subjectIndexKey);
            if (string.Equals(currentKey, key, StringComparison.Ordinal))
            {
                await cache.RemoveAsync(subjectIndexKey);
            }
        }
    }

    private async Task<bool> SaveAsync(string key, AuthenticationTicket ticket)
    {
        var now = timeProvider.GetUtcNow();
        EnsureIssuedAt(ticket, now);
        var idleExpiration = ticket.Properties.ExpiresUtc
            ?? now.AddMinutes(gatewayOptions.Value.SessionIdleTimeoutMinutes);
        var absoluteExpiration = GetIssuedAt(ticket).AddHours(gatewayOptions.Value.SessionAbsoluteLifetimeHours);
        var expiration = idleExpiration <= absoluteExpiration ? idleExpiration : absoluteExpiration;
        var lifetime = expiration - now;
        if (lifetime <= TimeSpan.Zero)
        {
            await cache.RemoveAsync(GetCacheKey(key));
            return false;
        }

        await cache.SetAsync(
            GetCacheKey(key),
            TicketSerializer.Default.Serialize(ticket),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            });
        return true;
    }

    private DistributedCacheEntryOptions CreateCacheOptions(AuthenticationTicket ticket)
    {
        var now = timeProvider.GetUtcNow();
        var idleExpiration = ticket.Properties.ExpiresUtc
            ?? now.AddMinutes(gatewayOptions.Value.SessionIdleTimeoutMinutes);
        var absoluteExpiration = GetIssuedAt(ticket).AddHours(gatewayOptions.Value.SessionAbsoluteLifetimeHours);
        var expiration = idleExpiration <= absoluteExpiration ? idleExpiration : absoluteExpiration;
        return new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = expiration - now
        };
    }

    private void EnsureIssuedAt(AuthenticationTicket ticket, DateTimeOffset? now = null)
    {
        if (!ticket.Properties.Items.ContainsKey(AbsoluteIssuedAtProperty))
        {
            ticket.Properties.Items[AbsoluteIssuedAtProperty] =
                (now ?? timeProvider.GetUtcNow()).ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        }
    }

    private DateTimeOffset GetIssuedAt(AuthenticationTicket ticket)
    {
        if (ticket.Properties.Items.TryGetValue(AbsoluteIssuedAtProperty, out var value)
            && long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var unixTime))
        {
            return DateTimeOffset.FromUnixTimeSeconds(unixTime);
        }

        return timeProvider.GetUtcNow();
    }

    private static string? GetSubject(AuthenticationTicket ticket) =>
        ticket.Principal.FindFirst("sub")?.Value;

    private static string GetCacheKey(string key) => CacheKeyPrefix + key;

    private static string GetSubjectIndexKey(string subject) =>
        SubjectIndexKeyPrefix + WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.UTF8.GetBytes(subject)));
}
