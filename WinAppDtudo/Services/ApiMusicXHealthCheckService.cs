using System.Net;
using System.Net.Http.Json;

namespace WinAppDtudo.Services;

public sealed class ApiMusicXHealthCheckService
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(8);
    private readonly WinAppAuthenticationService? _authenticationService;

    public ApiMusicXHealthCheckService(WinAppAuthenticationService? authenticationService = null)
    {
        _authenticationService = authenticationService;
    }

    public async Task<ApiMusicXHealthStatus> CheckAsync(CancellationToken cancellationToken = default)
    {
        var configuredBaseUrl = AppConfigurationService.ApiMusicXBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var apiBaseUrl)
            || apiBaseUrl.Scheme is not ("https" or "http"))
        {
            return ApiMusicXHealthStatus.Unavailable("A URL configurada da ApiMusicX e invalida.");
        }

        var healthEndpoint = new Uri(apiBaseUrl, "apiLocal/Health");

        try
        {
            using var handler = AppConfigurationService.CreateHttpClientHandler();
            using var client = new HttpClient(handler)
            {
                Timeout = HealthCheckTimeout
            };
            using var response = _authenticationService is null || !_authenticationService.IsAuthenticated
                ? await client.GetAsync(
                    healthEndpoint,
                    HttpCompletionOption.ResponseHeadersRead,
                    cancellationToken)
                : await _authenticationService.SendAuthenticatedAsync(
                    client,
                    _ => new HttpRequestMessage(HttpMethod.Get, healthEndpoint),
                    cancellationToken);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return ApiMusicXHealthStatus.Unavailable(
                    "A ApiMusicX esta em execucao, mas o banco local esta indisponivel.");
            }

            if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
            {
                return ApiMusicXHealthStatus.Available();
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApiMusicXHealthStatus.Unavailable(
                    $"A ApiMusicX retornou HTTP {(int)response.StatusCode}.");
            }

            var health = await response.Content.ReadFromJsonAsync<ApiMusicXHealthResponse>(cancellationToken);
            if (health is not null
                && string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase)
                && string.Equals(health.Database, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return ApiMusicXHealthStatus.Available();
            }

            return ApiMusicXHealthStatus.Unavailable(
                "A ApiMusicX nao confirmou a disponibilidade do banco local.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ApiMusicXHealthStatus.Unavailable("O health check da ApiMusicX excedeu o tempo limite.");
        }
        catch (HttpRequestException)
        {
            return ApiMusicXHealthStatus.Unavailable("A ApiMusicX nao esta acessivel.");
        }
        catch (NotSupportedException)
        {
            return ApiMusicXHealthStatus.Unavailable("A ApiMusicX retornou uma resposta de health invalida.");
        }
    }

    private sealed record ApiMusicXHealthResponse(string? Status, string? Service, string? Database);
}

public sealed record ApiMusicXHealthStatus(bool IsAvailable, string Message)
{
    public static ApiMusicXHealthStatus Available() => new(true, string.Empty);

    public static ApiMusicXHealthStatus Unavailable(string message) => new(false, message);
}
