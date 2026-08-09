using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace WinAppDtudo.Services;

public sealed record WinAppSessionInfo(
    Guid SessionId,
    Guid DeviceId,
    DateTimeOffset AccessTokenExpiresAtUtc,
    DateTimeOffset SessionExpiresAtUtc);

public sealed class WinAppAuthenticationService : IDisposable
{
    private const string RequiredWinAppRole = "Superadministrador";
    private const string RoleClaimName = "role";
    private const string LegacyRoleClaimName = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ProtectedTokenStore _tokenStore;
    private readonly WinAppPkceBrowserClient _browserClient;
    private readonly IApiIdentityStartupService? _identityStartupService;
    private readonly SemaphoreSlim _sessionGate = new(1, 1);
    private WinAppTokenSet? _tokenSet;
    private bool _disposed;

    public WinAppAuthenticationService(
        ProtectedTokenStore? tokenStore = null,
        WinAppPkceBrowserClient? browserClient = null,
        HttpClient? httpClient = null,
        IApiIdentityStartupService? identityStartupService = null)
    {
        _tokenStore = tokenStore ?? new ProtectedTokenStore();
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _httpClient.BaseAddress = new Uri(AppConfigurationService.ApiIdentityBaseUrl.TrimEnd('/') + "/", UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        _browserClient = browserClient ?? new WinAppPkceBrowserClient();
        _identityStartupService = identityStartupService
            ?? (httpClient is null ? new ApiIdentityStartupService() : null);
    }

    public bool IsAuthenticated => _tokenSet is not null;

    public WinAppSessionInfo? CurrentSession => _tokenSet is null
        ? null
        : new WinAppSessionInfo(
            _tokenSet.SessionId,
            _tokenSet.DeviceId,
            _tokenSet.AccessTokenExpiresAtUtc,
            _tokenSet.RefreshTokenExpiresAtUtc ?? _tokenSet.AccessTokenExpiresAtUtc);

    public async Task<WinAppSessionInfo> SignInAsync(CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            var stored = _tokenSet ?? await _tokenStore.LoadAsync(cancellationToken);
            if (IsSessionUsable(stored)
                && HasConfiguredResources(stored!.AccessToken)
                && HasRequiredRole(stored.AccessToken))
            {
                _tokenSet = stored;
                return CurrentSession!;
            }

            if (_identityStartupService is not null)
            {
                await _identityStartupService.EnsureReadyAsync(cancellationToken);
            }

            if (stored is not null
                && HasConfiguredResources(stored.AccessToken)
                && HasRequiredRole(stored.AccessToken)
                && !string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                var refreshed = await RefreshCoreAsync(stored, cancellationToken);
                if (refreshed is not null
                    && HasConfiguredResources(refreshed.AccessToken)
                    && HasRequiredRole(refreshed.AccessToken))
                {
                    _tokenSet = refreshed;
                    await _tokenStore.SaveAsync(refreshed, cancellationToken);
                    return CurrentSession!;
                }
            }

            await _tokenStore.ClearAsync();
            var fresh = await _browserClient.AuthenticateAsync(cancellationToken);
            if (!HasConfiguredResources(fresh.AccessToken)
                || !HasRequiredRole(fresh.AccessToken))
            {
                await _tokenStore.ClearAsync();
                throw new WinAppAuthenticationException(
                    "O WinAppDtudo aceita somente a conta com a role Superadministrador.");
            }

            var session = await CreateSecuritySessionAsync(fresh, cancellationToken);
            _tokenSet = fresh with
            {
                SessionId = session.SessionId,
                DeviceId = session.DeviceId
            };
            await _tokenStore.SaveAsync(_tokenSet, cancellationToken);
            return CurrentSession!;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            var stored = _tokenSet ?? await _tokenStore.LoadAsync(cancellationToken);
            if (IsAccessTokenUsable(stored)
                && HasRequiredRole(stored!.AccessToken))
            {
                _tokenSet = stored;
                return stored!.AccessToken;
            }

            if (stored is null || string.IsNullOrWhiteSpace(stored.RefreshToken))
            {
                throw new WinAppAuthenticationException("A sessao administrativa expirou.");
            }

            _tokenSet = stored;
            var refreshed = await RefreshCoreAsync(stored, cancellationToken);
            if (refreshed is null)
            {
                _tokenSet = null;
                await _tokenStore.ClearAsync();
                throw new WinAppAuthenticationException("A sessao administrativa nao pode ser renovada.");
            }

            if (!HasConfiguredResources(refreshed.AccessToken)
                || !HasRequiredRole(refreshed.AccessToken))
            {
                _tokenSet = null;
                await _tokenStore.ClearAsync();
                throw new WinAppAuthenticationException(
                    "O WinAppDtudo aceita somente a conta com a role Superadministrador.");
            }

            _tokenSet = refreshed;
            await _tokenStore.SaveAsync(refreshed, cancellationToken);
            return refreshed.AccessToken;
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    public async Task<HttpResponseMessage> SendAuthenticatedAsync(
        Func<string, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        return await SendAuthenticatedAsync(_httpClient, requestFactory, cancellationToken);
    }

    public async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpClient httpClient,
        Func<string, HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(requestFactory);
        var accessToken = await GetAccessTokenAsync(cancellationToken);
        using var request = requestFactory(accessToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var response = await httpClient.SendAsync(request, cancellationToken);
        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        await ForceRefreshAsync(accessToken, cancellationToken);
        var refreshedAccessToken = await GetAccessTokenAsync(cancellationToken);
        using var retry = requestFactory(refreshedAccessToken);
        retry.Headers.Authorization = new AuthenticationHeaderValue("Bearer", refreshedAccessToken);
        return await httpClient.SendAsync(retry, cancellationToken);
    }

    public async Task<bool> SignOutAsync(CancellationToken cancellationToken = default)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            ThrowIfDisposed();
            using var remoteCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            remoteCancellation.CancelAfter(TimeSpan.FromSeconds(8));
            var remoteCancellationToken = remoteCancellation.Token;
            try
            {
                var current = _tokenSet ?? await _tokenStore.LoadAsync(cancellationToken);
                var hadLocalState = current is not null;
                if (current is not null
                    && current.SessionId != Guid.Empty
                    && !IsAccessTokenUsable(current)
                    && !string.IsNullOrWhiteSpace(current.RefreshToken))
                {
                    try
                    {
                        var refreshed = await RefreshCoreAsync(current, remoteCancellationToken);
                        if (refreshed is not null)
                        {
                            current = refreshed;
                            _tokenSet = refreshed;
                        }
                    }
                    catch (HttpRequestException)
                    {
                    }
                    catch (OperationCanceledException)
                    {
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }

                var oidcRevoked = current is null || string.IsNullOrWhiteSpace(current.RefreshToken);
                if (current is not null && !string.IsNullOrWhiteSpace(current.RefreshToken))
                {
                    try
                    {
                        oidcRevoked = await RevokeRefreshTokenAsync(current.RefreshToken, remoteCancellationToken);
                    }
                    catch (HttpRequestException)
                    {
                        oidcRevoked = false;
                    }
                    catch (OperationCanceledException)
                    {
                        oidcRevoked = false;
                    }
                    catch (ObjectDisposedException)
                    {
                        oidcRevoked = false;
                    }
                }

                var sessionRevoked = current is null || current.SessionId == Guid.Empty;
                if (current is not null
                    && current.SessionId != Guid.Empty
                    && IsAccessTokenUsable(current))
                {
                    try
                    {
                        using var request = new HttpRequestMessage(
                            HttpMethod.Delete,
                            $"identity/security/sessions/{current.SessionId:D}");
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", current.AccessToken);
                        using var response = await _httpClient.SendAsync(request, remoteCancellationToken);
                        sessionRevoked = response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.NotFound;
                    }
                    catch (HttpRequestException)
                    {
                        sessionRevoked = false;
                    }
                    catch (OperationCanceledException)
                    {
                        sessionRevoked = false;
                    }
                    catch (ObjectDisposedException)
                    {
                        sessionRevoked = false;
                    }
                }

                return hadLocalState && oidcRevoked && sessionRevoked;
            }
            finally
            {
                _tokenSet = null;
                await _tokenStore.ClearAsync();
            }
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private async Task<WinAppTokenSet?> RefreshCoreAsync(
        WinAppTokenSet current,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("grant_type", "refresh_token"),
            new KeyValuePair<string, string>("client_id", AppConfigurationService.IdentityClientId),
            new KeyValuePair<string, string>("refresh_token", current.RefreshToken!)
        ]);
        using var response = await _httpClient.PostAsync("connect/token", content, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenRefreshResponse>(JsonOptions, cancellationToken);
        if (payload is null
            || string.IsNullOrWhiteSpace(payload.AccessToken)
            || string.IsNullOrWhiteSpace(payload.RefreshToken)
            || payload.ExpiresIn is < 60)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var refreshed = current with
        {
            AccessToken = payload.AccessToken,
            RefreshToken = payload.RefreshToken,
            AccessTokenExpiresAtUtc = now.AddSeconds(payload.ExpiresIn),
            RefreshTokenExpiresAtUtc = payload.RefreshExpiresIn is > 0
                ? now.AddSeconds(payload.RefreshExpiresIn.Value)
                : current.RefreshTokenExpiresAtUtc
        };
        if (!await BindAccessTokenAsync(refreshed, cancellationToken))
        {
            return null;
        }

        return refreshed;
    }

    private async Task<bool> BindAccessTokenAsync(
        WinAppTokenSet tokenSet,
        CancellationToken cancellationToken)
    {
        if (tokenSet.SessionId == Guid.Empty || tokenSet.DeviceId == Guid.Empty)
        {
            return false;
        }

        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            $"identity/security/sessions/{tokenSet.SessionId:D}/token")
        {
            Content = JsonContent.Create(new { DeviceId = tokenSet.DeviceId })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSet.AccessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task<bool> RevokeRefreshTokenAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        using var content = new FormUrlEncodedContent(
        [
            new KeyValuePair<string, string>("client_id", AppConfigurationService.IdentityClientId),
            new KeyValuePair<string, string>("token", refreshToken),
            new KeyValuePair<string, string>("token_type_hint", "refresh_token")
        ]);
        using var response = await _httpClient.PostAsync("connect/revocation", content, cancellationToken);
        return response.IsSuccessStatusCode;
    }

    private async Task ForceRefreshAsync(string previousAccessToken, CancellationToken cancellationToken)
    {
        await _sessionGate.WaitAsync(cancellationToken);
        try
        {
            var current = _tokenSet ?? await _tokenStore.LoadAsync(cancellationToken);
            if (current is null || !string.Equals(current.AccessToken, previousAccessToken, StringComparison.Ordinal))
            {
                return;
            }

            var refreshed = await RefreshCoreAsync(current, cancellationToken);
            if (refreshed is null
                || !HasConfiguredResources(refreshed.AccessToken)
                || !HasRequiredRole(refreshed.AccessToken))
            {
                _tokenSet = null;
                await _tokenStore.ClearAsync();
                return;
            }

            _tokenSet = refreshed;
            await _tokenStore.SaveAsync(refreshed, cancellationToken);
        }
        finally
        {
            _sessionGate.Release();
        }
    }

    private async Task<WinAppTokenSet> CreateSecuritySessionAsync(
        WinAppTokenSet tokenSet,
        CancellationToken cancellationToken)
    {
        var deviceName = $"WinAppDtudo - {Environment.MachineName}";
        using var request = new HttpRequestMessage(HttpMethod.Post, "identity/security/sessions")
        {
            Content = JsonContent.Create(new { Name = deviceName })
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenSet.AccessToken);
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new WinAppAuthenticationException("A sessao segura do WinApp nao foi criada.");
        }

        var session = await response.Content.ReadFromJsonAsync<SecuritySessionCreationResponse>(JsonOptions, cancellationToken);
        if (session is null || session.SessionId == Guid.Empty || session.DeviceId == Guid.Empty)
        {
            throw new WinAppAuthenticationException("A resposta de sessao do Identity e invalida.");
        }

        return tokenSet with
        {
            SessionId = session.SessionId,
            DeviceId = session.DeviceId
        };
    }

    private static bool IsSessionUsable(WinAppTokenSet? tokenSet) =>
        tokenSet is not null
        && !string.IsNullOrWhiteSpace(tokenSet.AccessToken)
        && !string.IsNullOrWhiteSpace(tokenSet.RefreshToken)
        && tokenSet.SessionId != Guid.Empty
        && tokenSet.DeviceId != Guid.Empty
        && (tokenSet.RefreshTokenExpiresAtUtc is not { } refreshExpiresAtUtc
            || refreshExpiresAtUtc > DateTimeOffset.UtcNow);

    private static bool IsAccessTokenUsable(WinAppTokenSet? tokenSet) =>
        IsSessionUsable(tokenSet)
        && tokenSet!.AccessTokenExpiresAtUtc > DateTimeOffset.UtcNow.AddSeconds(30);

    private static bool HasConfiguredResources(string accessToken)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return false;
        }

