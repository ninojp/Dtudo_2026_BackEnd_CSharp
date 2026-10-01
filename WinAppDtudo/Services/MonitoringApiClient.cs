using System.Net.Http.Json;
using System.Text.Json;
using LibDtudo.Shared.Dtos.FileMonitoring;

namespace WinAppDtudo.Services;

public sealed class MonitoringApiClient : IDisposable
{
    private static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(120);
    private readonly WinAppAuthenticationService authentication;
    private readonly HttpClient client;

    public MonitoringApiClient(WinAppAuthenticationService authenticationService)
    {
        authentication = authenticationService;
        client = new HttpClient(AppConfigurationService.CreateHttpClientHandler())
        {
            BaseAddress = new Uri(AppConfigurationService.ApiFileStorageBaseUrl.TrimEnd('/') + "/api/file-storage/monitoring/"),
            Timeout = RequestTimeout
        };
    }

    public Task<MonitoringRootDto[]> RootsAsync(CancellationToken token) => SendAsync<MonitoringRootDto[]>(HttpMethod.Get, "roots", null, token);
    public Task<CollectionSyncDiscoveryDto> DiscoverCollectionsAsync(CancellationToken token) =>
        SendAsync<CollectionSyncDiscoveryDto>(HttpMethod.Post, "synchronizations/discovery", null, token);
    public Task<MonitoringSessionDto> StartAsync(StartMonitoringRequest request, CancellationToken token) => SendAsync<MonitoringSessionDto>(HttpMethod.Post, "sessions", request, token);
    public Task<MonitoringSessionDto> HeartbeatAsync(Guid id, CancellationToken token) => SendAsync<MonitoringSessionDto>(HttpMethod.Post, $"sessions/{id}/heartbeat", null, token);
    public Task<bool> StopAsync(Guid id, CancellationToken token) => SendAsync<bool>(HttpMethod.Delete, $"sessions/{id}", null, token);
    public Task<bool> RefreshAsync(Guid id, CancellationToken token) => SendAsync<bool>(HttpMethod.Post, $"sessions/{id}/refresh", null, token);
    public Task<MonitoringEventDto[]> EventsAsync(Guid id, long afterId, bool latest, CancellationToken token) =>
        SendAsync<MonitoringEventDto[]>(HttpMethod.Get, $"sessions/{id}/events?afterId={afterId}&take=200&latest={latest}", null, token);
    public Task<MonitoringEntryDto[]> EntriesAsync(Guid id, Guid location, Guid snapshot, long afterId, CancellationToken token) =>
        SendAsync<MonitoringEntryDto[]>(HttpMethod.Get, $"sessions/{id}/entries/{location}?snapshotId={snapshot}&afterId={afterId}&take=1000", null, token);

    private async Task<T> SendAsync<T>(HttpMethod method, string path, object? body, CancellationToken token)
    {
        using var response = await authentication.SendAuthenticatedAsync(client, _ => new HttpRequestMessage(method, path)
        {
            Content = body is null ? null : JsonContent.Create(body)
        }, token);
        if (!response.IsSuccessStatusCode)
        {
            var message = $"Monitoramento: HTTP {(int)response.StatusCode}.";
            try
            {
                using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(token));
                if (problem.RootElement.TryGetProperty("title", out var title)) message = title.GetString() ?? message;
            }
            catch (JsonException) { }
            throw new HttpRequestException(message, null, response.StatusCode);
        }
        return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token)
            ?? throw new InvalidOperationException("Resposta vazia da API de monitoramento.");
    }

    public void Dispose() => client.Dispose();
}
