using System.Security.Cryptography;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Distributed;

namespace DtudoGateway.Infrastructure;

public sealed class ServerSideTicketStore(
    IDistributedCache cache,
    TimeProvider timeProvider) : ITicketStore
{
    private const string CacheKeyPrefix = "dtudo-gateway:auth-ticket:";

    public async Task<string> StoreAsync(AuthenticationTicket ticket)
    {
        var key = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        await SaveAsync(key, ticket);
        return key;
    }

    public Task RenewAsync(string key, AuthenticationTicket ticket) => SaveAsync(key, ticket);

    public async Task<AuthenticationTicket?> RetrieveAsync(string key)
    {
        var serializedTicket = await cache.GetAsync(GetCacheKey(key));
        return serializedTicket is null
            ? null
            : TicketSerializer.Default.Deserialize(serializedTicket);
    }

    public Task RemoveAsync(string key) => cache.RemoveAsync(GetCacheKey(key));

    private async Task SaveAsync(string key, AuthenticationTicket ticket)
    {
        var expiration = ticket.Properties.ExpiresUtc
            ?? timeProvider.GetUtcNow().AddDays(30);
        var lifetime = expiration - timeProvider.GetUtcNow();
        if (lifetime <= TimeSpan.Zero)
        {
            await RemoveAsync(key);
            return;
        }

        await cache.SetAsync(
            GetCacheKey(key),
            TicketSerializer.Default.Serialize(ticket),
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = lifetime
            });
    }

    private static string GetCacheKey(string key) => CacheKeyPrefix + key;
}