        var segments = accessToken.Split('.');
        if (segments.Length != 3)
        {
            return true;
        }

        try
        {
            var payload = segments[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            using var document = JsonDocument.Parse(Convert.FromBase64String(payload));
            if (!document.RootElement.TryGetProperty("aud", out var audienceElement))
            {
                return false;
            }

            var audiences = audienceElement.ValueKind == JsonValueKind.Array
                ? audienceElement.EnumerateArray()
                    .Where(element => element.ValueKind == JsonValueKind.String)
                    .Select(element => element.GetString()!)
                    .ToHashSet(StringComparer.Ordinal)
                : audienceElement.ValueKind == JsonValueKind.String
                    ? [audienceElement.GetString()!]
                    : [];

            return AppConfigurationService.IdentityResources
                .All(resource => audiences.Contains(resource));
        }
        catch (FormatException)
        {
            return true;
        }
        catch (JsonException)
        {
            return true;
        }
    }

    private static bool HasRequiredRole(string accessToken)
    {
        using var document = TryReadJwtPayload(accessToken);
        if (document is null)
        {
            return false;
        }

        return HasClaimValue(document.RootElement, RoleClaimName, RequiredWinAppRole)
            || HasClaimValue(document.RootElement, LegacyRoleClaimName, RequiredWinAppRole);
    }

