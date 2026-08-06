using System.Net;
using System.Net.Http.Json;

namespace WinAppDtudo.Services;

public sealed class ApiMyAnimesHealthCheckService
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(8);

    public async Task<ApiMyAnimesHealthStatus> CheckAsync(CancellationToken cancellationToken)
    {
        var configuredBaseUrl = AppConfigurationService.ApiMyAnimesBaseUrl.TrimEnd('/') + "/";
        if (!Uri.TryCreate(configuredBaseUrl, UriKind.Absolute, out var apiBaseUrl))
        {
            return ApiMyAnimesHealthStatus.Unavailable("The configured ApiMyAnimes URL is invalid.");
        }

        var healthEndpoint = new Uri(apiBaseUrl, "apiLocal/Health");

        try
        {
            using var handler = AppConfigurationService.CreateHttpClientHandler();
            using var client = new HttpClient(handler)
            {
                Timeout = HealthCheckTimeout
            };
            using var response = await client.GetAsync(
                healthEndpoint,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
            {
                return ApiMyAnimesHealthStatus.Unavailable(
                    "ApiMyAnimes is running, but DB_Local is unavailable.");
            }

            if (!response.IsSuccessStatusCode)
            {
                return ApiMyAnimesHealthStatus.Unavailable(
                    $"ApiMyAnimes returned HTTP {(int)response.StatusCode}.");
            }

            var health = await response.Content.ReadFromJsonAsync<HealthResponse>(cancellationToken);
            if (health is not null
                && string.Equals(health.Status, "ok", StringComparison.OrdinalIgnoreCase)
                && string.Equals(health.Database, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return ApiMyAnimesHealthStatus.Available();
            }

            return ApiMyAnimesHealthStatus.Unavailable(
                "ApiMyAnimes did not confirm DB_Local availability.");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return ApiMyAnimesHealthStatus.Unavailable("The ApiMyAnimes health check timed out.");
        }
        catch (HttpRequestException)
        {
            return ApiMyAnimesHealthStatus.Unavailable("ApiMyAnimes is not reachable.");
        }
        catch (NotSupportedException)
        {
            return ApiMyAnimesHealthStatus.Unavailable("ApiMyAnimes returned an unsupported health response.");
        }
    }

    private sealed record HealthResponse(string? Status, string? Database);
}

public sealed record ApiMyAnimesHealthStatus(bool IsAvailable, string Message)
{
    public static ApiMyAnimesHealthStatus Available() => new(true, string.Empty);

    public static ApiMyAnimesHealthStatus Unavailable(string message) => new(false, message);
}
