using System.Net;
using System.Net.Http.Json;

namespace WinAppDtudo.Services;

public sealed class ApiDiscogsHealthCheckService
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(8);
    private readonly WinAppAuthenticationService? _authenticationService;
    private readonly HttpClient _httpClient;
    private readonly bool _ownsHttpClient;

    public ApiDiscogsHealthCheckService(
        WinAppAuthenticationService? authenticationService = null,
        HttpClient? httpClient = null)
    {
        _authenticationService = authenticationService;
        _httpClient = httpClient ?? new HttpClient(AppConfigurationService.CreateHttpClientHandler());
        _ownsHttpClient = httpClient is null;
        _httpClient.BaseAddress ??= new Uri(
            AppConfigurationService.ApiDiscogsBaseUrl.TrimEnd('/') + "/",
            UriKind.Absolute);
    }

    public async Task<ApiDiscogsHealthStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var configuredBaseUrl = AppConfigurationService.ApiDiscogsBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var apiBaseUrl)
            || apiBaseUrl.Scheme is not ("https" or "http"))
        {
            return ApiDiscogsHealthStatus.Unavailable("A URL configurada da ApiDiscogs e invalida.");
        }

        var healthEndpoint = new Uri(apiBaseUrl, "ApiDiscogs/health");
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(HealthCheckTimeout);
            using var response = _authenticationService is null || !_authenticationService.IsAuthenticated
                ? await _httpClient.GetAsync(
                    healthEndpoint,
                    HttpCompletionOption.ResponseHeadersRead,
                    timeout.Token)
                : await _authenticationService.SendAuthenticatedAsync(
                    _httpClient,
                    _ => new HttpRequestMessage(HttpMethod.Get, healthEndpoint),
                    timeout.Token);

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return ApiDiscogsHealthStatus.Available("A ApiDiscogs respondeu e exige a sessao autorizada.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApiDiscogsHealthStatus.Unavailable(
                    $"A ApiDiscogs retornou HTTP {(int)response.StatusCode}.");
            }

            var health = await response.Content.ReadFromJsonAsync<ApiDiscogsHealthResponse>(timeout.Token);
            return health is not null
                && string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase)
                && string.Equals(health.Service, "ApiDiscogs", StringComparison.OrdinalIgnoreCase)
                ? ApiDiscogsHealthStatus.Available()
                : ApiDiscogsHealthStatus.Unavailable(
                    "A ApiDiscogs nao confirmou o estado operacional local.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ApiDiscogsHealthStatus.Unavailable(
                "O health check da ApiDiscogs excedeu o tempo limite.");
        }
        catch (HttpRequestException)
        {
            return ApiDiscogsHealthStatus.Unavailable("A ApiDiscogs nao esta acessivel.");
        }
        catch (NotSupportedException)
        {
            return ApiDiscogsHealthStatus.Unavailable(
                "A ApiDiscogs retornou uma resposta de health invalida.");
        }
    }

    public void Dispose()
    {
        if (_ownsHttpClient)
        {
            _httpClient.Dispose();
        }
    }

    private sealed record ApiDiscogsHealthResponse(string? Status, string? Service);
}

public sealed record ApiDiscogsHealthStatus(bool IsAvailable, string Message)
{
    public static ApiDiscogsHealthStatus Available(string message = "") => new(true, message);

    public static ApiDiscogsHealthStatus Unavailable(string message) => new(false, message);
}
