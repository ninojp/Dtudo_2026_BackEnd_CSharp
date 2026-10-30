using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace DtudoGateway.Infrastructure;

public enum TokenRefreshOutcome
{
    /// <summary>Novos tokens emitidos pela ApiIdentity.</summary>
    Succeeded,

    /// <summary>A ApiIdentity recusou o refresh token (invalid_grant): sessao sem renovacao possivel.</summary>
    Rejected,

    /// <summary>Falha transitoria (rede, 5xx, metadados indisponiveis): tentar novamente depois.</summary>
    Unavailable
}

public sealed record TokenRefreshResult(
    TokenRefreshOutcome Outcome,
    string? AccessToken = null,
    string? RefreshToken = null,
    string? IdToken = null,
    DateTimeOffset? ExpiresAt = null);

public interface IGatewayTokenRefresher
{
    Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken);
}

/// <summary>
/// Renova tokens no endpoint de token da ApiIdentity usando o refresh token guardado na sessao do gateway.
/// Requisicoes paralelas da mesma sessao compartilham uma unica renovacao (o refresh token e rotativo).
/// </summary>
public sealed class GatewayTokenRefresher(
    IOptionsMonitor<OpenIdConnectOptions> oidcOptions,
    TimeProvider timeProvider,
    ILogger<GatewayTokenRefresher> logger) : IGatewayTokenRefresher
{
    private static readonly TimeSpan SharedResultLifetime = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan EndpointTimeout = TimeSpan.FromSeconds(10);

    private readonly ConcurrentDictionary<string, SharedRefresh> _inFlightOrRecent = new(StringComparer.Ordinal);

    public async Task<TokenRefreshResult> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return new TokenRefreshResult(TokenRefreshOutcome.Rejected);
        }

        var now = timeProvider.GetUtcNow();
        foreach (var expired in _inFlightOrRecent.Where(entry => entry.Value.ExpiresAt <= now))
        {
            _inFlightOrRecent.TryRemove(expired);
        }

        var key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken)));
        var shared = _inFlightOrRecent.GetOrAdd(key, _ => new SharedRefresh(
            new Lazy<Task<TokenRefreshResult>>(
                () => RequestNewTokensAsync(refreshToken),
                LazyThreadSafetyMode.ExecutionAndPublication),
            now + SharedResultLifetime));

        var result = await shared.Result.Value.WaitAsync(cancellationToken);
        if (result.Outcome != TokenRefreshOutcome.Succeeded)
        {
            _inFlightOrRecent.TryRemove(new KeyValuePair<string, SharedRefresh>(key, shared));
        }

        return result;
    }

    private sealed record SharedRefresh(Lazy<Task<TokenRefreshResult>> Result, DateTimeOffset ExpiresAt);

    public async Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        try
        {
            var options = oidcOptions.Get(GatewayAuthenticationSchemes.OpenIdConnect);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(EndpointTimeout);

            var configuration = await options.ConfigurationManager!.GetConfigurationAsync(timeout.Token);
            var revocationEndpoint = ResolveRevocationEndpoint(configuration);
            if (revocationEndpoint is null)
            {
                return;
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, revocationEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["token"] = refreshToken,
                    ["token_type_hint"] = "refresh_token",
                    ["client_id"] = options.ClientId!,
                    ["client_secret"] = options.ClientSecret!
                })
            };
            using var response = await options.Backchannel.SendAsync(request, timeout.Token);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "A ApiIdentity nao revogou o refresh token da sessao encerrada (HTTP {StatusCode}).",
                    (int)response.StatusCode);
            }
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException)
        {
            logger.LogWarning(exception, "Falha ao revogar o refresh token da sessao encerrada.");
        }
    }

    private async Task<TokenRefreshResult> RequestNewTokensAsync(string refreshToken)
    {
        try
        {
            var options = oidcOptions.Get(GatewayAuthenticationSchemes.OpenIdConnect);
            using var timeout = new CancellationTokenSource(EndpointTimeout);

            var configuration = await options.ConfigurationManager!.GetConfigurationAsync(timeout.Token);
            if (string.IsNullOrWhiteSpace(configuration.TokenEndpoint))
            {
                logger.LogError("Os metadados OIDC nao informam o token endpoint; o refresh da sessao nao foi feito.");
                return new TokenRefreshResult(TokenRefreshOutcome.Unavailable);
            }

            using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>(StringComparer.Ordinal)
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = refreshToken,
                    ["client_id"] = options.ClientId!,
                    ["client_secret"] = options.ClientSecret!
                })
            };
            using var response = await options.Backchannel.SendAsync(request, timeout.Token);
            var body = await response.Content.ReadAsStringAsync(timeout.Token);

            if (!response.IsSuccessStatusCode)
            {
                return ClassifyFailure(response.StatusCode, body);
            }

            return ParseSuccess(body);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or InvalidOperationException or JsonException)
        {
            logger.LogWarning(exception, "Falha transitoria ao renovar os tokens da sessao.");
            return new TokenRefreshResult(TokenRefreshOutcome.Unavailable);
        }
    }

    private TokenRefreshResult ClassifyFailure(System.Net.HttpStatusCode statusCode, string body)
    {
        if (statusCode == System.Net.HttpStatusCode.BadRequest
            && TryReadProperty(body, "error", out var error)
            && string.Equals(error, "invalid_grant", StringComparison.Ordinal))
        {
            return new TokenRefreshResult(TokenRefreshOutcome.Rejected);
        }

        logger.LogWarning(
            "A ApiIdentity recusou o refresh da sessao com HTTP {StatusCode}; a sessao foi mantida.",
            (int)statusCode);
        return new TokenRefreshResult(TokenRefreshOutcome.Unavailable);
    }

    private TokenRefreshResult ParseSuccess(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;
        var accessToken = GetString(root, "access_token");
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            logger.LogWarning("A resposta de refresh da ApiIdentity nao contem access_token.");
            return new TokenRefreshResult(TokenRefreshOutcome.Unavailable);
        }

        var lifetimeSeconds = root.TryGetProperty("expires_in", out var expiresIn)
            && expiresIn.ValueKind == JsonValueKind.Number
            && expiresIn.TryGetInt32(out var seconds)
            && seconds > 0
                ? seconds
                : 300;

        return new TokenRefreshResult(
            TokenRefreshOutcome.Succeeded,
            accessToken,
            GetString(root, "refresh_token"),
            GetString(root, "id_token"),
            timeProvider.GetUtcNow().AddSeconds(lifetimeSeconds));
    }

    private static Uri? ResolveRevocationEndpoint(Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration configuration)
    {
        if (configuration.AdditionalData.TryGetValue("revocation_endpoint", out var advertised)
            && Uri.TryCreate(advertised?.ToString(), UriKind.Absolute, out var advertisedUri))
        {
            return advertisedUri;
        }

        return Uri.TryCreate(configuration.TokenEndpoint, UriKind.Absolute, out var tokenUri)
            && Uri.TryCreate(tokenUri, "revocation", out var derived)
                ? derived
                : null;
    }

    private static string? GetString(JsonElement element, string name) =>
        element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool TryReadProperty(string json, string name, out string? value)
    {
        value = null;
        try
        {
            using var document = JsonDocument.Parse(json);
            value = GetString(document.RootElement, name);
            return value is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}