    private static JsonDocument? TryReadJwtPayload(string accessToken)
    {
        var segments = accessToken.Split('.');
        if (segments.Length != 3)
        {
            return null;
        }

        try
        {
            var payload = segments[1].Replace('-', '+').Replace('_', '/');
            payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
            return JsonDocument.Parse(Convert.FromBase64String(payload));
        }
        catch (FormatException)
        {
            return null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool HasClaimValue(
        JsonElement payload,
        string claimName,
        string expectedValue)
    {
        if (!payload.TryGetProperty(claimName, out var claim))
        {
            return false;
        }

        return claim.ValueKind switch
        {
            JsonValueKind.String => string.Equals(
                claim.GetString(),
                expectedValue,
                StringComparison.Ordinal),
            JsonValueKind.Array => claim.EnumerateArray().Any(item =>
                item.ValueKind == JsonValueKind.String
                && string.Equals(item.GetString(), expectedValue, StringComparison.Ordinal)),
            _ => false
        };
    }

    private void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(WinAppAuthenticationService));
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _httpClient.Dispose();
    }

    private sealed class TokenRefreshResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("refresh_token")]
        public string? RefreshToken { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonPropertyName("refresh_expires_in")]
        public int? RefreshExpiresIn { get; set; }
    }

    private sealed class SecuritySessionCreationResponse
    {
        public Guid DeviceId { get; set; }
        public Guid SessionId { get; set; }
    }
}
